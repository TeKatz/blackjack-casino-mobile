using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal static class DiceFace
{
    public static Border Create(int value, string variant, bool held = false)
    {
        (string Top, string Bottom, string Pip, string Edge) p = variant switch
        {
            "dice-obsidian" => ("#35404A", "#0C1119", "#F4D483", "#C4A05B"),
            "dice-jade" => ("#4A987D", "#123E36", "#F5EDD5", "#97C7A4"),
            "dice-violet" => ("#9471B7", "#35224D", "#FFE5A5", "#CBA7E7"),
            "dice-ice" => ("#B7DAEE", "#4D7594", "#132F49", "#D5EDFA"),
            "dice-ruby" => ("#AE5668", "#491F32", "#FFF0D2", "#E1A287"),
            "dice-aurora" => ("#348B88", "#663D84", "#E1FFF4", "#85E6D0"),
            _ => ("#FFFCEE", "#D7CBB2", "#2D3730", "#BDA36E"),
        };
        var pips = new Grid { Padding = 8, RowSpacing = 2, ColumnSpacing = 2 };
        for (int i = 0; i < 3; i++) { pips.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); pips.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); }
        (int X, int Y)[] points = value switch
        { 1 => [(1,1)], 2 => [(0,0),(2,2)], 3 => [(0,0),(1,1),(2,2)], 4 => [(0,0),(2,0),(0,2),(2,2)], 5 => [(0,0),(2,0),(1,1),(0,2),(2,2)], 6 => [(0,0),(2,0),(0,1),(2,1),(0,2),(2,2)], _ => [] };
        foreach (var point in points) { pips.Add(new Ellipse { Fill = new SolidColorBrush(Color.FromArgb(p.Pip)), WidthRequest = 7, HeightRequest = 7, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }, point.X, point.Y); }
        if (value == 0) { var question = Text("?", 26, Color.FromArgb(p.Pip)); question.HorizontalOptions = LayoutOptions.Center; question.VerticalOptions = LayoutOptions.Center; pips.Add(question); Grid.SetColumnSpan(question,3); Grid.SetRowSpan(question,3); }
        var shine = new Border { Padding = 1, Margin = 3, Stroke = new SolidColorBrush(Colors.White.WithAlpha(.45f)), StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = pips };
        var face = new Border { HeightRequest = 60, MinimumWidthRequest = 40, MinimumHeightRequest = 40, Background = Gradient(p.Top, p.Bottom),
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, StrokeThickness = held ? 2.5 : 1,
            Stroke = new SolidColorBrush(held ? Win : Color.FromArgb(p.Edge)), Content = shine,
            Shadow = new Shadow { Brush = held ? Win : Colors.Black, Opacity = held ? .32f : .35f, Radius = held ? 12 : 5, Offset = new Point(0, held ? 0 : 4) } };
        face.SizeChanged += (_, _) => { double size = Math.Clamp(face.Width, 40, 80); if (face.Width > 0 && Math.Abs(face.HeightRequest - size) > 1) { face.HeightRequest = size; } };
        SemanticProperties.SetDescription(face, value == 0 ? "Unrolled die" : $"Die showing {value}{(held ? ", held" : "")}"); return face;
    }
}
