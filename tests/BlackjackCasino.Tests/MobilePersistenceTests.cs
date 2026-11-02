using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobilePersistenceTests
{
    [Fact]
    public void ActiveHand_RestoresSameCardsAndShoe_ThenSettlesExactlyOnce()
    {
        var session = Session(Rank.Five, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten, Rank.Queen);
        session.StartRound(10);
        session.Rename("Timo");
        session.UpdateSettings(true, false);
        var restored = Reload(session);
        Assert.Equal("Timo", restored.Profile.DisplayName);
        Assert.True(restored.Settings.LargeCards);
        Assert.False(restored.Settings.ConfirmSurrender);
        Assert.Equal(10m, restored.Settings.LastWager);
        Assert.Equal(990m, restored.AvailableCash);
        Assert.Null(restored.DealerCards[1]);
        restored.Act(PlayerAction.DoubleDown);
        Assert.Equal(1020m, restored.Bankroll);
        Assert.Equal(1, restored.Profile.LifetimeStatistics.RoundsPlayed);
        long xp = restored.Profile.LifetimeExperience;
        var again = Reload(restored);
        Assert.Equal(1020m, again.Bankroll);
        Assert.Equal(xp, again.Profile.LifetimeExperience);
        Assert.Equal(1, again.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Single(again.History);
        Assert.Throws<InvalidOperationException>(() => again.Act(PlayerAction.Stand));
        Assert.Equal(xp, again.Profile.LifetimeExperience);
    }

    [Fact]
    public void InsuranceDecision_AndSplitDouble_ReplayAtCorrectActiveHand()
    {
        var insurance = Session(Rank.Seven, Rank.Ace, Rank.Eight, Rank.King);
        insurance.StartRound(10);
        var restored = Reload(insurance);
        Assert.Equal(RoundPhase.InsuranceDecision, restored.Round!.Phase);
        restored.TakeInsurance();
        restored = Reload(restored);
        Assert.Equal(1000m, restored.Bankroll);
        Assert.Equal(1, restored.Profile.LifetimeStatistics.InsuranceTaken);
        var split = Session(Rank.Eight, Rank.Ten, Rank.Eight, Rank.Six, Rank.Three, Rank.Two, Rank.Ten, Rank.Five);
        split.StartRound(10);
        split.Act(PlayerAction.Split);
        split = Reload(split);
        split.Act(PlayerAction.DoubleDown);
        split = Reload(split);
        Assert.Equal(1, split.Round!.ActiveHandIndex);
        Assert.Equal(30m, split.Committed);
        split.Act(PlayerAction.Stand);
        split = Reload(split);
        Assert.Equal(990m, split.Bankroll);
        Assert.Equal(1, split.Profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(2, split.Profile.LifetimeStatistics.HandsPlayed);
        Assert.Equal(1, split.Profile.LifetimeStatistics.Splits);
        Assert.Equal(1, split.Profile.LifetimeStatistics.Doubles);
    }

    [Fact]
    public void DeclinedInsurance_AndBustOrSurrender_KeepTheirRewardsAfterReload()
    {
        var declined = Session(Rank.Ten, Rank.Ace, Rank.Six, Rank.Nine);
        declined.StartRound(10);
        declined.DeclineInsurance();
        declined = Reload(declined);
        declined.Act(PlayerAction.Surrender);
        declined = Reload(declined);
        Assert.Equal(995m, declined.Bankroll);
        Assert.Equal(1, declined.Profile.LifetimeStatistics.Surrenders);
        Assert.Equal(0, declined.Profile.LifetimeStatistics.InsuranceTaken);
        var bust = Session(Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight, Rank.Ten);
        bust.StartRound(10);
        bust.Act(PlayerAction.Hit);
        bust = Reload(bust);
        Assert.Equal(990m, bust.Bankroll);
        Assert.Equal(1, bust.Profile.LifetimeStatistics.HandsLost);
        Assert.True(bust.Profile.LifetimeExperience > 0);
    }

    [Fact]
    public void CoreLevelRewards_AndHistory_SurviveRepeatedRestores()
    {
        var session = Session(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven,
            Rank.Ace, Rank.Ten, Rank.King, Rank.Seven, Rank.Ace, Rank.Ten, Rank.King, Rank.Seven);
        for (int i = 0; i < 3; i++) { session.StartRound(10); session = Reload(session); }
        Assert.Equal(1, session.Profile.Level);
        Assert.Equal(105, session.Profile.LifetimeExperience);
        Assert.True(session.Profile.Coins >= 25);
        Assert.Equal(3, session.Profile.LifetimeStatistics.Blackjacks);
        Assert.Equal(3, session.History.Count);
        Assert.Equal(1045m, session.Bankroll);
    }

    [Fact]
    public void FailedWrite_RollsBackCashCardsProgressAndHistory_ThenRetryAwardsOnce()
    {
        bool fail = false;
        var session = Session(() => { if (fail) { throw new IOException("disk full"); } },
            Rank.Five, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten, Rank.Queen);
        session.StartRound(10);
        string before = MobileJson.Serialize(session.Export());
        fail = true;
        Assert.Throws<InvalidOperationException>(() => session.Act(PlayerAction.DoubleDown));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(RoundPhase.PlayerTurn, session.Round!.Phase);
        Assert.Equal(0, session.Profile.LifetimeStatistics.RoundsPlayed);
        fail = false;
        session.Act(PlayerAction.DoubleDown);
        Assert.Equal(1020m, session.Bankroll);
        Assert.Equal(1, session.Profile.LifetimeStatistics.RoundsPlayed);
    }

    [Fact]
    public void ChallengeClaimsAndTitlePurchases_ArePermanentAndCannotPayTwice()
    {
        var session = Session(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven);
        for (int i = 0; i < 10; i++) { session.StartRound(10); }
        ChallengeDefinition challenge = ChallengeCatalog.All.First(item => BlackjackSession.SupportedChallenge(item)
            && ChallengeCatalog.Complete(session.Profile, item));
        long coins = session.Profile.Coins;
        session.ClaimChallenge(challenge.Id);
        session = Reload(session);
        Assert.Equal(coins + challenge.CoinReward, session.Profile.Coins);
        Assert.Throws<InvalidOperationException>(() => session.ClaimChallenge(challenge.Id));
        var saved = session.Export();
        saved.Profile.Coins = 1000;
        session = BlackjackSession.Restore(saved);
        session.PurchaseTitle("title-night-owl");
        session = Reload(session);
        Assert.Equal(700, session.Profile.Coins);
        Assert.Equal("title-night-owl", session.Profile.EquippedTitleId);
        Assert.Contains("title-night-owl", session.Profile.OwnedCosmeticIds);
        Assert.Throws<InvalidOperationException>(() => session.PurchaseTitle("title-night-owl"));
        Assert.Equal(700, session.Profile.Coins);
    }

    [Fact]
    public void ActiveSeasonAndAutomaticTierRewards_SurviveRestartWithoutRegrant()
    {
        var session = Session(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven);
        session.SelectSeason("season-1-midnight-shift");
        session = Reload(session);
        // Reload switches to the normal shoe factory once this deterministic shoe is exhausted,
        // so keep the factory while earning the first tier and then verify persisted rewards.
        var initial = new BlackjackSession(createShoe: () => new FixedShoe(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven));
        initial.SelectSeason("season-1-midnight-shift");
        for (int i = 0; i < 8; i++) { initial.StartRound(10); }
        session = Reload(initial);
        Assert.Equal("season-1-midnight-shift", session.Profile.ActiveSeasonId);
        Assert.Equal(1, session.Profile.Seasons[session.Profile.ActiveSeasonId].Tier);
        Assert.Equal(200, session.Profile.Seasons[session.Profile.ActiveSeasonId].Experience);
        long coins = session.Profile.Coins;
        Assert.True(coins >= 15);
        session = Reload(session);
        Assert.Equal(coins, session.Profile.Coins);
    }

    [Fact]
    public void InvalidSettingsNameAndUnsupportedShopItem_DoNotChangeSavedState()
    {
        var session = new BlackjackSession();
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<ArgumentException>(() => session.Rename("  "));
        Assert.Throws<InvalidOperationException>(() => session.PurchaseTitle("frame-slate"));
        Assert.Throws<InvalidOperationException>(() => session.EquipTitle("title-night-owl"));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
    }

    [Fact]
    public void InvalidReplayAndUnknownVersions_AreRejected()
    {
        var session = Session(Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight);
        session.StartRound(10);
        var saved = session.Export();
        saved.Bankroll++;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(saved));
        saved = session.Export();
        saved.Journal!.Commands.Add(ReplayCommand.TakeInsurance);
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(saved));
        saved = session.Export();
        saved.Shoe!.Position--;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(saved));
        saved = session.Export();
        saved.Version = 999;
        Assert.Throws<NotSupportedException>(() => BlackjackSession.Restore(saved));
    }

    [Fact]
    public void FileStore_RecoversBackup_PreservesItWhenPrimaryIsCorrupt()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new MobileSaveStore(dir);
            Assert.Null(store.Load());
            var session = new BlackjackSession(persist: store.Save);
            store.Save(session.Export());
            session.Rename("First");
            session.Rename("Second");
            File.WriteAllText(Path.Combine(dir, "mobile-profile-v1.json"), "broken");
            MobileState recovered = store.Load()!;
            Assert.Equal("First", recovered.Profile.DisplayName);
            Assert.NotNull(store.RecoveryNotice);
            var restored = BlackjackSession.Restore(recovered, store.Save);
            restored.Rename("Recovered");
            Assert.Equal("Recovered", store.Load()!.Profile.DisplayName);
            Assert.True(Directory.EnumerateFiles(dir, "*.corrupt-*").Any());
            File.WriteAllText(Path.Combine(dir, "mobile-profile-v1.json"), "broken again");
            Assert.Equal("First", store.Load()!.Profile.DisplayName);
        }
        finally { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } }
    }

    [Fact]
    public void FutureVersionOrBothCorruptFiles_BlockLoadingWithoutReplacingData()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "mobile-profile-v1.json");
        try
        {
            File.WriteAllText(path, "{\"Version\":999}");
            var store = new MobileSaveStore(dir);
            Assert.Throws<NotSupportedException>(() => store.Load());
            Assert.Throws<NotSupportedException>(() => store.Save(new BlackjackSession().Export()));
            Assert.Equal("{\"Version\":999}", File.ReadAllText(path));
            File.WriteAllText(path, "bad");
            File.WriteAllText(path + ".bak", "bad backup");
            Assert.Throws<InvalidDataException>(() => store.Load());
            Assert.Equal("bad", File.ReadAllText(path));
            Assert.Equal("bad backup", File.ReadAllText(path + ".bak"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void HistoryIsBoundedAndRefillKeepsProfileProgress()
    {
        var session = new BlackjackSession(createShoe: () => new FixedShoe(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven));
        for (int i = 0; i < 25; i++) { session.StartRound(10); }
        session = Reload(session);
        Assert.Equal(20, session.History.Count);
        Assert.Equal(25, session.Profile.LifetimeStatistics.RoundsPlayed);
        var state = session.Export();
        state.Bankroll = 0;
        state.Journal = null;
        session = BlackjackSession.Restore(state);
        long xp = session.Profile.LifetimeExperience;
        session.RefillPracticeCash();
        session = Reload(session);
        Assert.Equal(1000m, session.Bankroll);
        Assert.Equal(xp, session.Profile.LifetimeExperience);
        Assert.Equal(25, session.Profile.LifetimeStatistics.RoundsPlayed);
    }

    private static BlackjackSession Reload(BlackjackSession session) => BlackjackSession.Restore(
        MobileJson.Deserialize<MobileState>(MobileJson.Serialize(session.Export())));
    private static BlackjackSession Session(params Rank[] ranks) => new(createShoe: () => new FixedShoe(ranks));
    private static BlackjackSession Session(Action persist, params Rank[] ranks) =>
        new(createShoe: () => new FixedShoe(ranks), persist: _ => persist());
    private sealed class FixedShoe(params Rank[] ranks) : ICardShoe
    {
        private readonly Queue<Card> cards = new(ranks.Select(rank => new Card(rank, Suit.Spades)));
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
