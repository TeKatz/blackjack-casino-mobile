using BlackjackCasino.Core.Collections;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal static class PackPresentation
{
    public static View Cover(CollectionRarity rarity, bool large = false)
    {
        Color accent = CollectionCardView.RarityColor(rarity);
        var face = new Grid { HeightRequest = large ? 285 : 132, IsClippedToBounds = true };
        var body = new VerticalStackLayout { Spacing = large ? 12 : 4, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
        body.Add(new Image { Source = "club_nav_album.png", HeightRequest = large ? 128 : 64, Aspect = Aspect.AspectFit });
        var name = Text(rarity.ToString().ToUpperInvariant(), large ? 24 : 14, accent); name.HorizontalTextAlignment = TextAlignment.Center; body.Add(name);
        var count = Text("FIVE COLLECTIBLE CARDS", 8, Gold); count.HorizontalTextAlignment = TextAlignment.Center; body.Add(count);
        face.Add(new Border { Margin = new Thickness(large ? 35 : 15, 4), Rotation = -5, Background = Gradient("#1E352B", "#0B1515"), Stroke = new SolidColorBrush(accent.WithAlpha(.4f)), StrokeShape = new RoundRectangle { CornerRadius = 10 } });
        face.Add(new Border { Margin = new Thickness(large ? 42 : 20, 0), Background = Gradient("#203B30", "#0A1714"), Stroke = new SolidColorBrush(accent), StrokeThickness = 1.5, StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = body,
            Shadow = new Shadow { Brush = accent, Radius = 15, Opacity = .2f } });
        return face;
    }
}
