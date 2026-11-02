using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class LobbyPage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly string? recoveryNotice;
    private readonly VerticalStackLayout body = new()
    {
        Padding = new Thickness(24, 32), Spacing = 20, MaximumWidthRequest = 560,
        HorizontalOptions = LayoutOptions.Center,
    };

    public LobbyPage(BlackjackSession session, string? recoveryNotice = null)
    {
        this.session = session;
        this.recoveryNotice = recoveryNotice;
        Title = "Blackjack Empire";
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        body.Children.Clear();
        body.Add(Text("THE MIDNIGHT CLUB", 14, Gold));
        body.Add(Text($"Welcome, {session.Profile.DisplayName}", 32));
        CosmeticDefinition title = CosmeticCatalog.Get(session.Profile.EquippedTitleId);
        body.Add(Text(title.Name, 18, TitleColor(title.VisualVariant)));
        if (recoveryNotice is not null) { body.Add(Box(Text(recoveryNotice, 15, Gold))); }
        var identity = new VerticalStackLayout { Spacing = 10 };
        identity.Add(Text($"LEVEL {session.Profile.Level} · {session.Profile.Coins:N0} COINS", 18, Gold));
        identity.Add(LevelProgress(session));
        identity.Add(Text($"Play cash · {Cash(session.AvailableCash)} available", 19));
        if (session.IsRoundActive) { identity.Add(Text($"{Cash(session.Committed)} reserved in your saved round", 14, Muted)); }
        body.Add(Box(identity));
        body.Add(Open(session.IsRoundActive ? "RESUME YOUR ROUND" : "PLAY BLACKJACK", "OpenBlackjack",
            () => new BlackjackTablePage(session), true));
        body.Add(Open("PROFILE & STATISTICS", "OpenProfile", () => new PlayerHubPage(session, HubSection.Profile)));
        body.Add(Open("CHALLENGES & REWARDS", "OpenChallenges", () => new PlayerHubPage(session, HubSection.Challenges)));
        body.Add(Open("SEASON PASS", "OpenSeason", () => new PlayerHubPage(session, HubSection.Season)));
        body.Add(Open("TITLE SHOP", "OpenShop", () => new PlayerHubPage(session, HubSection.Shop)));
        body.Add(Open("SETTINGS", "OpenSettings", () => new PlayerHubPage(session, HubSection.Settings)));
        body.Add(Text("Progress and open rounds save automatically on this device. Coins unlock cosmetics; play cash funds the table.", 14, Muted));
    }

    private Button Open(string caption, string id, Func<Page> create, bool primary = false)
    {
        bool opening = false;
        return Button(caption, id, async (_, _) =>
        {
            if (opening) { return; }
            opening = true;
            try { await Navigation.PushAsync(create()); }
            finally { opening = false; }
        }, primary);
    }

    internal static View LevelProgress(BlackjackSession session)
    {
        var progress = new VerticalStackLayout { Spacing = 6 };
        long needed = session.Profile.Level < ProgressionCurve.MaximumLevel
            ? ProgressionCurve.ExperienceForNextLevel(session.Profile.Level) : 0;
        progress.Add(new ProgressBar { Progress = needed == 0 ? 1 : (double)session.Profile.LevelExperience / needed,
            ProgressColor = Gold, BackgroundColor = Panel });
        progress.Add(Text(needed == 0 ? "Maximum level reached" : $"{session.Profile.LevelExperience:N0} / {needed:N0} XP", 14, Muted));
        return progress;
    }

    internal static Color TitleColor(string variant) => variant switch
    {
        "title-violet" => Color.FromArgb("#D2ACFF"), "title-teal" => Color.FromArgb("#80E4CD"),
        "title-rose" => Color.FromArgb("#FFA8C5"), "title-ice" => Color.FromArgb("#A9DBFF"),
        "title-amber" => Color.FromArgb("#FFBE75"), _ => Gold,
    };
}
