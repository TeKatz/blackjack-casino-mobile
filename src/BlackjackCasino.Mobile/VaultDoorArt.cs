using Microsoft.Maui.Graphics;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

internal sealed class VaultDoorArt(decimal progress) : IDrawable
{
    public void Draw(ICanvas c, RectF area)
    {
        float s=Math.Min(area.Width,area.Height),x=(area.Width-s)/2,y=(area.Height-s)/2;
        c.SaveState();c.Translate(x,y);c.Scale(s/200,s/200);
        c.FillColor=Color.FromArgb("#0A1914");c.FillEllipse(15,15,170,170);
        c.StrokeSize=2;c.StrokeColor=Gold;c.DrawEllipse(25,25,150,150);
        c.StrokeColor=Color.FromArgb("#3D5846");c.DrawEllipse(35,35,130,130);
        for(int i=0;i<48;i++)
        {
            float a=(i*7.5f-90)*MathF.PI/180;
            c.StrokeColor=i<48*(float)progress?Gold:Color.FromArgb("#2E473A");c.StrokeSize=4;
            c.DrawLine(100+MathF.Cos(a)*84,100+MathF.Sin(a)*84,100+MathF.Cos(a)*91,100+MathF.Sin(a)*91);
        }
        c.FillColor=Color.FromArgb("#1B392B");c.FillEllipse(48,48,104,104);
        c.StrokeColor=Gold;c.StrokeSize=3;c.DrawEllipse(48,48,104,104);
        for(int i=0;i<6;i++){float a=i*MathF.PI/3;c.DrawLine(100+MathF.Cos(a)*13,100+MathF.Sin(a)*13,100+MathF.Cos(a)*40,100+MathF.Sin(a)*40);}
        c.FillColor=Gold;c.FillEllipse(85,85,30,30);c.FillColor=Ink;c.FillEllipse(93,93,14,14);c.RestoreState();
    }
}
