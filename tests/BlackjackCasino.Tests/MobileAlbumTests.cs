using System.Text.Json.Nodes;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobileAlbumTests
{
    [Fact]
    public void Version03Profile_UpgradesWithoutExtraWelcomePacksOrLostProgress()
    {
        var old = new BlackjackSession();
        old.Rename("Timo");
        old.UpdateSettings(true, false);
        MobileState oldState = old.Export();
        oldState.Bankroll = 1500;
        oldState.Profile.Coins = 137;
        oldState.Profile.Level = 2;
        oldState.Profile.LevelExperience = 42;
        oldState.Profile.LifetimeExperience = 250;
        JsonObject json = JsonNode.Parse(MobileJson.Serialize(oldState))!.AsObject();
        json.Remove("RevealedCards");
        json["Settings"]!.AsObject().Remove("ReducePackAnimations");
        var upgraded = BlackjackSession.Restore(MobileJson.Deserialize<MobileState>(json.ToJsonString()));
        Assert.Equal("Timo", upgraded.Profile.DisplayName);
        Assert.Equal(1500m, upgraded.Bankroll);
        Assert.Equal(137, upgraded.Profile.Coins);
        Assert.Equal(2, upgraded.Profile.Level);
        Assert.Equal(42, upgraded.Profile.LevelExperience);
        Assert.Equal(250, upgraded.Profile.LifetimeExperience);
        Assert.Equal(3, upgraded.Profile.Collection.Packs[CollectionRarity.Common]);
        Assert.True(upgraded.Settings.LargeCards);
        Assert.False(upgraded.Settings.ConfirmSurrender);
        Assert.False(upgraded.Settings.ReducePackAnimations);
        Assert.Equal(0, upgraded.RevealedCards);
        upgraded = Reload(upgraded);
        Assert.Equal(3, upgraded.Profile.Collection.Packs[CollectionRarity.Common]);
    }

    [Fact]
    public void OpenAndResume_PreservesReceiptCardsAndRevealPosition_WithoutReroll()
    {
        var session = new BlackjackSession();
        session.OpenPack(CollectionRarity.Common, new Random(21));
        CollectionOpening opening = session.Profile.Collection.PendingOpening!;
        string cards = MobileJson.Serialize(opening.Cards);
        int count = session.Profile.Collection.OwnedCardIds.Count;
        Assert.Equal(5, opening.Cards.Count);
        Assert.Equal(opening.Cards.Count(card => card.IsNew), count);
        Assert.Equal(2 + opening.PacksEarned, session.Profile.Collection.Packs[CollectionRarity.Common]);
        session.RevealNextCard();
        session.RevealNextCard();
        session.SetReducedPackAnimations(true);
        session = Reload(session);
        Assert.Equal(2, session.RevealedCards);
        Assert.True(session.Settings.ReducePackAnimations);
        Assert.Equal(opening.Id, session.Profile.Collection.PendingOpening!.Id);
        Assert.Equal(cards, MobileJson.Serialize(session.Profile.Collection.PendingOpening!.Cards));
        Assert.Throws<InvalidOperationException>(() => session.OpenPack(CollectionRarity.Common));
        Assert.Throws<InvalidOperationException>(() => session.CompletePack(opening.Id));
        Assert.Equal(1, session.Profile.Collection.PacksOpened);
        session.RevealAllCards();
        Assert.Throws<InvalidOperationException>(() => session.CompletePack(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(session.RevealNextCard);
        session.CompletePack(opening.Id);
        session = Reload(session);
        Assert.Null(session.Profile.Collection.PendingOpening);
        Assert.Equal(0, session.RevealedCards);
        Assert.Equal(count, session.Profile.Collection.OwnedCardIds.Count);
        Assert.Equal(1, session.Profile.Collection.PacksOpened);
        Assert.Throws<InvalidOperationException>(() => session.CompletePack(opening.Id));
    }

    [Fact]
    public void DuplicatePointsRecycleAutomatically_AndDoNotPayAgainOnRevealOrRestart()
    {
        var seed = new BlackjackSession().Export();
        seed.Profile.Collection.OwnedCardIds = CollectionCatalog.Cards.Select(card => card.Id).ToHashSet();
        seed.Profile.Collection.DuplicatePoints = 490;
        var session = BlackjackSession.Restore(seed);
        session.OpenPack(CollectionRarity.Common, new Random(2));
        CollectionOpening opening = session.Profile.Collection.PendingOpening!;
        Assert.All(opening.Cards, card => Assert.False(card.IsNew));
        Assert.True(opening.PacksEarned > 0);
        int points = (490 + opening.PointsEarned) % CollectionCatalog.DuplicatePackCost;
        int packs = 2 + opening.PacksEarned;
        Assert.Equal(points, session.Profile.Collection.DuplicatePoints);
        Assert.Equal(packs, session.Profile.Collection.Packs[CollectionRarity.Common]);
        session = Reload(session);
        session.RevealAllCards();
        session.CompletePack(opening.Id);
        session = Reload(session);
        Assert.Equal(points, session.Profile.Collection.DuplicatePoints);
        Assert.Equal(packs, session.Profile.Collection.Packs[CollectionRarity.Common]);
        Assert.Equal(136, session.Profile.Collection.OwnedCardIds.Count);
        Assert.Equal(1000m, session.Bankroll);
    }

    [Fact]
    public void PackPurchasesSpendCoinsOnly_AndRejectUnaffordableOrUnknownPacks()
    {
        var seed = new BlackjackSession().Export();
        seed.Profile.Coins = 200;
        var session = BlackjackSession.Restore(seed);
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(() => session.PurchasePack(CollectionRarity.Rare));
        Assert.Throws<InvalidOperationException>(() => session.PurchasePack((CollectionRarity)99));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        session.PurchasePack(CollectionRarity.Common);
        session = Reload(session);
        Assert.Equal(100, session.Profile.Coins);
        Assert.Equal(4, session.Profile.Collection.Packs[CollectionRarity.Common]);
        Assert.Equal(1000m, session.Bankroll);
    }

    [Theory]
    [InlineData("velvet-table", 300, CollectionRarity.Rare)]
    [InlineData("astral-sovereigns", 750, CollectionRarity.Epic)]
    public void ThemeRewards_CannotBeClaimedBeforeCompletionOrTwice(string themeId, int coins, CollectionRarity rewardRarity)
    {
        var session = new BlackjackSession();
        Assert.Throws<InvalidOperationException>(() => session.ClaimThemeReward(themeId));
        var seed = session.Export();
        CollectionTheme theme = CollectionCatalog.Themes.Single(item => item.Id == themeId);
        seed.Profile.Collection.OwnedCardIds.UnionWith(theme.Cards.Select(card => card.Id));
        session = BlackjackSession.Restore(seed);
        session.ClaimThemeReward(themeId);
        session = Reload(session);
        Assert.Equal(coins, session.Profile.Coins);
        Assert.Equal(1, session.Profile.Collection.Packs[rewardRarity]);
        Assert.Contains(themeId, session.Profile.Collection.ClaimedThemeRewardIds);
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(() => session.ClaimThemeReward(themeId));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
    }

    [Fact]
    public void FailedPackWrite_RollsBackInventoryAndCoinsBeforeAnyCardIsShown()
    {
        bool fail = true;
        var session = new BlackjackSession(persist: _ => { if (fail) { throw new IOException("full"); } });
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(() => session.OpenPack(CollectionRarity.Common, new Random(42)));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Null(session.Profile.Collection.PendingOpening);
        fail = false;
        session.OpenPack(CollectionRarity.Common, new Random(42));
        Assert.Equal(1, session.Profile.Collection.PacksOpened);
    }

    [Fact]
    public void FailedRevealAndCompletionWrites_KeepTheSamePendingPackForRetry()
    {
        bool fail = false;
        var session = new BlackjackSession(persist: _ => { if (fail) { throw new IOException("full"); } });
        session.OpenPack(CollectionRarity.Common, new Random(42));
        Guid id = session.Profile.Collection.PendingOpening!.Id;
        fail = true;
        Assert.Throws<InvalidOperationException>(session.RevealNextCard);
        Assert.Equal(0, session.RevealedCards);
        Assert.Equal(id, session.Profile.Collection.PendingOpening!.Id);
        fail = false;
        session.RevealAllCards();
        string before = MobileJson.Serialize(session.Export());
        fail = true;
        Assert.Throws<InvalidOperationException>(() => session.CompletePack(id));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        fail = false;
        session.CompletePack(id);
        Assert.Null(session.Profile.Collection.PendingOpening);
    }

    [Fact]
    public void AlbumAndOpenBlackjackHand_RestoreTogether_AndKeepRewardsSeparate()
    {
        var session = new BlackjackSession(createShoe: () => new FixedShoe());
        session.StartRound(10);
        session.OpenPack(CollectionRarity.Common, new Random(123));
        session.RevealNextCard();
        Guid id = session.Profile.Collection.PendingOpening!.Id;
        session = Reload(session);
        Assert.Null(session.DealerCards[1]);
        Assert.Equal(990m, session.AvailableCash);
        Assert.Equal(1, session.RevealedCards);
        Assert.Equal(id, session.Profile.Collection.PendingOpening!.Id);
        session.Act(PlayerAction.DoubleDown);
        long xp = session.Profile.LifetimeExperience;
        Assert.Equal(1020m, session.Bankroll);
        session.RevealAllCards();
        session.CompletePack(id);
        session = Reload(session);
        Assert.Equal(1020m, session.Bankroll);
        Assert.Equal(xp, session.Profile.LifetimeExperience);
        Assert.Equal(1, session.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(1, session.Profile.Collection.PacksOpened);
    }

    [Fact]
    public void InvalidRevealState_AndRevealingWithoutPack_AreRejected()
    {
        var session = new BlackjackSession();
        Assert.Throws<InvalidOperationException>(session.RevealNextCard);
        Assert.Throws<InvalidOperationException>(session.RevealAllCards);
        var seed = session.Export();
        seed.RevealedCards = 1;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
        seed.RevealedCards = 6;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
        seed.RevealedCards = -1;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
    }

    private static BlackjackSession Reload(BlackjackSession session) => BlackjackSession.Restore(
        MobileJson.Deserialize<MobileState>(MobileJson.Serialize(session.Export())));
    private sealed class FixedShoe : ICardShoe
    {
        private readonly Queue<Card> cards = new(new[] { Rank.Five, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten, Rank.Queen }
            .Select(rank => new Card(rank, Suit.Spades)));
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
