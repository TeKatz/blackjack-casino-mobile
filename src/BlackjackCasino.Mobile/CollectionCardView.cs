using BlackjackCasino.Core.Collections;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal static class CollectionCardView
{
    internal static Color RarityColor(CollectionRarity rarity) => Color.FromArgb(rarity switch
    {
        CollectionRarity.Uncommon => "#83DCA4", CollectionRarity.Rare => "#7FC6FF",
        CollectionRarity.Epic => "#CEA8FF", CollectionRarity.Legendary => "#F6D27F", _ => "#BDCEC4",
    });
    internal static string Asset(string id) => "album_" + id.Replace('-', '_') + ".jpg";

    internal static Border Create(CollectionCard card, bool owned, bool large = false)
    {
        Color accent = RarityColor(card.Rarity);
        var content = new VerticalStackLayout { Spacing = 8 };
        if (owned)
        {
            content.Add(new Image { Source = Asset(card.Id), Aspect = Aspect.AspectFit,
                HeightRequest = large ? 400 : 196, AutomationId = "Artwork-" + card.Id });
        }
        else
        {
            var back = new VerticalStackLayout { Spacing = 18, VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center, Children =
                { Text(CollectionCatalog.Theme(card).Symbol, large ? 70 : 48, accent), Text("NOT COLLECTED", 11, Muted) } };
            content.Add(new Grid { HeightRequest = large ? 400 : 196, Children = { back } });
        }
        content.Add(new Label { Text = card.Name, FontSize = large ? 24 : 15, TextColor = Colors.White,
            MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation, FontAttributes = FontAttributes.Bold });
        content.Add(Text($"{card.Rarity.ToString().ToUpperInvariant()} · #{card.Number:00}", large ? 15 : 11, accent));
        Brush stroke = new SolidColorBrush(accent);
        if (card.Rarity is CollectionRarity.Epic or CollectionRarity.Legendary)
        {
            stroke = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
                GradientStops = { new GradientStop { Color = accent, Offset = 0 }, new GradientStop { Color = Colors.White, Offset = .45f }, new GradientStop { Color = accent, Offset = 1 } },
            };
        }
        var frame = new Border
        {
            Padding = large ? 14 : 9, BackgroundColor = Panel, Stroke = stroke,
            StrokeThickness = card.Rarity >= CollectionRarity.Epic ? 2.5 : 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = large ? 20 : 14 }, Content = content,
            AutomationId = "AlbumCard-" + card.Id,
        };
        SemanticProperties.SetDescription(frame, $"{card.Name}, {card.Rarity}, {(owned ? "collected" : "not collected")}");
        return frame;
    }
}

internal sealed class CollectionCardPage : ContentPage
{
    private readonly Game.BlackjackSession session;
    private readonly CollectionTheme theme;
    private int index;

    public CollectionCardPage(Game.BlackjackSession session, CollectionCard card)
    {
        this.session = session;
        theme = CollectionCatalog.Theme(card);
        index = theme.Cards.ToList().FindIndex(item => item.Id == card.Id);
        BackgroundColor = Ink;
        Render();
    }

    protected override void OnAppearing() { base.OnAppearing(); Render(); }

    private void Render()
    {
        CollectionCard card = theme.Cards[index];
        bool owned = session.Profile.Collection.OwnedCardIds.Contains(card.Id);
        Title = card.Name;
        var body = new VerticalStackLayout { Padding = 22, Spacing = 18, MaximumWidthRequest = 460,
            HorizontalOptions = LayoutOptions.Center };
        body.Add(Text(theme.Name, 18, Color.FromArgb(theme.Accent)));
        body.Add(CollectionCardView.Create(card, owned, true));
        body.Add(Text(owned ? "In your permanent collection" : "Find this card in a five-card pack", 17, owned ? Win : Muted));
        body.Add(Text($"A duplicate of this rarity earns {CollectionCatalog.DuplicatePoints(card.Rarity)} points. Every {CollectionCatalog.DuplicatePackCost} points becomes a Common pack.", 15, Muted));
        body.Add(Text($"Theme progress · {theme.Cards.Count(item => session.Profile.Collection.OwnedCardIds.Contains(item.Id))} / {theme.Cards.Count}", 16, Gold));
        if (index > 0) { body.Add(Button("PREVIOUS CARD", "PreviousCard", (_, _) => { index--; Render(); })); }
        if (index + 1 < theme.Cards.Count) { body.Add(Button("NEXT CARD", "NextCard", (_, _) => { index++; Render(); })); }
        body.Add(Button("BACK", "CardBack", async (_, _) => await Navigation.PopAsync()));
        Content = new ScrollView { Content = body };
    }
}
