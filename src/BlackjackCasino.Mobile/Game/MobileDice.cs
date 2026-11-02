using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Mobile.Game;

public sealed partial class BlackjackSession
{
    public bool IsDiceTableActive => KniffelTable.IsActive(state.DiceTable);
    public bool HasCasualDice => state.CasualDice is { Entries.Length: < 13 };
    // Callers receive detached checkpoints, never the live arrays or settlement receipt.
    public KniffelState? CasualDice => state.CasualDice is null ? null : KniffelGame.Restore(state.CasualDice).Snapshot;
    public KniffelTableState? DiceTable => state.DiceTable is null ? null
        : MobileJson.Deserialize<KniffelTableState>(MobileJson.Serialize(state.DiceTable));
    public ProgressionAward CasualDiceAward => state.CasualDiceAward;
    public ProgressionAward DiceTableAward => state.DiceTableAward;

    public void StartCasualDice(bool replaceActive = false) => Change(() =>
    {
        if (HasCasualDice && !replaceActive) { throw new InvalidOperationException("Resume your casual game or confirm starting over."); }
        state.CasualDice = new KniffelGame().Snapshot;
        state.CasualDiceAward = ProgressionAward.None;
    });

    public void StartDiceTable(decimal stake) => Change(() =>
    {
        if (IsRoundActive) { throw new InvalidOperationException("Finish your saved Blackjack round before entering a casino table. Casual Dice remains available."); }
        KniffelCashChange entry = KniffelCash.Start(Bankroll, state.DiceTable, stake, _ => { });
        // A completed Blackjack journal contains its old wallet total. Retain history,
        // but clear that journal before adopting a different game's wallet transaction.
        state.Journal = null;
        Round = null;
        state.LastAward = ProgressionAward.None;
        state.Bankroll = entry.Bankroll;
        state.DiceTable = entry.Table;
        state.DiceTableAward = new KniffelProgression(new ProgressionService(Profile)).ProcessTable(entry.Table);
    });

    public void RollDice(bool casino, Func<int>? nextDie = null) => Change(() =>
    {
        KniffelGame game = ActiveDice(casino, nextDie);
        game.Roll();
        SaveDice(casino, game.Snapshot);
    });

    public void HoldDie(bool casino, int index) => Change(() =>
    {
        KniffelGame game = ActiveDice(casino);
        game.ToggleHold(index);
        SaveDice(casino, game.Snapshot);
    });

    public async Task ScoreDiceAsync(bool casino, KniffelCategory category)
    {
        KniffelGame game = ActiveDice(casino);
        game.Commit(category);
        if (!casino)
        {
            Change(() =>
            {
                state.CasualDice = game.Snapshot;
                if (game.IsComplete)
                { state.CasualDiceAward = new KniffelProgression(new ProgressionService(Profile)).Process(game.Snapshot); }
            });
            return;
        }

        KniffelTableState before = DiceTable!;
        KniffelTableState pending = KniffelTable.WithPlayer(before, game.Snapshot);
        // The bounded Core AI search runs off the UI thread. Nothing is adopted or
        // displayed until all seats, cash, rewards and receipt have saved together.
        KniffelTableState next = await Task.Run(() => KniffelTable.Advance(pending));
        Change(() =>
        {
            if (MobileJson.Serialize(state.DiceTable) != MobileJson.Serialize(before))
            { throw new InvalidOperationException("The table changed while the opponents played. Reopen it to continue."); }
            KniffelCashChange result = KniffelCash.Apply(Bankroll, pending, next, _ => { });
            state.Bankroll = result.Bankroll;
            state.DiceTable = result.Table;
            state.DiceTableAward = new KniffelProgression(new ProgressionService(Profile)).ProcessTable(result.Table);
        });
    }

    public void ForfeitDiceTable() => Change(() =>
    {
        KniffelTableState current = state.DiceTable ?? throw new InvalidOperationException("There is no casino table to forfeit.");
        KniffelTableState next = KniffelTable.Forfeit(current);
        KniffelCashChange result = KniffelCash.Apply(Bankroll, current, next, _ => { });
        state.Bankroll = result.Bankroll;
        state.DiceTable = result.Table;
        state.DiceTableAward = new KniffelProgression(new ProgressionService(Profile)).ProcessTable(next);
    });

    public void SetReducedDiceAnimations(bool reduced) => Change(() => Settings.ReduceDiceAnimations = reduced);

    public void PurchaseDiceSet(string id) => Change(() =>
    {
        if (CosmeticCatalog.TryGet(id) is not { Kind: CosmeticKind.DiceSet })
        { throw new InvalidOperationException("Choose a dice set from this shop."); }
        var service = new ProgressionService(Profile);
        var result = service.PurchaseCosmetic(id);
        if (!result.Success) { throw new InvalidOperationException(result.Message); }
        if (!service.EquipCosmetic(id)) { throw new InvalidOperationException("The dice set could not be equipped."); }
    });

    public void EquipDiceSet(string id) => Change(() =>
    {
        if (CosmeticCatalog.TryGet(id) is not { Kind: CosmeticKind.DiceSet }
            || !new ProgressionService(Profile).EquipCosmetic(id))
        { throw new InvalidOperationException("Unlock this dice set before equipping it."); }
    });

    private KniffelGame ActiveDice(bool casino, Func<int>? nextDie = null)
    {
        KniffelState? saved = casino ? state.DiceTable?.Player : state.CasualDice;
        if (saved is null || casino && !IsDiceTableActive || saved.Entries.Length == 13)
        { throw new InvalidOperationException("Start or resume an unfinished Midnight Dice game first."); }
        return KniffelGame.Restore(saved, nextDie);
    }

    private void SaveDice(bool casino, KniffelState game)
    {
        if (casino) { state.DiceTable = KniffelTable.WithPlayer(state.DiceTable!, game); }
        else { state.CasualDice = game; }
    }

    private void ValidateDice()
    {
        if (state.CasualDice?.Version is not (null or 1) || state.DiceTable?.Version is not (null or 1))
        { throw new NotSupportedException("This Midnight Dice save requires a newer app version."); }
        try
        {
            if (state.CasualDice is { } casual)
            {
                _ = KniffelGame.Restore(casual);
                if (casual.GameId == Guid.Empty || casual.Entries.Length == 13
                    && !Profile.Kniffel.RewardedGameIds.Contains(casual.GameId))
                { throw new ArgumentException("Missing casual settlement receipt."); }
            }
            if (state.DiceTable is { } table)
            {
                if (table.Player is null || table.Opponents is null || table.Opponents.Any(item => item is null))
                { throw new ArgumentException("Missing casino score sheet."); }
                KniffelTable.Validate(table);
                if (table.LastMoves is null || table.LastMoves.Length is not (0 or 3)
                    || KniffelTable.NeedsOpponents(table)
                    || !Profile.Kniffel.EnteredTableIds.Contains(table.Player.GameId)
                    || table.Settled != Profile.Kniffel.SettledTableIds.Contains(table.Player.GameId)
                    || table.Settled && !table.Forfeited && !Profile.Kniffel.RewardedGameIds.Contains(table.Player.GameId)
                    || !table.Settled && state.Journal is { Settled: false })
                { throw new ArgumentException("Casino table, wallet reservation and reward receipts do not agree."); }
            }
            if (state.CasualDiceAward is null || state.DiceTableAward is null)
            { throw new ArgumentException("Missing dice reward summary."); }
        }
        catch (ArgumentException exception) { throw new InvalidDataException("Invalid saved Midnight Dice game.", exception); }
    }
}
