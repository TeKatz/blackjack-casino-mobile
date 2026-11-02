using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal sealed class SlotReelColumn : ContentView
{
    private readonly MobileSlotGame game;
    private readonly int[] final;
    private readonly int rows;
    private readonly double cellHeight;
    private readonly AbsoluteLayout viewport = new() { IsClippedToBounds = true };
    private VerticalStackLayout? strip;
    public SlotReelColumn(MobileSlotGame game, int[] final, double cellHeight)
    {
        this.game = game; this.final = final; rows = final.Length; this.cellHeight = cellHeight;
        HeightRequest = rows * cellHeight; Content = viewport; Show(final);
    }
    private void Show(IEnumerable<int> symbols)
    {
        viewport.Children.Clear();
        int[] visible = symbols.ToArray();
        strip = new VerticalStackLayout { Spacing = 0 };
        foreach (int symbol in visible)
        {
            var cell = new Grid { HeightRequest = cellHeight, Padding = game == MobileSlotGame.Reels ? 3 : 2,
                Background = game == MobileSlotGame.Reels ? Gradient("#E6DEC9", "#FFF9E9") : Gradient("#100D1B", "#05060B") };
            cell.Add(new Image { Source = ClubAssets.Symbol(game, symbol), Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill });
            strip.Add(cell);
        }
        AbsoluteLayout.SetLayoutBounds(strip, new Rect(0, 0, 1, cellHeight * visible.Length));
        AbsoluteLayout.SetLayoutFlags(strip, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.WidthProportional); viewport.Add(strip);
    }
    public async Task Spin(int reel, CancellationToken token)
    {
        // Decorative, deterministic strip motion. The only outcome is the saved receipt.
        int count = game == MobileSlotGame.Reels ? 5 : 8;
        int prefix = 9 + reel * 2;
        Show(Enumerable.Range(0,prefix).Select(i => (i * 3 + reel) % count).Concat(final));
        if (strip is not { } moving) { return; }
        try
        {
            await Task.Delay(reel * 55, token);
            token.ThrowIfCancellationRequested();
            await moving.TranslateToAsync(0, -prefix * cellHeight, (uint)(650 + reel * 115), Easing.CubicOut);
            token.ThrowIfCancellationRequested();
        }
        finally { Show(final); }
    }
    public Task WinPulse(Color color) => VisualMotion.Pulse(this, color);
}
