using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace BlackjackCasino.Mobile;

internal static class CasinoStyle
{
    public static readonly Color Ink = Color.FromArgb("#0A1116");
    public static readonly Color Panel = Color.FromArgb("#11241F");
    public static readonly Color Gold = Color.FromArgb("#D6B478");
    public static readonly Color Cream = Color.FromArgb("#F7EBD5");
    public static readonly Color Muted = Color.FromArgb("#91A69D");
    public static readonly Color Win = Color.FromArgb("#70DDB0");
    public static readonly Color Loss = Color.FromArgb("#E8919B");
    public static string Cash(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    public static string Net(decimal amount) => amount > 0 ? $"+{Cash(amount)}" : Cash(amount);
    public static LinearGradientBrush Gradient(string first, string last) => new()
    {
        StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
        GradientStops = { new GradientStop(Color.FromArgb(first), 0), new GradientStop(Color.FromArgb(last), 1) },
    };
    public static Label Text(string text, double size = 16, Color? color = null) => new()
    {
        Text = text, FontSize = size, TextColor = color ?? Cream,
        FontFamily = size >= 24 ? "serif" : null,
        CharacterSpacing = size <= 12 ? 1.2 : 0,
        LineHeight = 1.12,
    };
    public static Button Button(string text, string id, EventHandler clicked, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutomationId = id, MinimumHeightRequest = 48,
            CornerRadius = 9, FontSize = 12, CharacterSpacing = .8,
            FontAttributes = FontAttributes.Bold, Padding = new Thickness(14, 11),
            Background = primary ? Gradient("#E8CF96", "#B48A4B") : Gradient("#1D352D", "#12241F"),
            BorderColor = primary ? Color.FromArgb("#F1D99E") : Color.FromArgb("#3C594C"), BorderWidth = 1,
            TextColor = primary ? Color.FromArgb("#16231E") : Color.FromArgb("#E4CE9F"),
        };
        if (primary) { button.Shadow = new Shadow { Brush = Gold, Opacity = .13f, Radius = 12, Offset = new Point(0, 4) }; }
        button.Pressed += async (_, _) => { if (!VisualMotion.Reduced()) { await button.ScaleToAsync(.97, 65); } };
        button.Released += async (_, _) => { button.Scale = 1; await Task.CompletedTask; };
        button.Clicked += clicked;
        return button;
    }
    public static Border Box(View content, bool active = false) => new()
    {
        Content = content, Background = Gradient(active ? "#1C392E" : "#142922", "#0C1C19"), Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 13 },
        Stroke = new SolidColorBrush(active ? Gold : Color.FromArgb("#304C40")),
        StrokeThickness = 1,
        Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 4), Radius = 10, Opacity = .2f },
    };
    public static View Header(string eyebrow, string title, string? description = null, string? icon = null)
    {
        var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
        var copy = new VerticalStackLayout { Spacing = 5 };
        copy.Add(Text(eyebrow.ToUpperInvariant(), 10, Gold)); copy.Add(Text(title, 29));
        if (description is not null) { copy.Add(Text(description, 13, Muted)); }
        row.Add(copy);
        if (icon is not null) { row.Add(new Image { Source = icon, HeightRequest = 66, WidthRequest = 66, Aspect = Aspect.AspectFit }, 1); }
        return row;
    }
    public static Border Pill(string text, Color? color = null) => new()
    {
        Padding = new Thickness(10, 6), Stroke = new SolidColorBrush((color ?? Gold).WithAlpha(.4f)),
        StrokeShape = new RoundRectangle { CornerRadius = 15 }, BackgroundColor = (color ?? Gold).WithAlpha(.08f),
        Content = Text(text, 11, color ?? Gold),
    };
    public static Grid Columns(params View[] views)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        for (int i = 0; i < views.Length; i++) { grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); grid.Add(views[i], i); }
        return grid;
    }
    public static View Metric(string caption, string value, Color? accent = null)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(Text(caption.ToUpperInvariant(), 9, Muted)); stack.Add(Text(value, 22, accent ?? Cream));
        return stack;
    }
}
