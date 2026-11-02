using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public enum AlbumSection { Themes, Packs }

public sealed class AlbumPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout body = new()
    {
        Padding = new Thickness(20, 24), Spacing = 18, MaximumWidthRequest = 620,
        HorizontalOptions = LayoutOptions.Center,
    };
    private AlbumSection section;
    private CollectionTheme? theme;
    private CollectionRarity? purchase;
    private CollectionRarity? odds;
    private int page;
    private string? message;
    private bool navigating;

    public AlbumPage(BlackjackSession session, AlbumSection section = AlbumSection.Themes)
    {
        this.session = session;
        this.section = section;
        Title = "Collector's Album";
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
        Render();
    }

    protected override void OnAppearing() { base.OnAppearing(); Render(); }

    private void Render()
    {
        body.Children.Clear();
        CollectionState collection = session.Profile.Collection;
        body.Add(Header("YOUR COLLECTION", "Collector's Album", "Every card has a place in your story.", "club_nav_album.png"));
        body.Add(Text($"{collection.OwnedCardIds.Count} / {CollectionCatalog.Cards.Count} cards · {collection.Packs.Values.Sum()} packs · {session.Profile.Coins:N0} coins", 17, Muted));
        var progress = new ProgressBar { Progress = (double)collection.OwnedCardIds.Count / CollectionCatalog.Cards.Count,
            ProgressColor = Gold, BackgroundColor = Panel };
        body.Add(progress);
        var tabs = new Grid { ColumnSpacing = 12, ColumnDefinitions =
            { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } } };
        tabs.Add(Button("THEMES", "AlbumThemes", (_, _) => { section = AlbumSection.Themes; theme = null; page = 0; Render(); }, section == AlbumSection.Themes), 0, 0);
        tabs.Add(Button("PACKS", "AlbumPacks", (_, _) => { section = AlbumSection.Packs; theme = null; page = 0; Render(); }, section == AlbumSection.Packs), 1, 0);
        body.Add(tabs);
        if (message is not null) { body.Add(Box(Text(message, 15, Gold))); }
        if (collection.PendingOpening is not null)
        {
            body.Add(Button($"CONTINUE PACK · {session.RevealedCards} / 5 REVEALED", "ResumePack",
                async (_, _) => await Navigate(() => new PackRevealPage(session)), true));
        }
        if (section == AlbumSection.Packs) { Packs(); }
        else if (theme is not null) { ThemeCards(theme); }
        else { Themes(); }
        body.Add(Button("BACK TO LOBBY", "AlbumBack", async (_, _) => await Navigation.PopAsync()));
    }

    private void Themes()
    {
        CollectionTheme[] visible = CollectionCatalog.Themes.Skip(page * 4).Take(4).ToArray();
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 14, ColumnDefinitions = { new ColumnDefinition { Width=GridLength.Star },new ColumnDefinition { Width=GridLength.Star } } };
        for (int i=0;i<2;i++) { grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto }); }
        for (int i=0;i<visible.Length;i++)
        {
            CollectionTheme item=visible[i]; Color accent=Color.FromArgb(item.Accent);
            int owned=item.Cards.Count(card=>session.Profile.Collection.OwnedCardIds.Contains(card.Id));
            var row=new VerticalStackLayout { Spacing=8 };
            var cover=new Grid { HeightRequest=136, IsClippedToBounds=true };
            cover.Add(new Image { Source=CollectionCardView.Asset(item.Cards[0].Id), Aspect=Aspect.AspectFill, Opacity=.85 });
            var symbol=Text(item.Symbol,38,accent); symbol.HorizontalOptions=LayoutOptions.End; symbol.VerticalOptions=LayoutOptions.End; symbol.Margin=10; cover.Add(symbol); row.Add(cover);
            row.Add(Text(item.Name,18)); row.Add(Text($"{owned}/{item.Cards.Count} COLLECTED"+(item.UltraRare?" · ULTRA RARE":""),8,accent));
            row.Add(new ProgressBar { Progress=(double)owned/item.Cards.Count, ProgressColor=accent,BackgroundColor=Ink,HeightRequest=3 });
            var tile=Box(row);tile.Padding=9;tile.AutomationId="Theme-"+item.Id;
            var tap=new TapGestureRecognizer();tap.Tapped+=(_,_)=>{ theme=item;Render(); };tile.GestureRecognizers.Add(tap);grid.Add(tile,i%2,i/2);
        }
        body.Add(grid);
        body.Add(Text($"PAGE {page+1} / {(CollectionCatalog.Themes.Count+3)/4}",10,Muted));
        if(page>0){body.Add(Button("PREVIOUS THEMES","PreviousThemes",(_,_)=>{page--;Render();}));}
        if((page+1)*4<CollectionCatalog.Themes.Count){body.Add(Button("NEXT THEMES","NextThemes",(_,_)=>{page++;Render();}));}
    }

    private void ThemeCards(CollectionTheme selected)
    {
        int owned = selected.Cards.Count(card => session.Profile.Collection.OwnedCardIds.Contains(card.Id));
        body.Add(Text(selected.Name, 25, Color.FromArgb(selected.Accent)));
        body.Add(Text($"{owned} / {selected.Cards.Count} collected · Tap any card for details", 15, Muted));
        CollectionThemeCompletionReward reward = CollectionService.ThemeCompletionReward(selected);
        bool claimed = session.Profile.Collection.ClaimedThemeRewardIds.Contains(selected.Id);
        var claim = Button(claimed ? "REWARD CLAIMED" : $"CLAIM · {reward.Coins} COINS + {reward.PackRarity.ToString().ToUpperInvariant()} PACK", "ClaimTheme-" + selected.Id,
            (_, _) => Run(() => session.ClaimThemeReward(selected.Id), "Theme reward saved."));
        claim.IsEnabled = !claimed && owned == selected.Cards.Count;
        body.Add(claim);
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 16, ColumnDefinitions =
            { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } } };
        for (int i = 0; i < (selected.Cards.Count + 1) / 2; i++) { grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
        for (int i = 0; i < selected.Cards.Count; i++)
        {
            CollectionCard card = selected.Cards[i];
            var frame = CollectionCardView.Create(card, session.Profile.Collection.OwnedCardIds.Contains(card.Id));
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await Navigate(() => new CollectionCardPage(session, card));
            frame.GestureRecognizers.Add(tap);
            SemanticProperties.SetHint(frame, "Open card details");
            grid.Add(frame, i % 2, i / 2);
        }
        body.Add(grid);
        body.Add(Button("ALL THEMES", "AllThemes", (_, _) => { theme = null; Render(); }));
    }

    private void Packs()
    {
        CollectionState state = session.Profile.Collection;
        body.Add(Text($"Duplicate points · {state.DuplicatePoints} / {CollectionCatalog.DuplicatePackCost}", 19, Gold));
        body.Add(new ProgressBar { Progress = (double)state.DuplicatePoints / CollectionCatalog.DuplicatePackCost,
            ProgressColor = Gold, BackgroundColor = Panel });
        body.Add(Text("Every pack contains five cards. Duplicate points automatically become Common packs. Buy with cosmetic Coins; your table cash stays separate.", 15, Muted));
        foreach (CollectionPack pack in CollectionCatalog.Packs)
        {
            var row = new VerticalStackLayout { Spacing = 12 };
            row.Add(PackPresentation.Cover(pack.Rarity));
            row.Add(Text($"Owned · {state.Packs.GetValueOrDefault(pack.Rarity)}   |   Price · {pack.Price:N0} coins", 16));
            row.Add(Text($"One card is at least {pack.GuaranteedRarity}. Four more cards use this pack's normal odds.", 14, Muted));
            var open = Button("OPEN PACK", "OpenPack-" + pack.Rarity, async (_, _) =>
            {
                if (navigating) { return; }
                try { session.OpenPack(pack.Rarity); await Navigate(() => new PackRevealPage(session)); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { message = exception.Message.Split('\n')[0]; Render(); }
            }, true);
            open.IsEnabled = state.PendingOpening is null && state.Packs.GetValueOrDefault(pack.Rarity) > 0;
            row.Add(open);
            var buy = Button($"BUY ONE · {pack.Price:N0} COINS", "BuyPack-" + pack.Rarity,
                (_, _) => { purchase = pack.Rarity; Render(); });
            buy.IsEnabled = session.Profile.Coins >= pack.Price;
            row.Add(buy);
            if (purchase == pack.Rarity)
            {
                row.Add(Text($"Spend {pack.Price:N0} cosmetic Coins on one {pack.Rarity} pack?", 17, Gold));
                row.Add(Button("CONFIRM PURCHASE", "ConfirmPackPurchase", (_, _) =>
                {
                    if (purchase != pack.Rarity) { return; }
                    purchase = null;
                    Run(() => session.PurchasePack(pack.Rarity), "Pack purchased and saved.");
                }));
                row.Add(Button("CANCEL", "CancelPackPurchase", (_, _) => { purchase = null; Render(); }));
            }
            row.Add(Button(odds == pack.Rarity ? "HIDE ODDS" : "VIEW ODDS", "Odds-" + pack.Rarity,
                (_, _) => { odds = odds == pack.Rarity ? null : pack.Rarity; Render(); }));
            if (odds == pack.Rarity) { AddOdds(row, pack); }
            body.Add(Box(row));
        }
        body.Add(Text($"Packs opened · {state.PacksOpened:N0}. Earn more from Blackjack challenges and season tiers.", 15, Muted));
    }

    private static void AddOdds(VerticalStackLayout row, CollectionPack pack)
    {
        row.Add(Text($"Each slot first has a {pack.UltraRareChance:P2} chance of an ultra-rare theme card (always Legendary).", 14, Gold));
        row.Add(Text("Otherwise, these are the normal-theme rarity chances. First slot / each of the other four slots:", 14, Muted));
        int floor = (int)pack.GuaranteedRarity;
        double firstTotal = pack.Weights.Skip(floor).Sum();
        double normalTotal = pack.Weights.Sum();
        foreach (CollectionRarity rarity in Enum.GetValues<CollectionRarity>())
        {
            int index = (int)rarity;
            double first = index < floor ? 0 : pack.Weights[index] / firstTotal;
            double normal = pack.Weights[index] / normalTotal;
            row.Add(Text($"{rarity} · {first:P2} / {normal:P2}", 14, CollectionCardView.RarityColor(rarity)));
        }
        row.Add(Text("The ultra-rare roll is separate from the normal Legendary pool. Presentation does not change the draw.", 13, Muted));
    }

    private async Task Navigate(Func<Page> create)
    {
        if (navigating) { return; }
        navigating = true;
        try { await Navigation.PushAsync(create()); }
        finally { navigating = false; }
    }

    private void Run(Action command, string success)
    {
        try { command(); message = success; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { message = exception.Message.Split('\n')[0]; }
        Render();
    }
}
