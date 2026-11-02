using System.Text.Json.Nodes;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobileDiceTests
{
    [Fact]
    public void Old04SaveLoadsWithProfileAlbumAndBlackjackUnchanged()
    {
        var session = Blackjack();
        session.Rename("Timo");
        session.StartRound(10);
        session.OpenPack(CollectionRarity.Common, new Random(19));
        session.RevealNextCard();
        MobileState original = session.Export();
        JsonObject json = JsonNode.Parse(MobileJson.Serialize(original))!.AsObject();
        foreach (string key in new[] { "CasualDice", "DiceTable", "CasualDiceAward", "DiceTableAward" }) { json.Remove(key); }
        json["Settings"]!.AsObject().Remove("ReduceDiceAnimations");
        session = BlackjackSession.Restore(MobileJson.Deserialize<MobileState>(json.ToJsonString()));
        Assert.Equal("Timo", session.Profile.DisplayName);
        Assert.Equal(990m, session.AvailableCash);
        Assert.Null(session.DealerCards[1]);
        Assert.Equal(original.Profile.Collection.PendingOpening!.Id, session.Profile.Collection.PendingOpening!.Id);
        Assert.Equal(1, session.RevealedCards);
        Assert.Null(session.CasualDice);
        Assert.Null(session.DiceTable);
        Assert.False(session.Settings.ReduceDiceAnimations);
    }

    [Fact]
    public void DiceAndHoldsSurviveRestartWithoutDrawingAgain()
    {
        var session = new BlackjackSession();
        session.StartCasualDice();
        Guid id = session.CasualDice!.GameId;
        int calls = 0;
        session.RollDice(false, () => ++calls);
        session.HoldDie(false, 1);
        session.HoldDie(false, 4);
        session = Reload(session);
        Assert.Equal(id, session.CasualDice!.GameId);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, session.CasualDice.Dice);
        Assert.True(session.CasualDice.Held[1]);
        Assert.True(session.CasualDice.Held[4]);
        Assert.Equal(5, calls);
        calls = 0;
        session.RollDice(false, () => { calls++; return 6; });
        Assert.Equal(3, calls);
        Assert.Equal(new[] { 6, 2, 6, 6, 5 }, session.CasualDice!.Dice);
        Assert.Equal(2, session.CasualDice.RollsUsed);
        Assert.Equal(1000m, session.Bankroll);
    }

    [Fact]
    public async Task CasualCompletionAwardsOnceAndKeepsBlackjackStatisticsSeparate()
    {
        var session = new BlackjackSession();
        session.StartCasualDice();
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            session.RollDice(false, () => 4);
            await session.ScoreDiceAsync(false, category);
            session = Reload(session);
        }
        KniffelGame finished = KniffelGame.Restore(session.CasualDice);
        Assert.Equal(KniffelProgression.PlayerExperience(finished.Total), session.Profile.LifetimeExperience);
        Assert.Equal(1, session.Profile.Kniffel.CompletedGames);
        Assert.Equal(finished.Total, session.Profile.Kniffel.BestScore);
        Assert.Equal(13, session.Profile.Kniffel.FiveOfAKinds);
        Assert.Equal(0, session.Profile.Kniffel.CasinoGamesStarted);
        Assert.Equal(0, session.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(1000m, session.Bankroll);
        string before = MobileJson.Serialize(session.Export());
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ScoreDiceAsync(false, KniffelCategory.Chance));
        Assert.Throws<InvalidOperationException>(() => session.RollDice(false));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(session.Profile.LifetimeExperience, Reload(session).Profile.LifetimeExperience);
    }

    [Fact]
    public async Task CasinoWholeGamePaysAndRewardsExactlyOnceAcrossEveryRestart()
    {
        var session = new BlackjackSession();
        session.StartDiceTable(100);
        Assert.Equal(900m, session.Bankroll);
        Assert.Equal(0, session.Profile.Coins);
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            session.RollDice(true, () => 3);
            session = Reload(session);
            await session.ScoreDiceAsync(true, category);
            session = Reload(session);
            Assert.All(session.DiceTable!.Opponents, other => Assert.Equal(session.DiceTable.Player.Entries.Length, other.Entries.Length));
        }
        KniffelTableState result = session.DiceTable!;
        Assert.True(result.Settled);
        Assert.Equal(900m + KniffelTable.Prize(result), session.Bankroll);
        Assert.Equal(1, session.Profile.Kniffel.CasinoGamesStarted);
        Assert.Equal(1, session.Profile.Kniffel.CasinoGamesCompleted);
        Assert.Equal(1, session.Profile.Kniffel.CompletedGames);
        Assert.Equal(100m, session.Profile.Kniffel.TotalWagered);
        Assert.Equal(result.Payout, session.Profile.Kniffel.TotalReturned);
        Assert.Equal(0, session.Profile.LifetimeStatistics.RoundsPlayed);
        long xp = session.Profile.LifetimeExperience;
        decimal cash = session.Bankroll;
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ScoreDiceAsync(true, KniffelCategory.Chance));
        Assert.Throws<InvalidOperationException>(session.ForfeitDiceTable);
        session = Reload(session);
        Assert.Equal(cash, session.Bankroll);
        Assert.Equal(xp, session.Profile.LifetimeExperience);
        Assert.Equal(1, session.Profile.Kniffel.CasinoGamesCompleted);
    }

    [Fact]
    public void FailedEntryRestoresCashReceiptsAndCompletedBlackjackJournal()
    {
        bool fail = false;
        var session = Blackjack(_ => { if (fail) { throw new IOException("full"); } });
        session.StartRound(10);
        session.Act(PlayerAction.DoubleDown);
        string before = MobileJson.Serialize(session.Export());
        fail = true;
        Assert.Throws<InvalidOperationException>(() => session.StartDiceTable(100));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.NotNull(session.Round!.Result);
        fail = false;
        session.StartDiceTable(100);
        session = Reload(session);
        Assert.Null(session.Round);
        Assert.Single(session.History);
        Assert.Equal(1, session.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(920m, session.Bankroll);
    }

    [Fact]
    public async Task FinalCasinoWriteFailureRollsBackAllSeatsCashAndXpForSafeRetry()
    {
        var session = new BlackjackSession();
        session.StartDiceTable(100);
        foreach (KniffelCategory category in KniffelScoring.Categories.Take(12))
        { session.RollDice(true, () => 2); await session.ScoreDiceAsync(true, category); }
        session.RollDice(true, () => 2);
        MobileState saved = session.Export();
        string before = MobileJson.Serialize(saved);
        session = BlackjackSession.Restore(saved, _ => throw new IOException("full"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ScoreDiceAsync(true, KniffelCategory.Chance));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(900m, session.Bankroll);
        Assert.Equal(0, session.Profile.LifetimeExperience);
        Assert.Equal(0, session.Profile.Kniffel.CompletedGames);
        session = Reload(session);
        await session.ScoreDiceAsync(true, KniffelCategory.Chance);
        Assert.True(session.DiceTable!.Settled);
        Assert.Equal(900m + session.DiceTable.Payout, session.Bankroll);
        Assert.Equal(1, session.Profile.Kniffel.CompletedGames);
    }

    [Fact]
    public async Task CasualFinalWriteFailureDoesNotGrantPartialRewards()
    {
        var session = new BlackjackSession();
        session.StartCasualDice();
        foreach (KniffelCategory category in KniffelScoring.Categories.Take(12))
        { session.RollDice(false, () => 1); await session.ScoreDiceAsync(false, category); }
        session.RollDice(false, () => 1);
        string before = MobileJson.Serialize(session.Export());
        session = BlackjackSession.Restore(session.Export(), _ => throw new UnauthorizedAccessException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ScoreDiceAsync(false, KniffelCategory.Chance));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(0, session.Profile.Kniffel.CompletedGames);
    }

    [Fact]
    public void ForfeitAndFailedForfeitCannotRefundEntryOrAwardCompletionXp()
    {
        bool fail = false;
        var session = new BlackjackSession(persist: _ => { if (fail) { throw new IOException(); } });
        session.StartDiceTable(500);
        string before = MobileJson.Serialize(session.Export());
        fail = true;
        Assert.Throws<InvalidOperationException>(session.ForfeitDiceTable);
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        fail = false;
        session.ForfeitDiceTable();
        session = Reload(session);
        Assert.Equal(500m, session.Bankroll);
        Assert.Equal(1, session.Profile.Kniffel.Forfeits);
        Assert.Equal(-500m, session.Profile.Kniffel.NetProfit);
        Assert.Equal(0, session.Profile.LifetimeExperience);
        Assert.Equal(0, session.Profile.Kniffel.CompletedGames);
        Assert.Throws<InvalidOperationException>(session.ForfeitDiceTable);
        session.StartRound(10);
        Assert.True(session.IsRoundActive || session.Round!.Result is not null);
    }

    [Fact]
    public async Task CasualBlackjackAndPackCanCoexistButPaidGamesCannotOverlap()
    {
        var session = Blackjack();
        session.StartRound(10);
        Assert.Throws<InvalidOperationException>(() => session.StartDiceTable(100));
        session.StartCasualDice();
        session.OpenPack(CollectionRarity.Common, new Random(12));
        session.RevealNextCard();
        session.RollDice(false, () => 5);
        await session.ScoreDiceAsync(false, KniffelCategory.Fives);
        session = Reload(session);
        Assert.True(session.IsRoundActive);
        Assert.Null(session.DealerCards[1]);
        Assert.Single(session.CasualDice!.Entries);
        Assert.Equal(1, session.RevealedCards);
        session.Act(PlayerAction.DoubleDown);
        session.StartDiceTable(100);
        Assert.Throws<InvalidOperationException>(() => session.StartRound(10));
        Assert.Throws<InvalidOperationException>(session.RefillPracticeCash);
        session = Reload(session);
        Assert.True(session.IsDiceTableActive);
        Assert.Equal(1, session.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Single(session.CasualDice!.Entries);
        Assert.Equal(1, session.RevealedCards);
    }

    [Fact]
    public void DiceShopUsesCoinsOnlyAndRollsBackOwnershipWhenSavingFails()
    {
        var seed = new BlackjackSession().Export();
        seed.Profile.Coins = 3000;
        var session = BlackjackSession.Restore(seed, _ => throw new IOException());
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(() => session.PurchaseDiceSet("diceset-obsidian"));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        session = Reload(session);
        Assert.Throws<InvalidOperationException>(() => session.EquipDiceSet("diceset-jade"));
        Assert.Throws<InvalidOperationException>(() => session.PurchaseDiceSet(CosmeticCatalog.DefaultTitleId));
        session.PurchaseDiceSet("diceset-obsidian");
        session = Reload(session);
        Assert.Equal(600, session.Profile.Coins);
        Assert.Equal(1000m, session.Bankroll);
        Assert.Equal("diceset-obsidian", session.Profile.EquippedDiceSetId);
        Assert.Contains("diceset-obsidian", session.Profile.OwnedCosmeticIds);
        Assert.Throws<InvalidOperationException>(() => session.PurchaseDiceSet("diceset-obsidian"));
        session.EquipDiceSet(CosmeticCatalog.DefaultDiceSetId);
        session.SetReducedDiceAnimations(true);
        session = Reload(session);
        Assert.Equal(CosmeticCatalog.DefaultDiceSetId, session.Profile.EquippedDiceSetId);
        Assert.True(session.Settings.ReduceDiceAnimations);
    }

    [Fact]
    public async Task DiceChallengesAwardOnePackAndCoinsOnlyOnce()
    {
        var session = new BlackjackSession();
        session.StartDiceTable(100);
        foreach (KniffelCategory category in KniffelScoring.Categories)
        { session.RollDice(true, () => 6); await session.ScoreDiceAsync(true, category); }
        ChallengeDefinition challenge = ChallengeCatalog.TryGet("dice-table-1")!;
        Assert.True(BlackjackSession.SupportedChallenge(challenge));
        Assert.True(ChallengeCatalog.Complete(session.Profile, challenge));
        long coins = session.Profile.Coins;
        int packs = session.Profile.Collection.Packs.Values.Sum();
        session.ClaimChallenge(challenge.Id);
        session = Reload(session);
        Assert.Equal(coins + challenge.CoinReward, session.Profile.Coins);
        Assert.Equal(packs + 1, session.Profile.Collection.Packs.Values.Sum());
        Assert.Throws<InvalidOperationException>(() => session.ClaimChallenge(challenge.Id));
    }

    [Fact]
    public async Task InvalidActionsAndDetachedViewsCannotMutateSavedDice()
    {
        var session = new BlackjackSession();
        Assert.Throws<InvalidOperationException>(() => session.RollDice(false));
        session.StartCasualDice();
        Assert.Throws<InvalidOperationException>(() => session.StartCasualDice());
        Assert.Throws<InvalidOperationException>(() => session.HoldDie(false, 0));
        session.RollDice(false, () => 2);
        session.CasualDice!.Dice[0] = 6;
        Assert.Equal(2, session.CasualDice!.Dice[0]);
        await session.ScoreDiceAsync(false, KniffelCategory.Twos);
        session.RollDice(false, () => 3);
        string before = MobileJson.Serialize(session.Export());
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ScoreDiceAsync(false, KniffelCategory.Twos));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        session.StartDiceTable(100);
        session.DiceTable!.Player.Dice[0] = 6;
        Assert.Equal(0, session.DiceTable!.Player.Dice[0]);
        Assert.Throws<InvalidOperationException>(() => session.StartDiceTable(100));
    }

    [Fact]
    public void InvalidDiceSavesAreRejectedInsteadOfResettingExistingGames()
    {
        var session = new BlackjackSession();
        session.StartCasualDice();
        MobileState state = session.Export();
        state.CasualDice = state.CasualDice! with { Dice = [7, 0, 0, 0, 0] };
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(state));
        state = session.Export();
        state.CasualDice = state.CasualDice! with { Version = 2 };
        Assert.Throws<NotSupportedException>(() => BlackjackSession.Restore(state));
        session.StartDiceTable(100);
        state = session.Export();
        state.Profile.Kniffel.EnteredTableIds.Clear();
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(state));
        state = session.Export();
        state.DiceTable = state.DiceTable! with { Opponents = [] };
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(state));
    }

    [Fact]
    public async Task OverlappingScoreCommandsAdoptOnlyOneAiTurn()
    {
        var session = new BlackjackSession();
        session.StartDiceTable(100);
        session.RollDice(true, () => 3);
        Task first = session.ScoreDiceAsync(true, KniffelCategory.Threes);
        Task repeated = session.ScoreDiceAsync(true, KniffelCategory.Threes);
        try { await Task.WhenAll(first, repeated); }
        catch (InvalidOperationException) { }
        Assert.Equal(1, new[] { first, repeated }.Count(task => task.IsCompletedSuccessfully));
        Assert.Single(session.DiceTable!.Player.Entries);
        Assert.All(session.DiceTable.Opponents, other => Assert.Single(other.Entries));
        Assert.Equal(900m, session.Bankroll);
        Assert.Equal(0, session.Profile.Kniffel.CompletedGames);
        _ = Reload(session);
    }

    [Fact]
    public void RealSaveStoreRoundTripKeepsHeldDiceAndTableDebit()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mobile-dice-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new MobileSaveStore(directory);
            var session = new BlackjackSession(persist: store.Save);
            session.StartDiceTable(100);
            session.RollDice(true, () => 4);
            session.HoldDie(true, 2);
            session = BlackjackSession.Restore(store.Load()!);
            Assert.Equal(900m, session.Bankroll);
            Assert.True(session.DiceTable!.Player.Held[2]);
            Assert.Equal(1, session.DiceTable.Player.RollsUsed);
            Assert.All(session.DiceTable.Player.Dice, value => Assert.Equal(4, value));
            Assert.Equal(1, session.Profile.Kniffel.CasinoGamesStarted);
        }
        finally { if (Directory.Exists(directory)) { Directory.Delete(directory, true); } }
    }

    private static BlackjackSession Reload(BlackjackSession session) => BlackjackSession.Restore(
        MobileJson.Deserialize<MobileState>(MobileJson.Serialize(session.Export())));
    private static BlackjackSession Blackjack(Action<MobileState>? persist = null) => new(createShoe: () => new FixedShoe(), persist: persist);
    private sealed class FixedShoe : ICardShoe
    {
        private readonly Queue<Card> cards = new(new[] { Rank.Five, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten, Rank.Queen }
            .Select(rank => new Card(rank, Suit.Spades)));
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
