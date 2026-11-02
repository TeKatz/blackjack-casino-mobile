using BlackjackCasino.Core;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal static class PlayingCardView
{
    public static Border Create(Card? card, string setId, bool large = false, bool compact = false)
    {
        string variant = CosmeticCatalog.TryGet(setId)?.VisualVariant ?? "card-classic";
        (string FaceA, string FaceB, string Edge, string Black, string Red, string BackA, string BackB, string Gold) p = variant switch
        {
            "card-art-deco" => ("#F7EFD5", "#E4D8B5", "#362E22", "#1C1C1A", "#972B2F", "#1B1D1F", "#0A0C0F", "#CFAB5A"),
            "card-crimson-velvet" => ("#FAEFE0", "#E7D8CF", "#803944", "#261F22", "#8B2332", "#661927", "#290D16", "#D8C0B9"),
            "card-emerald-house" => ("#F4F1DD", "#DAE4CD", "#366848", "#192D22", "#A22F36", "#124B31", "#072218", "#E1C063"),
            "card-midnight-blue" => ("#E8EFF6", "#CBD8E8", "#4C678B", "#1B2B43", "#9A3545", "#1A3358", "#09142A", "#B8CFE9"),
            "card-midnight-sovereign" => ("#1F2022", "#0D0F12", "#D6A94C", "#EFD9A6", "#DE6568", "#101013", "#05070A", "#F2C660"),
            _ => ("#F8F8F5", "#EEEEE8", "#D8D8D2", "#171A18", "#B82931", "#163D29", "#0A2319", "#D4A84F"),
        };
        double width = compact ? 48 : large ? 87 : 72, height = compact ? 68 : large ? 124 : 102;
        var content = new Grid { Padding = compact ? 5 : 8 };
        var frame = new Border { Content = content, WidthRequest = width, HeightRequest = height,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }, Stroke = new SolidColorBrush(Color.FromArgb(card.HasValue ? p.Edge : p.Gold)),
            StrokeThickness = variant == "card-classic" ? 1 : 1.5, Background = Gradient(card.HasValue ? p.FaceA : p.BackA, card.HasValue ? p.FaceB : p.BackB),
            Shadow = new Shadow { Brush = Colors.Black, Opacity = .3f, Radius = 7, Offset = new Point(1, 4) } };
        if (variant != "card-classic" || !card.HasValue)
        { content.Add(new Border { Stroke = new SolidColorBrush(Color.FromArgb(card.HasValue ? p.Edge : p.Gold).WithAlpha(.5f)), StrokeShape = new RoundRectangle { CornerRadius = 3 }, InputTransparent = true }); }
        if (card is not { } face)
        {
            var emblem = Text(variant == "card-midnight-sovereign" ? "✦" : "♠", compact ? 25 : 42, Color.FromArgb(p.Gold));
            emblem.HorizontalOptions = LayoutOptions.Center; emblem.VerticalOptions = LayoutOptions.Center; content.Add(emblem);
            SemanticProperties.SetDescription(frame, "Face-down dealer card"); return frame;
        }
        string rank = face.Rank switch { Rank.Ace => "A", Rank.Jack => "J", Rank.Queen => "Q", Rank.King => "K", _ => ((int)face.Rank).ToString() };
        string suit = face.Suit switch { Suit.Clubs => "♣", Suit.Diamonds => "♦", Suit.Hearts => "♥", _ => "♠" };
        Color ink = Color.FromArgb(face.Suit is Suit.Hearts or Suit.Diamonds ? p.Red : p.Black);
        var corner = Text(rank + "\n" + suit, compact ? 11 : large ? 18 : 15, ink); corner.FontAttributes = FontAttributes.Bold; corner.LineHeight = .85; content.Add(corner);
        var centre = Text(suit, compact ? 25 : large ? 43 : 35, ink); centre.HorizontalOptions = LayoutOptions.Center; centre.VerticalOptions = LayoutOptions.Center; content.Add(centre);
        var bottom = Text(rank, compact ? 10 : 14, ink); bottom.HorizontalOptions = LayoutOptions.End; bottom.VerticalOptions = LayoutOptions.End; bottom.Rotation = 180; content.Add(bottom);
        SemanticProperties.SetDescription(frame, face.ToString()); return frame;
    }
    public static View Preview(string setId)
    {
        var fan = new AbsoluteLayout { WidthRequest = 146, HeightRequest = 94, HorizontalOptions = LayoutOptions.Center };
        Card[] cards = [new(Rank.Ace, Suit.Spades), new(Rank.Queen, Suit.Hearts), new(Rank.King, Suit.Clubs)];
        for (int i = 0; i < 3; i++)
        { var card = Create(cards[i], setId, compact: true); card.Rotation = (i - 1) * 13; AbsoluteLayout.SetLayoutBounds(card, new Rect(24 + i * 27, i == 1 ? 7 : 13, 48, 68)); fan.Add(card); }
        return fan;
    }
}
