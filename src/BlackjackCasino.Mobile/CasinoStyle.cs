using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace BlackjackCasino.Mobile;

internal static class CasinoStyle
{
    public static readonly Color Ink = Color.FromArgb("#10251F");
    public static readonly Color Panel = Color.FromArgb("#19372D");
    public static readonly Color Gold = Color.FromArgb("#E7C879");
    public static readonly Color Muted = Color.FromArgb("#B5C9BE");
    public static readonly Color Win = Color.FromArgb("#88DFB1");
    public static readonly Color Loss = Color.FromArgb("#FFA6AA");
    public static string Cash(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    public static string Net(decimal amount) => amount > 0 ? $"+{Cash(amount)}" : Cash(amount);
    public static Label Text(string text, double size = 16, Color? color = null) => new()
    {
        Text = text, FontSize = size, TextColor = color ?? Colors.White,
    };
    public static Button Button(string text, string id, EventHandler clicked, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutomationId = id, MinimumHeightRequest = 54,
            CornerRadius = 14, FontAttributes = FontAttributes.Bold,
            BackgroundColor = primary ? Gold : Panel, TextColor = primary ? Ink : Gold,
        };
        button.Clicked += clicked;
        return button;
    }
    public static Border Box(View content, bool active = false) => new()
    {
        Content = content, BackgroundColor = Panel, Padding = 16,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Stroke = new SolidColorBrush(active ? Gold : Color.FromArgb("#315244")),
        StrokeThickness = active ? 2 : 1,
    };
}
