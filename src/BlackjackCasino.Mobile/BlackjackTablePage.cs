using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class BlackjackTablePage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout table = new()
    {
        Padding = new Thickness(20, 24), Spacing = 14, MaximumWidthRequest = 760,
        HorizontalOptions = LayoutOptions.Center,
    };
    private readonly VerticalStackLayout header = new() { Padding = new Thickness(18, 10), Spacing = 5 };
    private readonly VerticalStackLayout dock = new() { Padding = new Thickness(16, 10), Spacing = 8 };
    private readonly Dictionary<string, (Card? Face, View View)> cardViews = [];
    private readonly Grid root = new() { RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Star }, new RowDefinition { Height = GridLength.Auto } } };
    private string betText = "25";
    private string? error;
    private bool busy;
    private bool surrenderPending;

    public BlackjackTablePage(BlackjackSession session)
    {
        this.session = session;
        betText = session.Settings.LastWager.ToString(CultureInfo.InvariantCulture);
        Title = "Blackjack";
        BackgroundColor = Ink;
        var felt = new GraphicsView { Drawable = new FeltArt(), InputTransparent = true }; Grid.SetRowSpan(felt,3); root.Add(felt);
        header.BackgroundColor = Color.FromArgb("#0D211C"); dock.Background = Gradient("#142D24", "#091B15");
        root.Add(header); root.Add(new ScrollView { Content = table }, 0, 1); root.Add(dock, 0, 2); Content = root;
        Render();
    }

    private void Render()
    {
        table.Children.Clear(); dock.Children.Clear(); header.Children.Clear(); cardViews.Clear();
        header.Add(CasinoStyle.Columns(Metric("YOUR TABLE", "Blackjack"), Metric("AVAILABLE CASH", Cash(session.AvailableCash), Gold)));
        if (session.IsRoundActive)
        {
            header.Add(Text($"{Cash(session.Committed)} in play · 6 decks · S17 · Blackjack pays 3:2", 10, Muted));
        }
        if (error is not null)
        {
            var message = Text(error, 16, Loss);
            message.AutomationId = "TableError";
            table.Add(message);
        }

        BlackjackRound? round = session.Round;
        if (round?.Result is { } result)
        {
            string headline = result.TotalNet > 0 ? $"YOU WIN {Cash(result.TotalNet)}"
                : result.TotalNet < 0 ? $"ROUND LOST · {Cash(-result.TotalNet)}" : "ROUND EVEN · NO NET CHANGE";
            var summary = Text(headline, 23, result.TotalNet >= 0 ? Win : Loss);
            summary.AutomationId = "RoundResult";
            SemanticProperties.SetDescription(summary, $"Round complete. Net result {Net(result.TotalNet)}.");
            table.Add(Box(summary));
            if (session.LastAward.PlayerExperience > 0)
            {
                var award = session.LastAward;
                table.Add(Text($"+{award.PlayerExperience} XP · +{award.SeasonExperience} season XP · +{award.CoinsEarned} coins", 16, Gold));
                if (award.HasLevelUp) { table.Add(Text($"LEVEL UP · Level {session.Profile.Level}", 22, Win)); }
            }
        }
        if (!session.IsRoundActive) { AddBettingControls(); }
        if (round is null)
        {
            var welcome = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(0, 30), HorizontalOptions = LayoutOptions.Center };
            welcome.Add(PlayingCardView.Preview(session.Profile.EquippedCardSetId)); welcome.Add(Text("Your seat is waiting.", 27, Gold));
            welcome.Add(Text("BLACKJACK PAYS 3 TO 2", 10, Gold)); table.Add(welcome);
        }
        if (round is not null)
        {
            AddDealer(round);
            for (int i = 0; i < round.PlayerHands.Count; i++)
            {
                BlackjackHand hand = round.PlayerHands[i];
                bool active = round.Phase == RoundPhase.PlayerTurn && i == round.ActiveHandIndex;
                var content = new VerticalStackLayout { Spacing = 12 };
                string title = round.PlayerHands.Count == 1 ? "YOUR HAND" : $"HAND {i + 1}";
                content.Add(Text($"{title}{(active ? " · YOUR TURN" : "")} · {Cash(hand.Bet)}", 14, active ? Gold : Muted));
                content.Add(CardRow(hand.Cards.Select(card => (Card?)card), "hand" + i));
                content.Add(Text(HandTotal(hand), 21, hand.Value.IsBust ? Loss : Colors.White));
                if (round.Result is { } settled)
                {
                    HandResult outcome = settled.Hands[i];
                    content.Add(Text($"{Outcome(outcome.Outcome)} · {Net(outcome.Net)}", 17,
                        outcome.Net >= 0 ? Win : Loss));
                }
                table.Add(Box(content, active));
            }
            if (round.Phase == RoundPhase.InsuranceDecision) { AddInsurance(round); }
            if (round.Phase == RoundPhase.PlayerTurn) { AddPlayerActions(); }
            if (round.Phase == RoundPhase.Settled && round.InsuranceBet > 0)
            {
                table.Add(Text($"Insurance · {Net(round.Result!.InsuranceNet)}", 15, Muted));
            }
        }
        
        table.Add(Button("LEAVE TABLE", "BackToLobby", async (_, _) => await Navigation.PopAsync()));
        if (session.IsRoundActive)
        {
            table.Add(Text("Your open round saves automatically, including after an app restart.", 13, Muted));
        }
    }

    private void AddBettingControls()
    {
        if (session.Bankroll < session.Rules.MinimumBet)
        {
            dock.Add(Text("Your play cash is below the minimum bet. Refill to take another seat.", 16, Muted));
            dock.Add(Button("REFILL PLAY CASH", "RefillCash", (_, _) => Run(session.RefillPracticeCash), true));
            return;
        }
        var entry = new Entry
        {
            Text = betText, Placeholder = "Bet · $1 to $1,000", Keyboard = Keyboard.Numeric,
            TextColor = Colors.White, PlaceholderColor = Muted, BackgroundColor = Panel,
            MinimumHeightRequest = 54, MaxLength = 8, AutomationId = "BetAmount",
        };
        SemanticProperties.SetDescription(entry, "Play wager in whole dollars, from 1 to 1000.");
        entry.TextChanged += (_, args) => betText = args.NewTextValue ?? "";
        dock.Add(Text("CHOOSE YOUR WAGER", 13, Gold));
        dock.Add(entry);
        var chips = new Grid { ColumnSpacing = 8, ColumnDefinitions = Columns(4) };
        int column = 0;
        foreach (int amount in new[] { 10, 25, 50, 100 })
        {
            var chip = Button($"${amount}", $"Bet{amount}", (_, _) => entry.Text = amount.ToString(CultureInfo.InvariantCulture));
            chip.IsEnabled = session.Bankroll >= amount;
            chips.Add(chip, column++, 0);
        }
        dock.Add(chips);
        dock.Add(Button(session.Round is null ? "DEAL CARDS" : "DEAL NEXT ROUND", "DealCards", (_, _) => Run(() =>
        {
            if (!decimal.TryParse(betText.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out decimal bet))
            {
                throw new InvalidOperationException("Enter a whole-dollar wager, for example 25.");
            }
            session.StartRound(bet);
        }), true));
    }

    private void AddDealer(BlackjackRound round)
    {
        var content = new VerticalStackLayout { Spacing = 12 };
        content.Add(Text("DEALER", 14, Muted));
        content.Add(CardRow(session.DealerCards, "dealer"));
        HandValue visible = session.VisibleDealerValue;
        string total = round.Phase == RoundPhase.Settled
            ? visible.IsBust ? $"Bust · {visible.Total}" : $"Total · {visible.Total}"
            : $"Showing · {visible.Total}";
        var label = Text(total, 21);
        label.AutomationId = "DealerVisibleTotal";
        content.Add(label);
        table.Add(Box(content));
    }

    private void AddInsurance(BlackjackRound round)
    {
        dock.Add(Text($"Dealer shows an Ace. Insurance costs {Cash(round.InitialBet / 2m)} and pays 2:1 if the dealer has Blackjack.", 16, Muted));
        var insurance = Button("TAKE INSURANCE", "TakeInsurance", (_, _) => Run(session.TakeInsurance));
        insurance.IsEnabled = session.CanTakeInsurance;
        dock.Add(insurance);
        dock.Add(Button("NO INSURANCE", "DeclineInsurance", (_, _) => Run(session.DeclineInsurance), true));
    }

    private void AddPlayerActions()
    {
        if (surrenderPending)
        {
            dock.Add(Text("Surrender this hand and lose half its wager?", 18, Gold));
            dock.Add(Button("CONFIRM SURRENDER", "ConfirmSurrender", (_, _) => Run(() =>
            { surrenderPending = false; session.Act(PlayerAction.Surrender); })));
            dock.Add(Button("KEEP PLAYING", "CancelSurrender", (_, _) => { surrenderPending = false; Render(); }, true));
            return;
        }
        IReadOnlySet<PlayerAction> available = session.AvailableActions();
        var grid = new Grid
        {
            ColumnDefinitions = Columns(2), RowSpacing = 10, ColumnSpacing = 10,
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } },
        };
        (string Label, PlayerAction Action)[] actions =
        [
            ("HIT", PlayerAction.Hit), ("STAND", PlayerAction.Stand),
            ("DOUBLE", PlayerAction.DoubleDown), ("SPLIT", PlayerAction.Split),
            ("SURRENDER", PlayerAction.Surrender),
        ];
        for (int i = 0; i < actions.Length; i++)
        {
            var (label, action) = actions[i];
            var button = Button(label, action.ToString(), (_, _) =>
            {
                if (action == PlayerAction.Surrender && session.Settings.ConfirmSurrender)
                { surrenderPending = true; Render(); }
                else { Run(() => session.Act(action)); }
            }, i < 2);
            button.IsEnabled = available.Contains(action);
            grid.Add(button, i % 2, i / 2);
            if (action == PlayerAction.Surrender) { Grid.SetColumnSpan(button, 2); }
        }
        dock.Add(grid);
    }

    private async void Run(Action command)
    {
        if (busy) { return; } busy = true;
        BlackjackRound? oldRound = session.Round;
        var before = cardViews.ToDictionary(item => item.Key, item => item.Value.Face);
        try
        {
            error = null; command(); Render();
            foreach (View button in dock.Children) { button.IsEnabled = false; }
            if (!VisualMotion.Reduced())
            {
                var moving = cardViews.Where(pair => !ReferenceEquals(oldRound, session.Round) || !before.TryGetValue(pair.Key, out Card? face) || !EqualityComparer<Card?>.Default.Equals(face, pair.Value.Face)).ToArray();
                int delay = 0;
                foreach (var pair in moving)
                {
                    View view = pair.Value.View; view.TranslationX = 28; view.TranslationY = -48; view.Rotation = -10; view.Opacity = 0;
                    await Task.Delay(delay == 0 ? 1 : 55); delay++;
                    await Task.WhenAll(view.TranslateToAsync(0,0,180,Easing.CubicOut), view.RotateToAsync(0,180), view.FadeToAsync(1,140));
                }
                if (session.Round?.Result is { TotalNet: > 0 }) { VisualMotion.Celebrate(root, Gold); }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { error = exception.Message.Split('\n')[0]; }
        finally { busy = false; Render(); }
    }

    private static ColumnDefinitionCollection Columns(int count)
    {
        var result = new ColumnDefinitionCollection();
        for (int i = 0; i < count; i++) { result.Add(new ColumnDefinition { Width = GridLength.Star }); }
        return result;
    }

    private View CardRow(IEnumerable<Card?> cards, string prefix)
    {
        var row = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(2, 5) }; int index = 0;
        foreach (Card? card in cards)
        {
            Border view = PlayingCardView.Create(card, session.Profile.EquippedCardSetId, session.Settings.LargeCards);
            cardViews[prefix + index++] = (card, view); row.Add(view);
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = row, HeightRequest = session.Settings.LargeCards ? 137 : 115 };
    }

    private sealed class FeltArt : Microsoft.Maui.Graphics.IDrawable
    {
        public void Draw(Microsoft.Maui.Graphics.ICanvas canvas, Microsoft.Maui.Graphics.RectF area)
        {
            canvas.StrokeColor = Gold.WithAlpha(.16f); canvas.StrokeSize = 1;
            canvas.DrawEllipse(-area.Width * .12f, area.Height * .08f, area.Width * 1.24f, area.Height * .78f);
            canvas.StrokeColor = Gold.WithAlpha(.06f);
            canvas.DrawEllipse(-area.Width * .14f, area.Height * .06f, area.Width * 1.28f, area.Height * .82f);
        }
    }

    private static string HandTotal(BlackjackHand hand) => hand.IsNaturalBlackjack ? "Blackjack · 21"
        : hand.IsSurrendered ? $"Surrendered · {hand.Value.Total}"
        : hand.Value.IsBust ? $"Bust · {hand.Value.Total}"
        : $"{(hand.Value.IsSoft ? "Soft total" : "Total")} · {hand.Value.Total}";

    private static string Outcome(HandOutcome outcome) => outcome switch
    {
        HandOutcome.Blackjack => "BLACKJACK · 3:2", HandOutcome.Win => "WIN", HandOutcome.Loss => "LOSS",
        HandOutcome.Push => "PUSH", HandOutcome.Surrender => "SURRENDER", _ => outcome.ToString(),
    };
}
