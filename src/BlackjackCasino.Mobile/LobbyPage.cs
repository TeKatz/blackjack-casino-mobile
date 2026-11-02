using BlackjackCasino.Core;

namespace BlackjackCasino.Mobile;

public sealed class LobbyPage : ContentPage
{
    private static readonly Color Gold = Color.FromArgb("#E7C879");
    private static readonly Color Ink = Color.FromArgb("#10251F");

    public LobbyPage()
    {
        Title = "Blackjack Empire";
        BackgroundColor = Ink;
        var openTable = new Button
        {
            Text = "CHECK THE TABLE",
            BackgroundColor = Gold,
            TextColor = Ink,
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 56,
            CornerRadius = 16,
            AutomationId = "OpenTableProbe",
        };
        openTable.Clicked += async (_, _) =>
        {
            openTable.IsEnabled = false;
            try { await Navigation.PushAsync(CreateTableProbe()); }
            finally { openTable.IsEnabled = true; }
        };
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28, 48),
                Spacing = 24,
                MaximumWidthRequest = 560,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "THE MIDNIGHT CLUB", TextColor = Gold,
                        FontSize = 14, CharacterSpacing = 3 },
                    new Label { Text = "Your table.\nAnywhere.", TextColor = Colors.White,
                        FontSize = 42, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Blackjack Empire · Android preview", TextColor = Gold,
                        FontSize = 18 },
                    new Label { Text = "The mobile foundation is ready for a first look. Check the table to see the original card engine at work.",
                        TextColor = Color.FromArgb("#C4D3CB"), FontSize = 17 },
                    openTable,
                    new Label { Text = "Foundation preview · Full rounds are coming next.",
                        TextColor = Color.FromArgb("#A1B8AD"), FontSize = 14 },
                },
            },
        };
    }

    private static ContentPage CreateTableProbe()
    {
        Card[] cards = [new(Rank.Ace, Suit.Spades), new(Rank.King, Suit.Hearts)];
        HandValue value = HandEvaluator.Evaluate(cards);
        var back = new Button
        {
            Text = "BACK TO LOBBY", BackgroundColor = Gold, TextColor = Ink,
            MinimumHeightRequest = 56, CornerRadius = 16, AutomationId = "BackToLobby",
        };
        var page = new ContentPage
        {
            Title = "Table preview", BackgroundColor = Ink,
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 28, Spacing = 24, MaximumWidthRequest = 560,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "A♠   K♥", TextColor = Gold, FontSize = 52,
                            HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = $"Blackjack · {value.Total}", TextColor = Colors.White,
                            FontSize = 30, AutomationId = "CoreHandValue" },
                        new Label { Text = "Evaluated by the original Blackjack Casino Core. This is a fixed demonstration hand; betting and complete rounds arrive in the next milestone.",
                            TextColor = Color.FromArgb("#C4D3CB"), FontSize = 17 },
                        back,
                    },
                },
            },
        };
        back.Clicked += async (_, _) => await page.Navigation.PopAsync();
        return page;
    }
}
