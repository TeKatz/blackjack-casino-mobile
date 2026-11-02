using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Graphics;

namespace BlackjackCasino.Mobile;

/// <summary>Small native vector symbols; no font emoji or atlas decoding in the reel grid.</summary>
internal sealed class SlotSymbolArt(MobileSlotGame game, int symbol) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        canvas.Translate((dirtyRect.Width - size) / 2, (dirtyRect.Height - size) / 2);
        canvas.Scale(size / 100, size / 100);
        canvas.StrokeSize = 5;
        canvas.StrokeColor = Color.FromArgb("#E7C879");
        canvas.FillColor = Color.FromArgb("#E7C879");
        if (game == MobileSlotGame.Reels)
        {
            switch ((SlotSymbol)symbol)
            {
                case SlotSymbol.Cherry:
                    canvas.FillColor = Color.FromArgb("#EF7790");
                    canvas.FillEllipse(13, 44, 32, 32); canvas.FillEllipse(50, 42, 32, 32);
                    canvas.StrokeColor = Color.FromArgb("#8EDBB1");
                    canvas.DrawLine(29, 44, 51, 17); canvas.DrawLine(66, 42, 51, 17);
                    break;
                case SlotSymbol.Lemon:
                    canvas.FillColor = Color.FromArgb("#FFE17F"); canvas.FillEllipse(15, 26, 70, 46);
                    canvas.StrokeColor = Color.FromArgb("#8EDBB1"); canvas.DrawLine(70, 25, 84, 14);
                    break;
                case SlotSymbol.Bell: Bell(canvas); break;
                case SlotSymbol.Bar:
                    canvas.FillRoundedRectangle(8, 30, 84, 42, 7);
                    canvas.StrokeColor = Color.FromArgb("#19372D");
                    for (int i = 0; i < 3; i++) { canvas.DrawLine(30 + i * 20, 40, 30 + i * 20, 62); }
                    break;
                default: canvas.DrawLine(20, 20, 82, 20); canvas.DrawLine(82, 20, 39, 83); canvas.DrawLine(35, 50, 72, 50); break;
            }
        }
        else if (game == MobileSlotGame.NeonVault)
        {
            canvas.FillColor = Color.FromArgb("#8CD5F2"); canvas.StrokeColor = Color.FromArgb("#8CD5F2");
            switch ((NeonVaultSymbol)symbol)
            {
                case NeonVaultSymbol.Pulse:
                    canvas.DrawEllipse(16, 16, 68, 68); canvas.DrawLine(12, 52, 36, 52);
                    canvas.DrawLine(36, 52, 46, 30); canvas.DrawLine(46, 30, 59, 70); canvas.DrawLine(59, 70, 70, 52); canvas.DrawLine(70, 52, 90, 52); break;
                case NeonVaultSymbol.Prism: Polygon(canvas, [50, 6, 90, 50, 50, 94, 10, 50]); break;
                case NeonVaultSymbol.Chip:
                    canvas.DrawEllipse(10, 10, 80, 80); canvas.DrawEllipse(29, 29, 42, 42);
                    for (int i = 0; i < 4; i++) { canvas.DrawLine(20 + i * 20, 14, 20 + i * 20, 24); } break;
                case NeonVaultSymbol.Crown: Polygon(canvas, [14, 74, 8, 22, 33, 43, 50, 10, 67, 43, 92, 22, 86, 74]); break;
                case NeonVaultSymbol.Key: Key(canvas); break;
                case NeonVaultSymbol.Wild: Star(canvas); break;
                case NeonVaultSymbol.Scatter:
                    canvas.DrawRoundedRectangle(10, 10, 80, 80, 8); canvas.DrawEllipse(30, 30, 40, 40);
                    canvas.DrawLine(50, 22, 50, 78); canvas.DrawLine(22, 50, 78, 50); break;
                default: Key(canvas); canvas.DrawLine(12, 82, 87, 82); break;
            }
        }
        else
        {
            canvas.FillColor = Color.FromArgb("#E7AECE"); canvas.StrokeColor = Color.FromArgb("#E7AECE");
            switch ((NineLivesSymbol)symbol)
            {
                case NineLivesSymbol.Yarn:
                    canvas.DrawEllipse(14, 14, 72, 72); canvas.DrawEllipse(26, 14, 48, 72);
                    canvas.DrawLine(18, 31, 80, 62); canvas.DrawLine(20, 62, 81, 35); break;
                case NineLivesSymbol.Fishbone:
                    canvas.DrawLine(12, 50, 79, 50); canvas.DrawLine(28, 30, 28, 70); canvas.DrawLine(48, 30, 48, 70);
                    Polygon(canvas, [78, 50, 94, 31, 94, 69]); break;
                case NineLivesSymbol.Bell: Bell(canvas); break;
                case NineLivesSymbol.Wild:
                    canvas.FillEllipse(29, 42, 43, 41);
                    for (int i = 0; i < 4; i++) { canvas.FillEllipse(13 + i * 20, i is 0 or 3 ? 29 : 17, 16, 21); } break;
                case NineLivesSymbol.Bonus:
                    for (int i = 0; i < 9; i++) { canvas.FillEllipse(13 + i % 3 * 29, 13 + i / 3 * 29, 17, 17); } break;
                default:
                    if ((NineLivesSymbol)symbol == NineLivesSymbol.WhiteCat) { canvas.FillColor = Color.FromArgb("#F4EFE5"); }
                    if ((NineLivesSymbol)symbol == NineLivesSymbol.BlackCat) { canvas.FillColor = Color.FromArgb("#748AAB"); }
                    Polygon(canvas, [14, 63, 14, 10, 38, 30, 62, 30, 86, 10, 86, 63, 70, 86, 30, 86]);
                    canvas.FillColor = Color.FromArgb("#19372D"); canvas.FillEllipse(29, 46, 10, 12); canvas.FillEllipse(61, 46, 10, 12);
                    canvas.FillEllipse(44, 66, 12, 8); break;
            }
        }
        canvas.RestoreState();
    }
    private static void Bell(ICanvas c)
    { c.FillRoundedRectangle(25, 22, 50, 52, 22); c.FillRoundedRectangle(15, 67, 70, 12, 5); c.FillEllipse(41, 79, 18, 12); }
    private static void Key(ICanvas c)
    { c.DrawEllipse(9, 10, 36, 36); c.DrawLine(39, 40, 82, 83); c.DrawLine(67, 67, 80, 54); c.DrawLine(78, 78, 91, 65); }
    private static void Star(ICanvas c)
    { Polygon(c, [50, 5, 62, 34, 95, 36, 70, 58, 79, 91, 50, 72, 21, 91, 30, 58, 5, 36, 38, 34]); }
    private static void Polygon(ICanvas c, float[] points)
    {
        var path = new PathF(); path.MoveTo(points[0], points[1]);
        for (int i = 2; i < points.Length; i += 2) { path.LineTo(points[i], points[i + 1]); }
        path.Close(); c.FillPath(path);
    }
}
