using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class BlackjackTablePage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout table = new()
    {
        Padding = new Thickness(20, 24), Spacing = 18, MaximumWidthRequest = 620,
        HorizontalOptions = LayoutOptions.Center,
    };
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
        Content = new ScrollView { Content = table };
        Render();
    }

    private void Render()
    {
        table.Children.Clear();
        table.Add(Text($"{session.Profile.DisplayName} · LEVEL {session.Profile.Level} · {session.Profile.Coins:N0} COINS", 13, Gold));
        table.Add(Text($"{Cash(session.AvailableCash)} available", 27, Gold));
        if (session.IsRoundActive)
        {
            table.Add(Text($"At the table · {Cash(session.Committed)}", 15, Muted));
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
                content.Add(CardRow(hand.Cards.Select(card => (Card?)card)));
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
        table.Add(Text("6 decks · Blackjack pays 3:2\nDealer stands on soft 17", 14, Muted));
        table.Add(Button("BACK TO LOBBY", "BackToLobby", async (_, _) => await Navigation.PopAsync()));
        if (session.IsRoundActive)
        {
            table.Add(Text("Your open round saves automatically, including after an app restart.", 13, Muted));
        }
    }

    private void AddBettingControls()
    {
        if (session.Bankroll < session.Rules.MinimumBet)
        {
            table.Add(Text("Your play cash is below the minimum bet. Refill to take another seat.", 16, Muted));
            table.Add(Button("REFILL PLAY CASH", "RefillCash", (_, _) => Run(session.RefillPracticeCash), true));
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
        table.Add(Text("CHOOSE YOUR WAGER", 13, Gold));
        table.Add(entry);
        var chips = new Grid { ColumnSpacing = 8, ColumnDefinitions = Columns(4) };
        int column = 0;
        foreach (int amount in new[] { 10, 25, 50, 100 })
        {
            var chip = Button($"${amount}", $"Bet{amount}", (_, _) => entry.Text = amount.ToString(CultureInfo.InvariantCulture));
            chip.IsEnabled = session.Bankroll >= amount;
            chips.Add(chip, column++, 0);
        }
        table.Add(chips);
        table.Add(Button(session.Round is null ? "DEAL CARDS" : "DEAL NEXT ROUND", "DealCards", (_, _) => Run(() =>
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
        content.Add(CardRow(session.DealerCards));
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
        table.Add(Text($"Dealer shows an Ace. Insurance costs {Cash(round.InitialBet / 2m)} and pays 2:1 if the dealer has Blackjack.", 16, Muted));
        var insurance = Button("TAKE INSURANCE", "TakeInsurance", (_, _) => Run(session.TakeInsurance));
        insurance.IsEnabled = session.CanTakeInsurance;
        table.Add(insurance);
        table.Add(Button("NO INSURANCE", "DeclineInsurance", (_, _) => Run(session.DeclineInsurance), true));
    }

    private void AddPlayerActions()
    {
        if (surrenderPending)
        {
            table.Add(Text("Surrender this hand and lose half its wager?", 18, Gold));
            table.Add(Button("CONFIRM SURRENDER", "ConfirmSurrender", (_, _) => Run(() =>
            { surrenderPending = false; session.Act(PlayerAction.Surrender); })));
            table.Add(Button("KEEP PLAYING", "CancelSurrender", (_, _) => { surrenderPending = false; Render(); }, true));
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
        table.Add(grid);
    }

    private void Run(Action command)
    {
        if (busy) { return; }
        busy = true;
        try
        {
            error = null;
            command();
        }
        catch (ArgumentException exception) { error = exception.Message.Split('\n')[0]; }
        catch (InvalidOperationException exception) { error = exception.Message; }
        finally
        {
            Render();
            busy = false;
        }
    }

    private static ColumnDefinitionCollection Columns(int count)
    {
        var result = new ColumnDefinitionCollection();
        for (int i = 0; i < count; i++) { result.Add(new ColumnDefinition { Width = GridLength.Star }); }
        return result;
    }

    private View CardRow(IEnumerable<Card?> cards)
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        foreach (Card? card in cards) { row.Add(CardView(card)); }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = row, HeightRequest = session.Settings.LargeCards ? 138 : 114 };
    }

    private Border CardView(Card? card)
    {
        var frame = new Border
        {
            WidthRequest = session.Settings.LargeCards ? 92 : 76, HeightRequest = session.Settings.LargeCards ? 132 : 108, Padding = 8,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = card.HasValue ? Color.FromArgb("#F4EFE5") : Ink,
            Stroke = new SolidColorBrush(card.HasValue ? Color.FromArgb("#D4C9AD") : Gold),
        };
        if (card is not { } face)
        {
            frame.Content = new Label { Text = "♠", FontSize = 44, TextColor = Gold,
                VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            SemanticProperties.SetDescription(frame, "Face-down dealer card");
            return frame;
        }
        string rank = face.Rank switch
        {
            Rank.Ace => "A", Rank.Jack => "J", Rank.Queen => "Q", Rank.King => "K",
            _ => ((int)face.Rank).ToString(CultureInfo.InvariantCulture),
        };
        string suit = face.Suit switch
        {
            Suit.Clubs => "♣", Suit.Diamonds => "♦", Suit.Hearts => "♥", _ => "♠",
        };
        Color color = face.Suit is Suit.Hearts or Suit.Diamonds ? Color.FromArgb("#A33145") : Ink;
        var content = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Star } },
        };
        content.Add(new Label { Text = rank, FontSize = session.Settings.LargeCards ? 28 : 22, FontAttributes = FontAttributes.Bold, TextColor = color }, 0, 0);
        content.Add(new Label { Text = suit, FontSize = session.Settings.LargeCards ? 44 : 36, TextColor = color,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }, 0, 1);
        frame.Content = content;
        SemanticProperties.SetDescription(frame, face.ToString());
        return frame;
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
