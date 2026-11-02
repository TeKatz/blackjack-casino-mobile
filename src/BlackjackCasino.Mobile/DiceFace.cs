using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal static class DiceFace
{
    public static Border Create(int value, string variant, bool held = false)
    {
        (string Face, string Pip) palette = variant switch
        {
            "dice-obsidian" => ("#172123", "#E7C879"),
            "dice-jade" => ("#126855", "#F4EFE5"),
            "dice-violet" => ("#694593", "#F4DEA2"),
            "dice-ice" => ("#B7DCEB", "#24495B"),
            "dice-ruby" => ("#8D2948", "#FFF0DB"),
            "dice-aurora" => ("#286F82", "#D8C8FF"),
            _ => ("#F4EFE5", "#19372D"),
        };
        var grid = new Grid { Padding = 9, RowSpacing = 3, ColumnSpacing = 3 };
        for (int i = 0; i < 3; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }
        (int Column, int Row)[] pips = value switch
        {
            1 => [(1, 1)], 2 => [(0, 0), (2, 2)],
            3 => [(0, 0), (1, 1), (2, 2)],
            4 => [(0, 0), (2, 0), (0, 2), (2, 2)],
            5 => [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)],
            6 => [(0, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)], _ => [],
        };
        foreach (var pip in pips)
        {
            grid.Add(new Ellipse { Fill = new SolidColorBrush(Color.FromArgb(palette.Pip)),
                WidthRequest = 7, HeightRequest = 7, HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center }, pip.Column, pip.Row);
        }
        if (value == 0)
        { grid.Add(new Label { Text = "·", FontSize = 20, TextColor = Muted, HorizontalOptions = LayoutOptions.Center }, 1, 1); }
        var face = new Border
        {
            MinimumHeightRequest = 56, MinimumWidthRequest = 44, HeightRequest = 62,
            BackgroundColor = value == 0 ? Panel : Color.FromArgb(palette.Face),
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, StrokeThickness = held ? 3 : 1,
            Stroke = new SolidColorBrush(held ? Gold : Color.FromArgb("#547160")), Content = grid,
        };
        SemanticProperties.SetDescription(face, value == 0 ? "Unrolled die" : $"Die showing {value}{(held ? ", held" : "")}");
        return face;
    }
}
