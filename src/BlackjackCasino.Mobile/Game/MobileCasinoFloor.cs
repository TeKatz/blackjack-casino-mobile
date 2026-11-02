using BlackjackCasino.Core;

namespace BlackjackCasino.Mobile.Game;

public enum MobileSlotGame { Reels, NeonVault, NineLives }

public sealed record MobileSlotReceipt
{
    public Guid Id { get; init; }
    public Guid CommandId { get; init; }
    public MobileSlotGame Game { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public decimal RequestedBet { get; init; }
    public decimal Bet { get; init; }
    public decimal Stake { get; init; }
    public decimal Payout { get; init; }
    public decimal Before { get; init; }
    public decimal After { get; init; }
    public decimal Net => Payout - Stake;
    public int[] Symbols { get; init; } = [];
    public int[] HighlightCells { get; init; } = [];
    public string[] Events { get; init; } = [];
    public bool IsBonus { get; init; }
    public int BonusAwarded { get; init; }
    public int BonusRemaining { get; init; }
    public ProgressionAward Award { get; init; } = ProgressionAward.None;
}

public sealed class MobileCasinoFloor
{
    public int Version { get; set; } = 1;
    public NeonVaultCheckpoint Neon { get; set; } = new();
    public NineLivesCheckpoint Cats { get; set; } = new();
    public decimal[] LastBets { get; set; } = [10m, 10m, 10m];
    public List<MobileSlotReceipt> History { get; set; } = [];
    public CasinoVaultOpenResult? LastVaultOpening { get; set; }
}

public sealed partial class BlackjackSession
{
    private MobileCasinoFloor Floor => state.CasinoFloor!;
    public bool HasSlotBonus => Floor.Neon.FreeSpinsRemaining > 0 || Floor.Cats.BonusSpinsRemaining > 0;
    public int NeonEnergy => Floor.Neon.VaultEnergy;
    public bool NeonOverrideArmed => Floor.Neon.SecurityOverrideArmed;
    public CasinoVaultSnapshot Vault => CasinoVaultService.Snapshot(Profile);
    public CasinoVaultOpenResult? LastVaultOpening => Floor.LastVaultOpening;
    public IReadOnlyList<MobileSlotReceipt> SlotHistory => MobileJson.Deserialize<List<MobileSlotReceipt>>(MobileJson.Serialize(Floor.History));
    public int FreeSlotSpins(MobileSlotGame game) => game switch
    { MobileSlotGame.NeonVault => Floor.Neon.FreeSpinsRemaining, MobileSlotGame.NineLives => Floor.Cats.BonusSpinsRemaining, _ => 0 };
    public decimal SlotBet(MobileSlotGame game) => FreeSlotSpins(game) > 0
        ? game == MobileSlotGame.NeonVault ? Floor.Neon.BonusBet : Floor.Cats.BonusBet : Floor.LastBets[(int)game];

    public MobileSlotReceipt SpinSlot(MobileSlotGame game, decimal requestedBet, Func<int, int>? nextInt = null, Guid? commandId = null)
    {
        MobileSlotReceipt? completed = null;
        Guid command = commandId ?? Guid.NewGuid();
        Change(() =>
        {
            if (!Enum.IsDefined(game) || command == Guid.Empty) { throw new ArgumentException("Unknown machine or spin command."); }
            MobileSlotReceipt? previous = Floor.History.FirstOrDefault(item => item.CommandId == command);
            if (previous is not null)
            {
                if (previous.Game != game || previous.RequestedBet != requestedBet)
                { throw new InvalidOperationException("This spin command belongs to a different wager."); }
                completed = previous;
                return;
            }
            EnsureCashGameAvailable();
            var progression = new SlotProgression(new ProgressionService(Profile));
            if (game == MobileSlotGame.Reels)
            {
                var engine = new SlotMachineSession(Bankroll, nextInt);
                SlotSpinResult spin = engine.Spin(requestedBet);
                completed = new()
                {
                    Id = spin.SpinId, CompletedAt = spin.CompletedAt, Bet = spin.Bet, Stake = spin.Bet,
                    Before = spin.BankrollBefore, After = spin.BankrollAfter, Payout = spin.Payout,
                    Symbols = spin.Symbols.Select(symbol => (int)symbol).ToArray(),
                    HighlightCells = spin.Multiplier > 0 ? [0, 1, 2] : [],
                    Events = [$"Payout multiplier · {spin.Multiplier}×"], Award = progression.Process(spin),
                };
            }
            else if (game == MobileSlotGame.NeonVault)
            {
                NeonVaultSession engine = NeonVaultSession.Restore(Bankroll, Floor.Neon, nextInt);
                NeonVaultSpinResult spin = engine.Spin(requestedBet);
                Floor.Neon = engine.Checkpoint;
                var events = new List<string> { $"Ways {spin.WaysWon} · Ways return {spin.WaysPayout:N2}",
                    $"Scatter return {spin.ScatterPayout:N2} · Jackpot return {spin.JackpotPayout:N2}" };
                if (spin.Blackout != NeonVaultBlackout.None) { events.Add($"BLACKOUT · {spin.Blackout}"); }
                if (spin.Jackpot != NeonVaultJackpot.None) { events.Add($"JACKPOT · {spin.Jackpot}"); }
                if (spin.ExpandedReels.Count > 0) { events.Add("Expanded wild reels · " + string.Join(", ", spin.ExpandedReels.Select(i => i + 1))); }
                if (spin.SecurityOverrideReel is { } reel) { events.Add($"SECURITY OVERRIDE · Reel {reel + 1}"); }
                completed = new()
                {
                    Id = spin.SpinId, CompletedAt = spin.CompletedAt, Bet = spin.Bet, Stake = spin.Stake,
                    Before = spin.BankrollBefore, After = spin.BankrollAfter, Payout = spin.Payout,
                    Symbols = spin.FinalGrid.Select(symbol => (int)symbol).ToArray(),
                    HighlightCells = Enumerable.Range(0, spin.FinalGrid.Count).Where(i => spin.FinalGrid[i] is NeonVaultSymbol.Wild or NeonVaultSymbol.Scatter or NeonVaultSymbol.VaultKey).ToArray(),
                    Events = events.ToArray(), IsBonus = spin.IsFreeSpin, BonusAwarded = spin.FreeSpinsAwarded,
                    BonusRemaining = spin.FreeSpinsRemaining, Award = progression.Process(spin),
                };
            }
            else
            {
                NineLivesSession engine = NineLivesSession.Restore(Bankroll, Floor.Cats, nextInt);
                NineLivesSpinResult spin = engine.Spin(requestedBet);
                Floor.Cats = engine.Checkpoint;
                var events = new List<string> { $"Winning lines · {string.Join(", ", spin.WinningLines.Select(i => i + 1))}", $"Return multiplier · {spin.TotalMultiplier:N2}×" };
                if (spin.PounceWildCell is { } cell) { events.Add($"POUNCE WILD · Row {cell / 3 + 1}, reel {cell % 3 + 1}"); }
                if (spin.FullGridFrenzy) { events.Add("FULL GRID FRENZY · 5,000× or line total, whichever is higher"); }
                completed = new()
                {
                    Id = spin.SpinId, CompletedAt = spin.CompletedAt, Bet = spin.Bet, Stake = spin.Stake,
                    Before = spin.BankrollBefore, After = spin.BankrollAfter, Payout = spin.Payout,
                    Symbols = spin.Grid.Select(symbol => (int)symbol).ToArray(), HighlightCells = spin.WinningCells.Order().ToArray(),
                    Events = events.ToArray(), IsBonus = spin.IsBonusSpin, BonusAwarded = spin.BonusSpinsAwarded,
                    BonusRemaining = spin.BonusSpinsRemaining, Award = progression.Process(spin),
                };
            }
            ClearSettledCashJournal();
            state.Bankroll = completed.After;
            Floor.LastBets[(int)game] = completed.Bet;
            completed = completed with { Game = game, CommandId = command, RequestedBet = requestedBet };
            Floor.History.Insert(0, completed);
            if (Floor.History.Count > 20) { Floor.History.RemoveAt(20); }
        });
        return MobileJson.Deserialize<MobileSlotReceipt>(MobileJson.Serialize(completed!));
    }

    public void OpenCasinoVault() => Change(() =>
    {
        EnsureCashGameAvailable();
        _ = CasinoVaultService.SyncFromStatistics(Profile);
        CasinoVaultOpenResult receipt = CasinoVaultService.PrepareOpen(Profile);
        ClearSettledCashJournal();
        state.Bankroll = checked(Bankroll + receipt.TotalPayout);
        if (!CasinoVaultService.CompletePendingPayout(Profile, receipt.PayoutId))
        { throw new InvalidOperationException("The vault payout could not be committed."); }
        Floor.LastVaultOpening = receipt;
    });

    public void SetReducedSlotAnimations(bool reduced) => Change(() => Settings.ReduceSlotAnimations = reduced);

    private void EnsureCashGameAvailable()
    {
        if (IsRoundActive || IsDiceTableActive)
        { throw new InvalidOperationException("Finish your saved Blackjack round or paid Dice table before using another cash game or opening the Vault."); }
    }
    private void ClearSettledCashJournal()
    { state.Journal = null; Round = null; state.LastAward = ProgressionAward.None; }

    private void InitializeCasinoFloor()
    {
        if (state.CasinoFloor is not null) { return; }
        state.CasinoFloor = new();
        // Enabling the Vault establishes an observation baseline, never a payout
        // for wagering that happened before this integration existed.
        Profile.Vault.ObservedEligibleWager = CasinoVaultService.TotalEligibleWager(Profile);
    }

    private void ValidateCasinoFloor()
    {
        if (Floor.Version != 1 || Floor.Neon?.Version is not (null or 1) || Floor.Cats?.Version is not (null or 1))
        { throw new NotSupportedException("This casino save requires a newer app version."); }
        try
        {
            if (Floor.Neon is null || Floor.Cats is null || Floor.History is null || Floor.History.Count > 20
                || Floor.LastBets is not { Length: 3 } || Floor.History.Any(item => item is null)
                || Profile.Vault.PendingPayoutId != Guid.Empty)
            { throw new ArgumentException("Missing casino checkpoint or unresolved vault payout."); }
            _ = NeonVaultSession.Restore(Bankroll, Floor.Neon);
            _ = NineLivesSession.Restore(Bankroll, Floor.Cats);
            for (int i = 0; i < 3; i++) { ValidateSlotWager((MobileSlotGame)i, Floor.LastBets[i]); }
            if (HasSlotBonus && (state.Journal is { Settled: false } || IsDiceTableActive))
            { throw new ArgumentException("A bonus overlaps another active paid game."); }
            if (Floor.History.Select(item => item.Id).Distinct().Count() != Floor.History.Count
                || Floor.History.Select(item => item.CommandId).Distinct().Count() != Floor.History.Count)
            { throw new ArgumentException("Duplicate spin receipts."); }
            foreach (MobileSlotReceipt receipt in Floor.History)
            {
                ValidateSlotWager(receipt.Game, receipt.Bet);
                if (receipt.Id == Guid.Empty || receipt.CommandId == Guid.Empty || receipt.Before < 0 || receipt.After < 0
                    || receipt.Payout < 0 || receipt.After != receipt.Before - receipt.Stake + receipt.Payout
                    || receipt.Stake != (receipt.IsBonus ? 0m : receipt.Bet) || receipt.Before < receipt.Stake
                    || receipt.IsBonus && receipt.Game == MobileSlotGame.Reels
                    || receipt.BonusAwarded < 0 || receipt.BonusRemaining < 0 || receipt.Award is null
                    || receipt.Symbols is null || receipt.HighlightCells is null || receipt.Events is null
                    || receipt.Events.Length > 12 || receipt.Events.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 300)
                    || receipt.Symbols.Length != (receipt.Game == MobileSlotGame.Reels ? 3 : receipt.Game == MobileSlotGame.NeonVault ? 20 : 9)
                    || receipt.Symbols.Any(value => receipt.Game == MobileSlotGame.Reels ? !Enum.IsDefined((SlotSymbol)value)
                        : receipt.Game == MobileSlotGame.NeonVault ? !Enum.IsDefined((NeonVaultSymbol)value) : !Enum.IsDefined((NineLivesSymbol)value))
                    || receipt.HighlightCells.Any(i => i < 0 || i >= receipt.Symbols.Length))
                { throw new ArgumentException("Invalid spin receipt."); }
            }
            if (Floor.LastVaultOpening is { } opening && (opening.PayoutId == Guid.Empty || opening.BaseAmount <= 0
                || opening.BonusAmount < 0 || opening.TotalPayout != opening.BaseAmount + opening.BonusAmount
                || !CasinoVaultTierCatalog.All.Any(tier => tier.Id == opening.TierId)))
            { throw new ArgumentException("Invalid vault receipt."); }
        }
        catch (ArgumentException exception) { throw new InvalidDataException("Invalid saved Casino Floor.", exception); }
    }

    private static void ValidateSlotWager(MobileSlotGame game, decimal wager)
    {
        if (!Enum.IsDefined(game) || wager < 1 || decimal.Round(wager, 2) != wager
            || wager > (game == MobileSlotGame.Reels ? SlotMachineRules.MaximumBet : game == MobileSlotGame.NeonVault ? NeonVaultRules.MaximumBet : NineLivesRules.MaximumBet)
            || game == MobileSlotGame.NineLives && !NineLivesRules.Bets.Contains(wager))
        { throw new ArgumentException("Invalid saved slot wager."); }
    }
}
