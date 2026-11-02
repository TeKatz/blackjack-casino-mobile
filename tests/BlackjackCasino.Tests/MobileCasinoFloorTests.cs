using System.Text.Json.Nodes;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobileCasinoFloorTests
{
    [Fact]
    public void Upgrade05PreservesEveryGameAndUsesHistoricalWagersOnlyAsVaultBaseline()
    {
        var session = new BlackjackSession(createShoe: () => new FixedShoe());
        session.Rename("Timo"); session.StartRound(10);
        session.StartCasualDice(); session.RollDice(false, () => 4); session.HoldDie(false, 2);
        session.OpenPack(CollectionRarity.Common, new Random(55)); session.RevealNextCard();
        MobileState seed = session.Export();
        seed.Profile.LifetimeStatistics.TotalWagered = 500_000;
        seed.Profile.Vault.CurrentAmount = 0;
        JsonObject json = JsonNode.Parse(MobileJson.Serialize(seed))!.AsObject();
        json.Remove("CasinoFloor"); json["Settings"]!.AsObject().Remove("ReduceSlotAnimations");
        session = BlackjackSession.Restore(MobileJson.Deserialize<MobileState>(json.ToJsonString()));
        Assert.Equal("Timo", session.Profile.DisplayName);
        Assert.True(session.IsRoundActive); Assert.Null(session.DealerCards[1]);
        Assert.Equal(990m, session.AvailableCash); Assert.True(session.CasualDice!.Held[2]);
        Assert.Equal(1, session.RevealedCards);
        Assert.Equal(seed.Profile.Collection.PendingOpening!.Id, session.Profile.Collection.PendingOpening!.Id);
        Assert.Equal(500_000m, session.Profile.Vault.ObservedEligibleWager);
        Assert.Equal(0m, session.Vault.CurrentAmount); Assert.Equal(0, session.Vault.VaultsOpened);
        Assert.False(session.Settings.ReduceSlotAnimations);
        session.Act(PlayerAction.DoubleDown);
        session = Reload(session);
        Assert.Equal(.4m, session.Vault.CurrentAmount); // doubled hand wagers 20
        Assert.Equal(1020m, session.Bankroll);
        Assert.Equal(0, session.Profile.Slots.Spins);
    }

    [Fact]
    public void ClassicSpinCashXpStatsAndVaultCommitTogetherAndRestoreWithoutAnotherSpin()
    {
        var session = new BlackjackSession();
        int seven = Stop(SlotSymbol.Seven);
        MobileSlotReceipt spin = session.SpinSlot(MobileSlotGame.Reels, 10, _ => seven);
        Assert.Equal(1990m, session.Bankroll);
        Assert.Equal(1000m, spin.Payout); Assert.Equal(990m, spin.Net);
        Assert.Equal(1, session.Profile.Slots.Spins); Assert.Equal(25, session.Profile.LifetimeExperience);
        Assert.Equal(10m, session.Profile.Slots.TotalWagered); Assert.Equal(.2m, session.Vault.CurrentAmount);
        Assert.Equal(0, session.Profile.LifetimeStatistics.RoundsPlayed);
        string before = MobileJson.Serialize(session.Export());
        session = Reload(session);
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(spin.Id, session.SlotHistory[0].Id);
        Assert.Equal(1000m, session.SlotHistory[0].Payout);
    }

    [Fact]
    public void RepeatedSpinCommandReturnsReceiptWithoutDrawingSpendingOrRewardingAgain()
    {
        var session = new BlackjackSession(); Guid command = Guid.NewGuid();
        MobileSlotReceipt spin = session.SpinSlot(MobileSlotGame.Reels, 10, _ => 0, command);
        session = Reload(session);
        string before = MobileJson.Serialize(session.Export());
        MobileSlotReceipt repeated = session.SpinSlot(MobileSlotGame.Reels, 10, _ => throw new Exception("must not draw"), command);
        Assert.Equal(spin.Id, repeated.Id); Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.Reels, 5, commandId: command));
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.NineLives, 10, commandId: command));
        repeated.Symbols[0] = 999;
        Assert.NotEqual(999, session.SlotHistory[0].Symbols[0]);
    }

    [Fact]
    public void NeonBonusRestoresTriggerBetAndNoExtraStakeOrVaultContribution()
    {
        var session = new BlackjackSession();
        int[] trigger = NeonGrid(); trigger[0] = 96; trigger[1] = 96; trigger[2] = 96;
        MobileSlotReceipt first = session.SpinSlot(MobileSlotGame.NeonVault, 100, Sequence(trigger.Concat([5000])));
        Assert.Equal(8, first.BonusAwarded); Assert.Equal(8, session.FreeSlotSpins(MobileSlotGame.NeonVault));
        session = Reload(session);
        decimal cash = session.Bankroll, vault = session.Vault.CurrentAmount;
        MobileSlotReceipt bonus = session.SpinSlot(MobileSlotGame.NeonVault, 1, Sequence(NeonGrid().Concat([5000])));
        Assert.True(bonus.IsBonus); Assert.Equal(100m, bonus.Bet); Assert.Equal(0m, bonus.Stake);
        Assert.Equal(cash + bonus.Payout, session.Bankroll); Assert.Equal(vault, session.Vault.CurrentAmount);
        Assert.Equal(7, bonus.BonusRemaining); Assert.Equal(7, Reload(session).FreeSlotSpins(MobileSlotGame.NeonVault));
        Assert.Equal(1, session.Profile.NeonVault.PaidSpins); Assert.Equal(1, session.Profile.NeonVault.FreeSpins);
        Assert.Equal(0, session.Profile.Slots.Spins); Assert.Equal(0, session.Profile.NineLives.Spins);
    }

    [Fact]
    public void NineLivesBonusRestoresAndRetriggersAtTheOriginalWager()
    {
        var session = new BlackjackSession();
        session.SpinSlot(MobileSlotGame.NineLives, 25, Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89]));
        session = Reload(session);
        MobileSlotReceipt bonus = session.SpinSlot(MobileSlotGame.NineLives, 1, Sequence([100, 100, 100, 0, 26, 48, 65, 77, 85]));
        Assert.True(bonus.IsBonus); Assert.Equal(25m, bonus.Bet); Assert.Equal(0m, bonus.Stake);
        Assert.Equal(9, bonus.BonusAwarded); Assert.Equal(17, bonus.BonusRemaining);
        session = Reload(session);
        Assert.Equal(25m, session.SlotBet(MobileSlotGame.NineLives));
        Assert.Equal(17, session.FreeSlotSpins(MobileSlotGame.NineLives));
        Assert.Equal(25m, session.Profile.NineLives.TotalWagered);
        Assert.Equal(.5m, session.Vault.CurrentAmount);
        Assert.Equal(1, session.Profile.NineLives.BonusSessions);
    }

    [Fact]
    public void FailedTriggerSpinRollsBackCashXpBonusGridAndVault()
    {
        var session = new BlackjackSession(persist: _ => throw new IOException("full"));
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.NineLives, 25,
            Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89])));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        Assert.Equal(0, session.FreeSlotSpins(MobileSlotGame.NineLives));
        Assert.Empty(session.SlotHistory); Assert.Equal(0, session.Profile.LifetimeExperience);
    }

    [Fact]
    public void FailedBonusSpinKeepsRemainingCountAndReceiptForRetry()
    {
        var session = new BlackjackSession();
        session.SpinSlot(MobileSlotGame.NineLives, 25, Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89]));
        string before = MobileJson.Serialize(session.Export());
        session = BlackjackSession.Restore(session.Export(), _ => throw new IOException());
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.NineLives, 1, Sequence(CatBonusGrid())));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        session = Reload(session);
        session.SpinSlot(MobileSlotGame.NineLives, 1, Sequence(CatBonusGrid()));
        Assert.Equal(8, session.FreeSlotSpins(MobileSlotGame.NineLives));
        Assert.Equal(2, session.Profile.NineLives.Spins);
    }

    [Fact]
    public void BonusMustFinishBeforeBlackjackDiceOrRefillButCanPauseForAnotherMachine()
    {
        var session = new BlackjackSession();
        session.SpinSlot(MobileSlotGame.NineLives, 25, Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89]));
        Assert.Throws<InvalidOperationException>(() => session.StartRound(10));
        Assert.Throws<InvalidOperationException>(() => session.StartDiceTable(100));
        Assert.Throws<InvalidOperationException>(session.RefillPracticeCash);
        session.StartCasualDice(); session.RollDice(false, () => 3);
        session.SpinSlot(MobileSlotGame.Reels, 1, Sequence(ReelsLoss()));
        Assert.Equal(9, session.FreeSlotSpins(MobileSlotGame.NineLives));
        for (int i = 0; i < 9; i++) { session.SpinSlot(MobileSlotGame.NineLives, 1, Sequence(CatBonusGrid())); }
        Assert.False(session.HasSlotBonus); session.StartRound(1);
        Assert.NotNull(session.Round); Assert.NotNull(Reload(session).Round);
    }

    [Fact]
    public void FreeSpinsCanContinueWithZeroCashAndNoRefill()
    {
        var session = new BlackjackSession();
        session.SpinSlot(MobileSlotGame.NineLives, 25, Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89]));
        MobileState seed = session.Export(); seed.Bankroll = 0;
        session = BlackjackSession.Restore(seed);
        Assert.Throws<InvalidOperationException>(session.RefillPracticeCash);
        MobileSlotReceipt result = session.SpinSlot(MobileSlotGame.NineLives, 1, Sequence(CatBonusGrid()));
        Assert.True(result.IsBonus); Assert.Equal(0, result.Stake);
        Assert.Equal(result.Payout, session.Bankroll);
    }

    [Fact]
    public void ActiveCashGamesBlockSlotsAndVaultWhileSettledHistoryRemainsAfterSwitch()
    {
        var session = new BlackjackSession(createShoe: () => new FixedShoe());
        session.StartRound(10);
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.Reels, 1));
        Assert.Throws<InvalidOperationException>(session.OpenCasinoVault);
        session.Act(PlayerAction.DoubleDown);
        session.SpinSlot(MobileSlotGame.Reels, 10, Sequence(ReelsLoss()));
        session = Reload(session);
        Assert.Null(session.Round); Assert.Single(session.History);
        Assert.Equal(1010m, session.Bankroll); Assert.Equal(1, session.Profile.LifetimeStatistics.RoundsPlayed);
        session.StartDiceTable(100);
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.Reels, 1));
        Assert.Throws<InvalidOperationException>(session.OpenCasinoVault);
        session.ForfeitDiceTable();
        session.SpinSlot(MobileSlotGame.Reels, 1, Sequence(ReelsLoss()));
        Assert.Equal(909m, session.Bankroll);
    }

    [Fact]
    public void VaultTracksBlackjackDiceAndPaidSlotsOnceWithoutRetroactiveRecredit()
    {
        var session = new BlackjackSession(createShoe: () => new FixedShoe());
        session.StartRound(10); session.Act(PlayerAction.DoubleDown);
        Assert.Equal(.4m, session.Vault.CurrentAmount);
        session.StartDiceTable(100); Assert.Equal(2.4m, session.Vault.CurrentAmount);
        session.ForfeitDiceTable(); Assert.Equal(2.4m, session.Vault.CurrentAmount);
        session.SpinSlot(MobileSlotGame.Reels, 10, Sequence(ReelsLoss()));
        Assert.Equal(2.6m, session.Vault.CurrentAmount);
        session.Rename("Vault Test"); session = Reload(session);
        Assert.Equal(2.6m, session.Vault.CurrentAmount);
        Assert.Equal(2.6m, session.Vault.LifetimeContributions);
    }

    [Fact]
    public void VaultOpenCreditsCashBonusAndOnePackOnlyOnce()
    {
        var session = FullVault();
        int packs = session.Profile.Collection.Packs.Values.Sum();
        decimal cash = session.Bankroll; long xp = session.Profile.LifetimeExperience;
        session.OpenCasinoVault(); session = Reload(session);
        Assert.Equal(cash + 11000m, session.Bankroll);
        Assert.Equal(11000m, session.LastVaultOpening!.TotalPayout);
        Assert.Equal(1, session.Vault.VaultsOpened); Assert.Equal(0, session.Vault.CurrentAmount);
        Assert.Equal(packs + 1, session.Profile.Collection.Packs.Values.Sum());
        Assert.Equal(xp, session.Profile.LifetimeExperience); Assert.Equal(Guid.Empty, session.Profile.Vault.PendingPayoutId);
        string before = MobileJson.Serialize(session.Export());
        Assert.Throws<InvalidOperationException>(session.OpenCasinoVault);
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
    }

    [Fact]
    public void FailedVaultWritePreservesFullVaultCashAndPackInventory()
    {
        var session = FullVault();
        string before = MobileJson.Serialize(session.Export());
        session = BlackjackSession.Restore(session.Export(), _ => throw new UnauthorizedAccessException());
        Assert.Throws<InvalidOperationException>(session.OpenCasinoVault);
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
        session = Reload(session); session.OpenCasinoVault();
        Assert.Equal(12000m, session.Bankroll); Assert.Equal(1, session.Vault.VaultsOpened);
    }

    [Fact]
    public void SlotChallengesAwardCoinsAndAlbumPacksExactlyOnce()
    {
        var session = new BlackjackSession();
        session.SpinSlot(MobileSlotGame.Reels, 10, _ => Stop(SlotSymbol.Seven));
        ChallengeDefinition challenge = ChallengeCatalog.All.First(item => item.Category == ChallengeCategory.MidnightReels && ChallengeCatalog.Complete(session.Profile, item));
        Assert.True(BlackjackSession.SupportedChallenge(challenge));
        int packs = session.Profile.Collection.Packs.Values.Sum(); long coins = session.Profile.Coins;
        session.ClaimChallenge(challenge.Id); session = Reload(session);
        Assert.Equal(packs + 1, session.Profile.Collection.Packs.Values.Sum());
        Assert.Equal(coins + challenge.CoinReward, session.Profile.Coins);
        Assert.Throws<InvalidOperationException>(() => session.ClaimChallenge(challenge.Id));
    }

    [Fact]
    public void MalformedOrFutureCheckpointsAndReceiptsAreRejectedWithoutResetting()
    {
        var session = new BlackjackSession(); MobileState seed = session.Export();
        seed.CasinoFloor!.Neon = new() { FreeSpinsRemaining = 2, BonusBet = 0 };
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
        seed = session.Export(); seed.CasinoFloor!.Cats = new() { Version = 2 };
        Assert.Throws<NotSupportedException>(() => BlackjackSession.Restore(seed));
        seed = session.Export(); seed.CasinoFloor!.History = null!;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
        session.SpinSlot(MobileSlotGame.Reels, 10, _ => 0);
        seed = session.Export(); seed.CasinoFloor!.History[0] = seed.CasinoFloor!.History[0] with { Payout = 1 };
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
        seed = session.Export(); seed.Profile.Vault.PendingPayoutId = Guid.NewGuid(); seed.Profile.Vault.PendingPayout = 100;
        Assert.Throws<InvalidDataException>(() => BlackjackSession.Restore(seed));
    }

    [Fact]
    public void HistoryIsBoundedWhileCareerTotalsAndSettingsKeepAccumulating()
    {
        var session = new BlackjackSession();
        for (int i = 0; i < 25; i++) { session.SpinSlot(MobileSlotGame.Reels, 1, Sequence(ReelsLoss())); }
        session.SetReducedSlotAnimations(true); session = Reload(session);
        Assert.Equal(20, session.SlotHistory.Count); Assert.Equal(25, session.Profile.Slots.Spins);
        Assert.Equal(975m, session.Bankroll); Assert.Equal(.5m, session.Vault.CurrentAmount);
        Assert.True(session.Settings.ReduceSlotAnimations);
    }

    [Fact]
    public void InvalidWagersAndRandomSourcesLeaveWalletProfileAndReceiptsUntouched()
    {
        var session = new BlackjackSession(); string before = MobileJson.Serialize(session.Export());
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SpinSlot(MobileSlotGame.Reels, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SpinSlot(MobileSlotGame.NineLives, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SpinSlot(MobileSlotGame.NeonVault, 250001));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SpinSlot(MobileSlotGame.Reels, 1001));
        Assert.Throws<InvalidOperationException>(() => new BlackjackSession(bankroll: 1).SpinSlot(MobileSlotGame.Reels, 10));
        Assert.Throws<InvalidOperationException>(() => session.SpinSlot(MobileSlotGame.NeonVault, 1, maximum => maximum));
        Assert.Equal(before, MobileJson.Serialize(session.Export()));
    }

    [Fact]
    public void RealSaveStoreRestoresBonusAndPackRevealWithoutRepeatingRewards()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mobile-floor-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new MobileSaveStore(directory);
            var session = new BlackjackSession(persist: store.Save);
            session.OpenPack(CollectionRarity.Common, new Random(7)); session.RevealNextCard();
            session.SpinSlot(MobileSlotGame.NineLives, 25, Sequence([97, 97, 97, 0, 28, 51, 69, 81, 89]));
            long xp = session.Profile.LifetimeExperience; decimal cash = session.Bankroll;
            session = BlackjackSession.Restore(store.Load()!);
            Assert.Equal(9, session.FreeSlotSpins(MobileSlotGame.NineLives)); Assert.Equal(1, session.RevealedCards);
            Assert.Equal(xp, session.Profile.LifetimeExperience); Assert.Equal(cash, session.Bankroll);
            Assert.Equal(1, session.Profile.NineLives.Spins);
        }
        finally { if (Directory.Exists(directory)) { Directory.Delete(directory, true); } }
    }

    private static BlackjackSession FullVault()
    {
        var seed = new BlackjackSession().Export();
        seed.Profile.Vault.CurrentAmount = CasinoVaultTierCatalog.Bronze.Capacity;
        seed.Profile.Vault.LifetimeContributions = seed.Profile.Vault.CurrentAmount;
        return BlackjackSession.Restore(seed);
    }
    private static BlackjackSession Reload(BlackjackSession session) => BlackjackSession.Restore(
        MobileJson.Deserialize<MobileState>(MobileJson.Serialize(session.Export())));
    private static int Stop(SlotSymbol symbol) => Enumerable.Range(0, SlotMachineRules.ReelStops).First(i => SlotMachineRules.SymbolAt(i) == symbol);
    private static int[] ReelsLoss() => [Stop(SlotSymbol.Lemon), Stop(SlotSymbol.Bell), Stop(SlotSymbol.Seven)];
    private static int[] NeonGrid() => [0, 28, 50, 68, 82, 0, 28, 50, 68, 82, 0, 28, 50, 68, 82, 0, 28, 50, 68, 82];
    private static int[] CatBonusGrid() => [0, 26, 48, 65, 77, 85, 25, 47, 64];
    private static Func<int, int> Sequence(IEnumerable<int> values)
    {
        var queue = new Queue<int>(values);
        return max => queue.Count > 0 ? queue.Dequeue() : max - 1;
    }
    private sealed class FixedShoe : ICardShoe
    {
        private readonly Queue<Card> cards = new(new[] { Rank.Five, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten, Rank.Queen }.Select(rank => new Card(rank, Suit.Spades)));
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
