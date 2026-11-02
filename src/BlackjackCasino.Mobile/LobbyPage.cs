using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class LobbyPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly string? recoveryNotice;
    private readonly VerticalStackLayout body = new() { Padding = new Thickness(18, 10, 18, 24), Spacing = 18, MaximumWidthRequest = 820, HorizontalOptions = LayoutOptions.Fill };
    private readonly Grid root = new() { RowDefinitions = { new RowDefinition { Height = GridLength.Star }, new RowDefinition { Height = GridLength.Auto } } };
    private bool opening;
    public LobbyPage(BlackjackSession session, string? recoveryNotice = null)
    {
        this.session = session; this.recoveryNotice = recoveryNotice;
        Title = "The Midnight Club";
        root.Add(new ScrollView { Content = body });
        var nav = Columns(Nav("Club", "club_nav_season.png", "HomeTab", null),
            Nav("Album", "club_nav_album.png", "OpenAlbumTab", () => new AlbumPage(session)),
            Nav("You", "club_nav_statistics.png", "OpenProfileTab", () => new PlayerHubPage(session, HubSection.Profile)),
            Nav("Settings", "club_nav_shop.png", "OpenSettings", () => new PlayerHubPage(session, HubSection.Settings)));
        nav.Padding = new Thickness(12, 5); nav.BackgroundColor = Color.FromArgb("#0A1713"); root.Add(nav, 0, 1); Content = root;
    }
    protected override void OnAppearing() { base.OnAppearing(); Render(); }
    private void Render()
    {
        body.Children.Clear();
        var welcome = new Grid { ColumnSpacing = 14, ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 58 } } };
        var copy = new VerticalStackLayout { Spacing = 4 };
        copy.Add(Text("THE MIDNIGHT CLUB", 10, Gold)); copy.Add(Text("A different kind of night.", 29));
        copy.Add(Text($"Welcome back, {session.Profile.DisplayName}.", 13, Muted)); welcome.Add(copy);
        welcome.Add(new Border { HeightRequest = 56, WidthRequest = 56, Stroke = new SolidColorBrush(ClubAssets.FrameAccent(session.Profile.EquippedFrameId)), StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Content = new Image { Source = ClubAssets.Portrait(session.Profile.EquippedPortraitId), Aspect = Aspect.AspectFill }, IsClippedToBounds = true }, 1);
        body.Add(welcome);
        if (recoveryNotice is not null) { body.Add(Box(Text(recoveryNotice, 13, Gold))); }
        var identity = new VerticalStackLayout { Spacing = 12 };
        identity.Add(Columns(Metric("Play cash", Cash(session.AvailableCash), Cream), Metric("Club coins", session.Profile.Coins.ToString("N0"), Gold)));
        identity.Add(LevelProgress(session));
        body.Add(Box(identity));
        if (session.Profile.Collection.PendingOpening is not null)
        { body.Add(Button($"CONTINUE YOUR PACK · {session.RevealedCards}/5", "LobbyResumePack", async (_, _) => await Open(() => new PackRevealPage(session)), true)); }
        if (session.IsRoundActive || session.IsDiceTableActive || session.HasSlotBonus)
        {
            string text = session.IsRoundActive ? "Your Blackjack seat is waiting." : session.IsDiceTableActive ? "Your paid Dice table is saved." : "Your bonus spins are waiting.";
            body.Add(Box(Text(text, 16, Win), true));
        }
        body.Add(Text("FIND YOUR TABLE", 10, Gold));
        var games = new Grid { ColumnSpacing = 12, RowSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } } };
        for (int i = 0; i < 3; i++) { games.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
        games.Add(GameTile("Blackjack", "THE HOUSE CLASSIC", PlayingCardView.Preview(session.Profile.EquippedCardSetId), "OpenBlackjack", () => new BlackjackTablePage(session), !session.IsDiceTableActive && !session.HasSlotBonus), 0, 0);
        var dice = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        for (int i = 0; i < 3; i++) { var die = DiceFace.Create(i + 3, CosmeticCatalog.Get(session.Profile.EquippedDiceSetId).VisualVariant); die.WidthRequest = 44; die.HeightRequest = 48; die.Rotation = (i - 1) * 10; dice.Add(die); }
        games.Add(GameTile("Midnight Dice", "CASUAL & CASINO", dice, "OpenMidnightDice", () => new DiceRoomPage(session)), 1, 0);
        games.Add(GameTile("Midnight Reels", "A CLASSIC LITTLE ESCAPE", SymbolHero(MobileSlotGame.Reels), "OpenSlot-Reels", () => new SlotMachinePage(session, MobileSlotGame.Reels)), 0, 1);
        games.Add(GameTile("Neon Vault", "1,024 WAYS TO PLAY", SymbolHero(MobileSlotGame.NeonVault), "OpenSlot-NeonVault", () => new SlotMachinePage(session, MobileSlotGame.NeonVault)), 1, 1);
        games.Add(GameTile("Nine Lives", "LUCK HAS NINE LIVES", SymbolHero(MobileSlotGame.NineLives), "OpenSlot-NineLives", () => new SlotMachinePage(session, MobileSlotGame.NineLives)), 0, 2);
        CasinoVaultSnapshot vault = session.Vault;
        var vaultArt = new Image { Source = "club_nav_shop.png", HeightRequest = 88, Aspect = Aspect.AspectFit };
        games.Add(GameTile("Casino Vault", vault.IsFull ? "CASH & PACK READY" : $"{vault.Progress:P0} FULL", vaultArt, "OpenCasinoVault", () => new CasinoVaultPage(session)), 1, 2);
        body.Add(games);
        body.Add(Text("YOUR MIDNIGHT CLUB", 10, Gold));
        int packs = session.Profile.Collection.Packs.Values.Sum();
        body.Add(Destination("Collector's Album", $"{session.Profile.Collection.OwnedCardIds.Count}/{CollectionCatalog.Cards.Count} cards · {packs} unopened packs", "album", "OpenAlbum", () => new AlbumPage(session)));
        body.Add(Destination("Season Pass", "Your next reward is closer than you think.", "season", "OpenSeason", () => new PlayerHubPage(session, HubSection.Season)));
        body.Add(Destination("Challenges", "Goals, milestones and something to collect.", "challenges", "OpenChallenges", () => new PlayerHubPage(session, HubSection.Challenges)));
        body.Add(Destination("Cosmetic Shop", "Cards, portraits and your signature style.", "shop", "OpenShop", () => new PlayerHubPage(session, HubSection.Shop)));
        body.Add(Button("CASINO FLOOR & STATISTICS", "OpenCasinoFloor", async (_, _) => await Open(() => new CasinoFloorPage(session))));
    }
    private View GameTile(string title, string subtitle, View art, string id, Func<Page> page, bool enabled = true)
    {
        var content = new VerticalStackLayout { Spacing = 7 };
        var stage = new Grid { HeightRequest = 90, IsClippedToBounds = true }; stage.Add(art); content.Add(stage);
        content.Add(Text(subtitle, 8, Gold)); content.Add(Text(title, 21));
        var frame = Box(content); frame.Padding = 11; frame.AutomationId = id; frame.Opacity = enabled ? 1 : .45;
        SemanticProperties.SetDescription(frame, title);
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => { if (enabled) { await Open(page); } }; frame.GestureRecognizers.Add(tap);
        return frame;
    }
    private View Destination(string title, string subtitle, string art, string id, Func<Page> page)
    {
        var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition { Width = 62 }, new ColumnDefinition { Width = GridLength.Star } } };
        row.Add(new Image { Source = "club_nav_" + art + ".png", HeightRequest = 62 });
        var text = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center }; text.Add(Text(title, 22)); text.Add(Text(subtitle, 12, Muted)); row.Add(text, 1);
        Border tile = Box(row); tile.AutomationId = id;
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await Open(page); tile.GestureRecognizers.Add(tap); return tile;
    }
    private View Nav(string title, string image, string id, Func<Page>? page)
    {
        var cell = new VerticalStackLayout { Spacing = 1, Padding = 4, AutomationId = id, HorizontalOptions = LayoutOptions.Fill };
        cell.Add(new Image { Source = image, HeightRequest = 27 }); var text = Text(title, 10, page is null ? Gold : Muted); text.HorizontalTextAlignment = TextAlignment.Center; cell.Add(text);
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => { if (page is not null) { await Open(page); } }; cell.GestureRecognizers.Add(tap); return cell;
    }
    private async Task Open(Func<Page> page)
    { if (opening) { return; } opening = true; try { await Navigation.PushAsync(page()); } finally { opening = false; } }
    internal static View SymbolHero(MobileSlotGame game)
    {
        var row = new HorizontalStackLayout { Spacing = -12, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        int[] symbols = game == MobileSlotGame.Reels ? [4, 3, 4] : game == MobileSlotGame.NeonVault ? [0, 3, 7] : [5, 7, 0];
        for (int i = 0; i < 3; i++) { row.Add(new Image { Source = ClubAssets.Symbol(game, symbols[i]), WidthRequest = i == 1 ? 76 : 58, HeightRequest = i == 1 ? 86 : 66, Aspect = Aspect.AspectFit, Rotation = (i - 1) * 8, ZIndex = i == 1 ? 2 : 1 }); }
        return row;
    }
    internal static View LevelProgress(BlackjackSession session)
    {
        var progress = new VerticalStackLayout { Spacing = 5 };
        long needed = session.Profile.Level < ProgressionCurve.MaximumLevel ? ProgressionCurve.ExperienceForNextLevel(session.Profile.Level) : 0;
        progress.Add(new ProgressBar { HeightRequest = 4, Progress = needed == 0 ? 1 : (double)session.Profile.LevelExperience / needed, ProgressColor = Gold, BackgroundColor = Panel });
        progress.Add(Text($"LEVEL {session.Profile.Level} · " + (needed == 0 ? "Maximum level" : $"{session.Profile.LevelExperience:N0} / {needed:N0} XP"), 10, Muted)); return progress;
    }
    internal static Color TitleColor(string variant) => Color.FromArgb(variant switch
    { "title-violet" => "#D2ACFF", "title-teal" => "#80E4CD", "title-rose" => "#FFA8C5", "title-ice" => "#A9DBFF", "title-amber" => "#FFBE75", _ => "#D6B478" });
}
