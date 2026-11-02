using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class DiceGamePage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly bool casino;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(18, 20), Spacing = 16, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private readonly List<View> diceViews = [];
    private bool busy;
    private bool forfeit;
    private KniffelCategory? zeroCategory;
    private string? message;
    private string? celebration;
    private int? opponentSheet;

    public DiceGamePage(BlackjackSession session, bool casino)
    {
        this.session = session;
        this.casino = casino;
        Title = casino ? "Midnight Dice · Casino" : "Midnight Dice · Casual";
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
        Render();
    }

    protected override bool OnBackButtonPressed() => busy || base.OnBackButtonPressed();

    private void Render()
    {
        body.Children.Clear();
        diceViews.Clear();
        KniffelTableState? table = casino ? session.DiceTable : null;
        KniffelState? saved = casino ? table?.Player : session.CasualDice;
        if (saved is null)
        {
            body.Add(Text("Return to the dice room to start a game.", 20, Gold));
            body.Add(Button("BACK", "DiceGameBack", async (_, _) => await Navigation.PopAsync()));
            return;
        }
        KniffelGame game = KniffelGame.Restore(saved);
        bool complete = casino ? table!.Settled : game.IsComplete;
        body.Add(Text(casino ? "THE CASINO TABLE" : "YOUR CASUAL TABLE", 14, Gold));
        body.Add(Text($"{session.Profile.DisplayName} · {game.Total} POINTS", 26));
        body.Add(Text(complete ? "GAME COMPLETE" : $"Turn {game.FilledCount + 1} / 13 · Roll {game.RollsUsed} / 3", 18, Gold));
        if (casino) { body.Add(Text($"Entry paid · {Cash(table!.Stake)} · Available cash {Cash(session.Bankroll)}", 15, Muted)); }
        if (message is not null) { body.Add(Box(Text(message, 16, Gold))); }
        if (celebration is not null) { body.Add(Box(Text(celebration, 22, Gold), true)); }
        if (busy)
        {
            body.Add(new ActivityIndicator { IsRunning = true, Color = Gold, HeightRequest = 36 });
            body.Add(Text(casino ? "Saving your turn and playing the opponents…" : "Saving your turn…", 16, Muted));
        }
        if (complete)
        {
            Results(game, table);
        }
        else
        {
            AddDice(game);
            if (game.CanScore && game.Dice.Distinct().Count() == 1)
            {
                body.Add(Box(Text(game.IsAdditionalKniffel ? "FIVE OF A KIND · BONUS!\nCommit for +50 and the maximum in any open box."
                    : "FIVE OF A KIND!\nChoose your box to record this roll.", 20, Gold), true));
            }
            var roll = Button(game.RollsUsed == 0 ? "ROLL FIVE DICE" : $"ROLL AGAIN · {3 - game.RollsUsed} LEFT", "RollDice", async (_, _) => await Roll(), true);
            roll.IsEnabled = !busy && game.CanRoll;
            body.Add(roll);
            body.Add(Text(game.RollsUsed == 0 ? "Roll to preview scores in every open box."
                : game.RollsUsed == 3 ? "Choose an open score box to finish this turn."
                : "Tap a die to hold or release it. Gold borders mark held dice. Choose a score box whenever you are ready.", 15, Muted));
        }
        if (table is not null) { Opponents(table); }
        body.Add(Text(opponentSheet is null ? "YOUR SCORE SHEET" : KniffelTable.Names[opponentSheet.Value].ToUpperInvariant() + " · SCORE SHEET", 14, Gold));
        KniffelGame sheet = opponentSheet is { } seat && table is not null
            ? KniffelGame.Restore(table.Opponents[seat]) : game;
        AddSheet(sheet, !complete && opponentSheet is null);
        var totals = new VerticalStackLayout { Spacing = 8 };
        totals.Add(Text($"Upper section · {sheet.UpperSubtotal} / 63 · Bonus {sheet.UpperBonus}", 16));
        totals.Add(Text($"Lower section · {sheet.LowerSubtotal} · Extra five-of-a-kind bonus {sheet.AdditionalKniffelBonus}", 16, Muted));
        totals.Add(Text($"TOTAL · {sheet.Total}", 24, Gold));
        body.Add(Box(totals));
        if (casino && !complete)
        {
            if (forfeit)
            {
                body.Add(Text($"Forfeit and lose your {Cash(table!.Stake)} entry fee? No completion XP will be awarded.", 17, Loss));
                var confirm = Button("CONFIRM FORFEIT", "ConfirmDiceForfeit", (_, _) =>
                {
                    if (busy) { return; }
                    try { session.ForfeitDiceTable(); message = null; forfeit = false; }
                    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message; }
                    Render();
                });
                confirm.IsEnabled = !busy;
                body.Add(confirm);
                body.Add(Button("KEEP PLAYING", "CancelDiceForfeit", (_, _) => { forfeit = false; Render(); }));
            }
            else { body.Add(Button("FORFEIT TABLE", "ForfeitDiceTable", (_, _) => { if (!busy) { forfeit = true; Render(); } })); }
        }
        body.Add(Text(complete ? "Result and rewards saved. Return to the dice room to start another game."
            : "Your dice and score sheet save after every action. You can leave and resume later.", 14, Muted));
        var back = Button(complete ? "BACK TO DICE ROOM" : "LEAVE · GAME SAVED", "DiceGameBack", async (_, _) =>
        { if (!busy) { await Navigation.PopAsync(); } });
        back.IsEnabled = !busy;
        body.Add(back);
    }

    private void AddDice(KniffelGame game)
    {
        string variant = CosmeticCatalog.Get(session.Profile.EquippedDiceSetId).VisualVariant;
        var dice = new Grid { ColumnSpacing = 6 };
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            dice.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var cell = new VerticalStackLayout { Spacing = 5 };
            Border die = DiceFace.Create(game.Dice[i], variant, game.Held[i]);
            die.AutomationId = "Die" + (i + 1);
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (busy || !game.CanScore || game.RollsUsed >= 3) { return; }
                try { session.HoldDie(casino, index); message = null; }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message; }
                Render();
            };
            die.GestureRecognizers.Add(tap);
            diceViews.Add(die);
            cell.Add(die);
            Label hold = Text(game.Held[i] ? "HELD" : $"DIE {i + 1}", 11, game.Held[i] ? Gold : Muted);
            hold.HorizontalTextAlignment = TextAlignment.Center;
            cell.Add(hold);
            dice.Add(cell, i, 0);
        }
        body.Add(dice);
        body.Add(Text(CosmeticCatalog.Get(session.Profile.EquippedDiceSetId).Name, 13, Muted));
    }

    private void AddSheet(KniffelGame game, bool playable)
    {
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            int? recorded = game.ScoreFor(category);
            if (recorded.HasValue)
            {
                body.Add(Box(Text($"{Name(category)} · {recorded.Value} points", 16, Muted)));
                continue;
            }
            int? preview = game.CanScore ? game.Preview(category) : null;
            var choose = Button($"{Name(category)} · {(preview.HasValue ? "+" + preview : "—")}", "DiceScore-" + category, async (_, _) =>
            {
                if (busy) { return; }
                if (preview == 0) { zeroCategory = category; Render(); }
                else { await Score(category); }
            });
            choose.IsEnabled = playable && game.CanScore && !busy;
            if (preview > 0 && playable) { choose.TextColor = Win; }
            body.Add(choose);
            if (playable && zeroCategory == category)
            {
                body.Add(Text($"Record zero in {Name(category)} and close this box?", 16, Gold));
                var confirm = Button("RECORD ZERO", "ConfirmDiceZero", async (_, _) => { if (!busy) { await Score(category); } });
                confirm.IsEnabled = !busy;
                body.Add(confirm);
                body.Add(Button("KEEP BOX OPEN", "CancelDiceZero", (_, _) => { zeroCategory = null; Render(); }));
            }
        }
    }

    private void Opponents(KniffelTableState table)
    {
        body.Add(Text(table.Settled ? "FINAL STANDINGS" : "TABLE STANDINGS · LIVE", 14, Gold));
        var players = table.Opponents.Select((state, index) => (Name: KniffelTable.Names[index], Game: KniffelGame.Restore(state)))
            .Append((Name: session.Profile.DisplayName + " (you)", Game: KniffelGame.Restore(table.Player)))
            .OrderByDescending(item => item.Game.Total).ToArray();
        int place = 0, prior = -1;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].Game.Total != prior) { place = i + 1; prior = players[i].Game.Total; }
            body.Add(Text($"{place}. {players[i].Name} · {players[i].Game.Total} points", 17));
        }
        foreach (string move in table.LastMoves)
        { body.Add(Text(move.Replace("Kniffel", "Five of a Kind").Replace("ThreeOfAKind", "Three of a Kind").Replace("FourOfAKind", "Four of a Kind")
            .Replace("SmallStraight", "Small Straight").Replace("LargeStraight", "Large Straight").Replace("FullHouse", "Full House"), 14, Muted)); }
        var own = Button("YOUR SCORE SHEET", "DiceOwnSheet", (_, _) => { opponentSheet = null; Render(); });
        own.IsEnabled = !busy;
        body.Add(own);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var inspect = Button("INSPECT " + KniffelTable.Names[i].ToUpperInvariant(), "DiceOpponent" + i,
                (_, _) => { opponentSheet = index; zeroCategory = null; Render(); });
            inspect.IsEnabled = !busy;
            body.Add(inspect);
        }
    }

    private void Results(KniffelGame game, KniffelTableState? table)
    {
        ProgressionAward award = casino ? session.DiceTableAward : session.CasualDiceAward;
        var result = new VerticalStackLayout { Spacing = 12 };
        if (table is not null)
        {
            result.Add(Text(table.Forfeited ? "TABLE FORFEITED" : $"FINISHED · PLACE {table.Placement}", 26, Gold));
            result.Add(Text($"Entry {Cash(table.Stake)} · Returned {Cash(table.Payout)}", 18));
            decimal net = table.Payout - table.Stake;
            result.Add(Text($"NET RESULT · {Net(net)}", 24, net >= 0 ? Win : Loss));
            if (table.Forfeited) { result.Add(Text("Entry fee lost. No game-completion rewards.", 16, Muted)); }
        }
        else { result.Add(Text($"CASUAL COMPLETE · {game.Total} POINTS", 26, Gold)); }
        result.Add(Text($"Score {game.Total} · Upper bonus {game.UpperBonus} · Five of a Kind {game.KniffelsScored}", 17));
        if (award.PlayerExperience > 0)
        {
            result.Add(Text($"+{award.PlayerExperience} XP · +{award.SeasonExperience} season XP\n+{award.CoinsEarned} coins from progression", 18, Gold));
            if (award.HasLevelUp) { result.Add(Text($"LEVEL UP · {session.Profile.Level}", 22, Win)); }
        }
        result.Add(Text("Rewards are already saved and will not be granted again when reopening this result.", 14, Muted));
        body.Add(Box(result, true));
        body.Add(Button("VIEW CHALLENGES & REWARDS", "DiceResultChallenges", async (_, _) => await Navigation.PushAsync(new PlayerHubPage(session, HubSection.Challenges))));
    }

    private async Task Roll()
    {
        if (busy) { return; }
        busy = true;
        try
        {
            KniffelState? before = casino ? session.DiceTable?.Player : session.CasualDice;
            session.RollDice(casino);
            message = null; celebration = null; zeroCategory = null;
            Render();
            if (!session.Settings.ReduceDiceAnimations)
            {
                View[] moving = diceViews.Where((_, index) => before is null || !before.Held[index]).ToArray();
                foreach (View die in moving) { die.Scale = .88; die.Rotation = -14; }
                try
                {
                    await Task.WhenAll(moving.SelectMany(die => new[]
                    { die.ScaleToAsync(1, 220, Easing.CubicOut), die.RotateToAsync(0, 260, Easing.CubicOut) }));
                }
                finally { foreach (View die in moving) { die.Scale = 1; die.Rotation = 0; } }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message.Split('\n')[0]; }
        finally { busy = false; Render(); }
    }

    private async Task Score(KniffelCategory category)
    {
        if (busy) { return; }
        busy = true;
        KniffelTableState? before = casino ? session.DiceTable : null;
        try
        {
            Render();
            await session.ScoreDiceAsync(casino, category);
            message = null; celebration = null; zeroCategory = null; opponentSheet = null;
            if (before is not null && session.DiceTable is { } after)
            {
                string[] names = after.Opponents.Select((state, index) => state.KniffelsScored > before.Opponents[index].KniffelsScored
                    ? KniffelTable.Names[index] : null).OfType<string>().ToArray();
                if (names.Length > 0) { celebration = $"FIVE OF A KIND!\n{string.Join(" · ", names)}"; }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message.Split('\n')[0]; }
        finally { busy = false; Render(); }
    }

    internal static string Name(KniffelCategory category) => category switch
    {
        KniffelCategory.ThreeOfAKind => "Three of a Kind", KniffelCategory.FourOfAKind => "Four of a Kind",
        KniffelCategory.FullHouse => "Full House", KniffelCategory.SmallStraight => "Small Straight",
        KniffelCategory.LargeStraight => "Large Straight", KniffelCategory.Kniffel => "Five of a Kind", _ => category.ToString(),
    };
}
