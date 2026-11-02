using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class DiceGamePage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly bool casino;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(18, 20), Spacing = 10, MaximumWidthRequest = 760, HorizontalOptions = LayoutOptions.Center };
    private readonly List<View> diceViews = [];
    private readonly VerticalStackLayout tray = new() { Padding = new Thickness(18,10), Spacing = 10 };
    private readonly VerticalStackLayout dock = new() { Padding = new Thickness(16,10), Spacing = 7 };
    private readonly Grid root = new() { RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Star }, new RowDefinition { Height = GridLength.Auto } } };
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
        tray.Background = Gradient("#19372B", "#10251E"); dock.BackgroundColor = Color.FromArgb("#0C1F18");
        root.Add(tray); root.Add(new ScrollView { Content = body },0,1); root.Add(dock,0,2); Content = root;
        Render();
    }

    protected override bool OnBackButtonPressed() => busy || base.OnBackButtonPressed();

    private void Render()
    {
        body.Children.Clear(); tray.Children.Clear(); dock.Children.Clear();
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
        tray.Add(Columns(Metric(casino ? "CASINO TABLE" : "CASUAL TABLE", "Midnight Dice"), Metric($"TURN {Math.Min(game.FilledCount+1,13)}/13", game.Total + " pts", Gold)));
        if (message is not null) { tray.Add(Text(message,12,Gold)); }
        if (celebration is not null) { tray.Add(Pill(celebration,Gold)); }
        if (complete)
        {
            Results(game, table);
        }
        else
        {
            AddDice(game);
            if (game.CanScore && game.Dice.Distinct().Count() == 1)
            {
                tray.Add(Box(Text(game.IsAdditionalKniffel ? "FIVE OF A KIND · BONUS!\nCommit for +50 and the maximum in any open box."
                    : "FIVE OF A KIND!\nChoose your box to record this roll.", 20, Gold), true));
            }
            var roll = Button(game.RollsUsed == 0 ? "ROLL FIVE DICE" : $"ROLL AGAIN · {3 - game.RollsUsed} LEFT", "RollDice", async (_, _) => await Roll(), true);
            roll.IsEnabled = !busy && game.CanRoll;
            dock.Add(roll);
            tray.Add(Text(game.RollsUsed == 0 ? "Roll to begin. Your score previews appear below." : $"ROLL {game.RollsUsed}/3 · Tap dice to hold · Choose a box below", 11, Muted));
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
        tray.Add(dice);
    }

    private void AddSheet(KniffelGame game, bool playable)
    {
        var sheet = new VerticalStackLayout { Spacing = 0 };
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            if (category == KniffelCategory.Ones || category == KniffelCategory.ThreeOfAKind)
            { var caption = Text(category == KniffelCategory.Ones ? "UPPER SECTION · 63 FOR A +35 BONUS" : "LOWER SECTION",9,Gold); caption.Margin = new Thickness(12,10); sheet.Add(caption); }
            int? recorded = game.ScoreFor(category);
            int? preview = game.CanScore && !recorded.HasValue ? game.Preview(category) : null;
            var row = new Grid { Padding = new Thickness(12,8), HeightRequest = 43, ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 70 } } };
            row.Add(Text(Name(category),14,recorded.HasValue ? Muted : Cream));
            string score = recorded.HasValue ? recorded.Value.ToString() : preview.HasValue ? "+"+preview : "—";
            var number = Text(score,17,recorded.HasValue ? Gold : preview > 0 ? Win : Muted); number.HorizontalTextAlignment = TextAlignment.End; number.FontAttributes = FontAttributes.Bold; row.Add(number,1);
            var frame = new Border { Content = row, BackgroundColor = Color.FromArgb((int)category % 2 == 0 ? "#142E24" : "#10271F"), StrokeThickness = .4, Stroke = new SolidColorBrush(Color.FromArgb("#2C493A")), AutomationId = "DiceScore-"+category };
            if (playable && preview.HasValue && !busy)
            {
                var tap = new TapGestureRecognizer(); tap.Tapped += async (_,_) => { if (preview==0) { zeroCategory=category; Render(); } else { await Score(category); } }; frame.GestureRecognizers.Add(tap);
            }
            sheet.Add(frame);
        }
        var border = Box(sheet); border.Padding = 0; body.Add(border);
        if (zeroCategory is { } zero && playable)
        {
            dock.Add(Text($"Record zero in {Name(zero)}?",14,Gold));
            var confirm = Button("RECORD ZERO", "ConfirmDiceZero", async (_,_) => { if (!busy) { await Score(zero); } }); confirm.IsEnabled=!busy;
            dock.Add(Columns(confirm, Button("KEEP BOX", "CancelDiceZero", (_,_) => { zeroCategory=null; Render(); })));
        }
    }

    private void Opponents(KniffelTableState table)
    {
        var row = new Grid { ColumnSpacing = 6 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        var own = Button("YOU\n" + KniffelGame.Restore(table.Player).Total, "DiceOwnSheet", (_,_) => { opponentSheet=null; Render(); },opponentSheet is null); own.FontSize=10; own.Padding=4; row.Add(own);
        for (int i=0;i<3;i++)
        {
            int seat=i; row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var button=Button(KniffelTable.Names[i].ToUpperInvariant()+"\n"+KniffelGame.Restore(table.Opponents[i]).Total,"DiceOpponent"+i,(_,_)=>{ opponentSheet=seat; zeroCategory=null; Render(); },opponentSheet==i);
            button.FontSize=10; button.Padding=4; button.IsEnabled=!busy; row.Add(button,i+1);
        }
        body.Add(row);
        foreach (string move in table.LastMoves.Take(3)) { body.Add(Text(move.Replace("Kniffel","Five of a Kind"),10,Muted)); }
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
            if (!session.Settings.ReduceDiceAnimations && !VisualMotion.Reduced())
            {
                View[] moving = diceViews.Where((_, index) => before is null || !before.Held[index]).ToArray();
                foreach (View die in moving) { die.Scale = .8; die.Rotation = -200; die.TranslationY = -10; }
                try
                {
                    await Task.WhenAll(moving.SelectMany(die => new[]
                    { die.ScaleToAsync(1, 440, Easing.BounceOut), die.RotateToAsync(0, 520, Easing.CubicOut), die.TranslateToAsync(0,0,520,Easing.BounceOut) }));
                }
                finally { foreach (View die in moving) { die.Scale = 1; die.Rotation = 0; die.TranslationY = 0; } }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message.Split('\n')[0]; }
        finally { busy = false; Render(); }
        KniffelState? result = casino ? session.DiceTable?.Player : session.CasualDice;
        if (result is { RollsUsed: > 0 } && result.Dice.Distinct().Count()==1) { VisualMotion.Celebrate(root,Gold); }
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
