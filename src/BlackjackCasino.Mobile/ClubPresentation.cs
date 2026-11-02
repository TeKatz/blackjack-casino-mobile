using BlackjackCasino.Core;
using Microsoft.Maui.Graphics;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public class CasinoPage : ContentPage
{
    protected string StageStart { get; set; } = "#0A1116";
    protected string StageEnd { get; set; } = "#14261F";
    protected override void OnAppearing()
    {
        base.OnAppearing(); Background = Gradient(StageStart, StageEnd);
        if (Content is { } content && !VisualMotion.Reduced()) { _ = VisualMotion.Enter(content); }
    }
}

internal static class ClubAssets
{
    private static readonly string[] Reels = ["cherry", "lemon", "bell", "bar", "seven"];
    private static readonly string[] Neon = ["pulse", "prism", "chip", "crown", "key", "wild", "scatter", "vaultkey"];
    private static readonly string[] Cats = ["yarn", "fishbone", "bell", "tabbycat", "whitecat", "blackcat", "wild", "bonus"];
    public static string Symbol(Game.MobileSlotGame game, int symbol) => game switch
    {
        Game.MobileSlotGame.Reels => "club_reels_" + Reels[symbol] + ".png",
        Game.MobileSlotGame.NeonVault => "club_neon_" + Neon[symbol] + ".jpg",
        _ => "club_cats_" + Cats[symbol] + ".jpg",
    };
    private static readonly string[] Portraits = ["night-dealer","card-shark","moon-cat","neon-ace","lucky-raven","midnight-lady","golden-joker","velvet-king"];
    public static bool HasPortrait(string id) => id==CosmeticCatalog.DefaultPortraitId || Portraits.Contains(id.Replace("portrait-",""));
    public static string Portrait(string id) => id==CosmeticCatalog.DefaultPortraitId || !HasPortrait(id) ? "club_portrait_classic.png" : "club_portrait_" + id.Replace("portrait-", "").Replace('-', '_') + ".jpg";
    public static Color FrameAccent(string id)
    {
        string variant=CosmeticCatalog.TryGet(id)?.VisualVariant??"classic";
        return Color.FromArgb(variant.Contains("slate")?"#8794A1":variant.Contains("copper")?"#C49476":variant.Contains("sage")?"#9FBAA3":variant.Contains("pearl")?"#EBE5D8":variant.Contains("emerald")?"#70DDB0":variant.Contains("neon")?"#80D6EC":variant.Contains("violet")?"#C0A3E1":"#D6B478");
    }
}

internal static class VisualMotion
{
    public static Func<bool> Reduced { get; set; } = () => false;
    public static async Task Enter(View view)
    {
        try { view.Opacity = .65; view.TranslationY = 7; await Task.WhenAll(view.FadeToAsync(1, 190), view.TranslateToAsync(0, 0, 220, Easing.CubicOut)); }
        catch (InvalidOperationException) { }
        finally { view.Opacity = 1; view.TranslationY = 0; }
    }
    public static async Task Pulse(View view, Color color)
    {
        if (Reduced()) { return; }
        var old = view.Shadow;
        try
        {
            view.Shadow = new Shadow { Brush = color, Radius = 24, Opacity = .45f };
            for (int i = 0; i < 2; i++) { await view.ScaleToAsync(1.035, 160, Easing.CubicOut); await view.ScaleToAsync(1, 200, Easing.CubicInOut); }
        }
        catch (InvalidOperationException) { }
        finally { view.Scale = 1; view.Shadow = old; }
    }
    public static void Celebrate(Grid root, Color color)
    {
        if (Reduced()) { return; }
        var particles = new ParticleArt(color);
        var overlay = new GraphicsView { Drawable = particles, InputTransparent = true, ZIndex = 100 };
        Grid.SetRowSpan(overlay, 5); Grid.SetColumnSpan(overlay, 5); root.Add(overlay);
        new Animation(value => { particles.Progress = (float)value; overlay.Invalidate(); }, 0, 1)
            .Commit(overlay, "club-celebration", 16, 1250, Easing.Linear, (_, _) => root.Remove(overlay));
    }
    private sealed class ParticleArt(Color color) : IDrawable
    {
        public float Progress { get; set; }
        public void Draw(ICanvas canvas, RectF area)
        {
            float p = Progress; canvas.FillColor = color.WithAlpha((1 - p) * .85f);
            for (int i = 0; i < 36; i++)
            {
                float angle = i * 2.399963f;
                float radius = (35 + i % 7 * 12) * p * 2;
                float x = area.Width / 2 + MathF.Cos(angle) * radius;
                float y = area.Height * .38f + MathF.Sin(angle) * radius + p * p * 160;
                canvas.FillEllipse(x, y, i % 3 + 2, i % 4 + 3);
            }
        }
    }
}
