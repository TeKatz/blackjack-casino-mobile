using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class PackRevealPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout body = new()
    {
        Padding = new Thickness(22, 24), Spacing = 18, MaximumWidthRequest = 460,
        HorizontalOptions = LayoutOptions.Center,
    };
    private readonly VerticalStackLayout dock = new() { Padding = new Thickness(18,10), Spacing = 7 };
    private readonly Grid root = new() { RowDefinitions = { new RowDefinition { Height=GridLength.Star }, new RowDefinition { Height=GridLength.Auto } } };
    private int selected;
    private bool busy;
    private string? message;

    public PackRevealPage(BlackjackSession session)
    {
        this.session = session;
        selected = Math.Max(0, session.RevealedCards - 1);
        Title = "Pack opening";
        BackgroundColor = Ink;
        root.Add(new ScrollView { Content=body }); dock.BackgroundColor=Color.FromArgb("#0D211A"); root.Add(dock,0,1); Content=root;
        Render();
    }

    protected override void OnAppearing() { base.OnAppearing(); Render(); }

    private View? Render()
    {
        body.Children.Clear(); dock.Children.Clear();
        if (message is not null) { body.Add(Box(Text(message, 15, Gold))); }
        CollectionOpening? opening = session.Profile.Collection.PendingOpening;
        if (opening is null)
        {
            body.Add(Text("Your cards are collected", 28, Gold));
            body.Add(Text("Open your Album to explore your collection.", 17, Muted));
            body.Add(Button("BACK", "RevealBack", async (_, _) => await Navigation.PopAsync(), true));
            return null;
        }
        Color accent = CollectionCardView.RarityColor(opening.PackRarity);
        body.Add(Text($"{opening.PackRarity.ToString().ToUpperInvariant()} PACK", 28, accent));
        body.Add(Text($"{session.RevealedCards} / {CollectionCatalog.CardsPerPack} revealed", 16, Muted));
        var indicators = new Grid { ColumnSpacing = 6 };
        for (int i = 0; i < opening.Cards.Count; i++)
        {
            indicators.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            int position = i;
            bool revealed = position < session.RevealedCards;
            var indicator = Button(revealed ? $"{position + 1}" : "?", $"RevealSlot{position + 1}",
                (_, _) => { if (!busy && position < session.RevealedCards) { selected = position; Render(); } }, revealed && selected == position);
            indicator.IsEnabled = revealed;
            indicators.Add(indicator, position, 0);
        }
        body.Add(indicators);
        View? cardView = null;
        if (session.RevealedCards > 0)
        {
            selected = Math.Clamp(selected, 0, session.RevealedCards - 1);
            CollectionDraw draw = opening.Cards[selected];
            CollectionCard card = CollectionCatalog.Find(draw.CardId)!;
            body.Add(Text(draw.IsNew ? "NEW TO YOUR COLLECTION" : $"DUPLICATE · +{draw.DuplicatePoints} POINTS", 17, draw.IsNew ? Win : Gold));
            cardView = CollectionCardView.Create(card, true, true);
            body.Add(cardView);
            body.Add(Text(CollectionCatalog.Theme(card).Name, 17, Color.FromArgb(CollectionCatalog.Theme(card).Accent)));
        }
        else
        {
            body.Add(PackPresentation.Cover(opening.PackRarity,true));
        }
        if (session.RevealedCards < CollectionCatalog.CardsPerPack)
        {
            dock.Add(Button($"REVEAL CARD {session.RevealedCards + 1} / 5", "RevealNextCard", async (_, _) => await Reveal(false), true));
            dock.Add(Button("REVEAL ALL", "RevealAllCards", async (_, _) => await Reveal(true)));
        }
        else
        {
            int newCards = opening.Cards.Count(card => card.IsNew);
            body.Add(Box(Text($"PACK COMPLETE\n{newCards} new · {5 - newCards} duplicates\n+{opening.PointsEarned} duplicate points · {opening.PacksEarned} bonus pack(s)", 19, Gold)));
            body.Add(Text("Tap the numbered slots to revisit any of your five cards.", 15, Muted));
            dock.Add(Button("COLLECT & FINISH", "CompletePack", async (_, _) =>
            {
                if (busy) { return; }
                busy = true;
                try { session.CompletePack(opening.Id); await Navigation.PopAsync(); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { message = exception.Message.Split('\n')[0]; Render(); }
                finally { busy = false; }
            }, true));
        }
        body.Add(Text("You can leave and continue this pack later. Opening another pack waits until this reveal is finished.", 14, Muted));
        dock.Add(Button("RETURN · OPENING SAVED", "LeavePackReveal", async (_, _) => await Navigation.PopAsync()));
        return cardView;
    }

    private async Task Reveal(bool all)
    {
        if (busy) { return; }
        busy = true;
        try
        {
            if (all) { session.RevealAllCards(); } else { session.RevealNextCard(); }
            selected = session.RevealedCards - 1;
            message = null;
            View? target = Render();
            if (target is not null && !session.Settings.ReducePackAnimations && !VisualMotion.Reduced())
            {
                target.Opacity = 0;
                target.Scale = .91; target.RotationY = -85;
                try
                {
                    await Task.WhenAll(target.FadeToAsync(1, 280, Easing.CubicOut), target.ScaleToAsync(1, 480, Easing.CubicOut), target.RotateYToAsync(0,480,Easing.CubicOut));
                }
                finally { target.Opacity = 1; target.Scale = 1; target.RotationY=0; }
                VisualMotion.Celebrate(root,CollectionCardView.RarityColor(session.Profile.Collection.PendingOpening!.PackRarity));
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { message = exception.Message.Split('\n')[0]; Render(); }
        finally { busy = false; }
    }
}
