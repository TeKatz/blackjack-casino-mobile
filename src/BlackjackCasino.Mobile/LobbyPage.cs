using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class LobbyPage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly Button openTable;
    private readonly Label balance;

    public LobbyPage(BlackjackSession session)
    {
        this.session = session;
        Title = "Blackjack Empire";
        BackgroundColor = Ink;
        balance = Text("", 18, Gold);
        openTable = Button("PLAY BLACKJACK", "OpenBlackjack", async (_, _) =>
        {
            openTable!.IsEnabled = false;
            try { await Navigation.PushAsync(new BlackjackTablePage(session)); }
            finally { openTable.IsEnabled = true; }
        }, primary: true);
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28, 48), Spacing = 24, MaximumWidthRequest = 560,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    Text("THE MIDNIGHT CLUB", 14, Gold),
                    new Label { Text = "Your table.\nAnywhere.", TextColor = Colors.White,
                        FontSize = 42, FontAttributes = FontAttributes.Bold },
                    Text("Classic Blackjack", 24, Gold),
                    Text("Take a seat, choose your wager and play against the dealer.", 17, Muted),
                    balance,
                    openTable,
                    Text("6 decks · Blackjack pays 3:2\nDealer stands on soft 17", 15, Muted),
                    Text("Practice preview · Cash and rounds reset when the app process restarts.", 14, Muted),
                },
            },
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        balance.Text = $"Practice cash · {Cash(session.AvailableCash)} available";
        openTable.Text = session.IsRoundActive ? "RESUME YOUR ROUND" : "PLAY BLACKJACK";
    }
}
