using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class CasinoFloorPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(20, 24), Spacing = 18, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private bool statistics;
    private bool opening;

    public CasinoFloorPage(BlackjackSession session, bool statistics = false)
    {
        this.session = session;
        this.statistics = statistics;
        Title = "Casino Floor";
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
    }
    protected override void OnAppearing() { base.OnAppearing(); Render(); }

    private void Render()
    {
        body.Children.Clear();
        body.Add(Header("THE MIDNIGHT CLUB", statistics ? "Your casino career" : "Find your rhythm.", null, "club_nav_statistics.png"));
        body.Add(Text($"{Cash(session.Bankroll)} play cash · {session.Profile.Coins:N0} coins", 18));
        if (statistics) { Statistics(); return; }
        body.Add(Text("Three machines. Original rules. Every spin saves before its result appears.", 16, Muted));
        if (session.IsRoundActive || session.IsDiceTableActive)
        { body.Add(Box(Text("Finish your saved Blackjack round or paid Dice table to spin or open the Vault. You can browse the machines and paytables now.", 16, Gold))); }
        foreach (MobileSlotGame game in Enum.GetValues<MobileSlotGame>())
        {
            var card = new VerticalStackLayout { Spacing = 12 };
            card.Add(LobbyPage.SymbolHero(game)); card.Add(Text(Name(game), 24, Accent(game)));
            card.Add(Text(game switch
            {
                MobileSlotGame.Reels => "Three classic reels, one centre payline. Cherries, lemons, bells, BARs and lucky sevens.",
                MobileSlotGame.NeonVault => "Five reels, four rows and 1,024 ways. Expanding Wilds, Blackouts, jackpots and persistent free spins.",
                _ => "A 3×3 cat-themed machine with five lines. Pounce Wilds, nine-spin bonuses and Full Grid Frenzy.",
            }, 16, Muted));
            int free = session.FreeSlotSpins(game);
            if (free > 0) { card.Add(Text($"{free} FREE SPINS SAVED · Wager locked at {Cash(session.SlotBet(game))}", 18, Win)); }
            card.Add(Button(free > 0 ? "CONTINUE BONUS" : "TAKE A SEAT", "OpenSlot-" + game,
                async (_, _) => await Open(() => new SlotMachinePage(session, game)), free > 0));
            body.Add(Box(card, free > 0));
        }
        CasinoVaultSnapshot vault = session.Vault;
        var vaultCard = new VerticalStackLayout { Spacing = 10 };
        vaultCard.Add(Text(vault.Tier.Name.ToUpperInvariant(), 22, Color.FromArgb(vault.Tier.Accent)));
        vaultCard.Add(new ProgressBar { Progress = (double)vault.Progress, ProgressColor = Gold, BackgroundColor = Ink });
        vaultCard.Add(Text(vault.IsFull ? "FULL · CASH & PACK READY" : $"{Cash(vault.CurrentAmount)} / {Cash(vault.Tier.Capacity)}", 18, Gold));
        vaultCard.Add(Button("OPEN CASINO VAULT", "FloorVault", async (_, _) => await Open(() => new CasinoVaultPage(session)), vault.IsFull));
        body.Add(Box(vaultCard));
        body.Add(Button("STATISTICS & RECENT SPINS", "FloorStats", (_, _) => { statistics = true; Render(); }));
        body.Add(Button("BACK", "FloorBack", async (_, _) => await Navigation.PopAsync()));
    }

    private void Statistics()
    {
        foreach (MobileSlotGame game in Enum.GetValues<MobileSlotGame>())
        {
            SlotStatistics stats = game switch { MobileSlotGame.Reels => session.Profile.Slots,
                MobileSlotGame.NeonVault => session.Profile.NeonVault, _ => session.Profile.NineLives };
            var card = new VerticalStackLayout { Spacing = 8 };
            card.Add(Text(Name(game), 22, Accent(game)));
            card.Add(Text($"Spins {stats.Spins:N0} · Wins {stats.Wins:N0} · Even {stats.Pushes:N0}"));
            card.Add(Text($"Wagered {Cash(stats.TotalWagered)} · Returned {Cash(stats.TotalReturned)}"));
            card.Add(Text($"Net {Net(stats.NetProfit)} · Biggest return {Cash(stats.BiggestPayout)}"));
            if (game == MobileSlotGame.NeonVault)
            {
                NeonVaultStatistics neon = session.Profile.NeonVault;
                card.Add(Text($"Paid {neon.PaidSpins:N0} · Free {neon.FreeSpins:N0} · Bonus sessions {neon.FreeSpinSessions:N0}"));
                card.Add(Text($"Jackpots {neon.Jackpots:N0} · Grand {neon.GrandJackpots:N0} · Blackouts {neon.Blackouts:N0}"));
                card.Add(Text($"Security overrides {neon.SecurityOverrides:N0} · Best return {neon.BestPayoutMultiplier:N2}×"));
            }
            if (game == MobileSlotGame.NineLives)
            {
                NineLivesStatistics cats = session.Profile.NineLives;
                card.Add(Text($"Paid {cats.PaidSpins:N0} · Bonus spins {cats.BonusSpins:N0} · Sessions {cats.BonusSessions:N0}"));
                card.Add(Text($"Pounce Wilds {cats.PounceWilds:N0} · Frenzies {cats.FullGridFrenzies:N0} · Best return {cats.BestPayoutMultiplier:N2}×"));
            }
            body.Add(Box(card));
        }
        body.Add(Text("RECENT SPINS · UP TO 20", 14, Gold));
        if (session.SlotHistory.Count == 0) { body.Add(Text("Your first saved spin will appear here.", 16, Muted)); }
        foreach (MobileSlotReceipt spin in session.SlotHistory)
        {
            body.Add(Box(Text($"{Name(spin.Game)} · {spin.CompletedAt.ToLocalTime():g}\n{(spin.IsBonus ? "Free spin" : "Paid spin")} · Charged {Cash(spin.Stake)} · Returned {Cash(spin.Payout)}\nNet {Net(spin.Net)}", 16,
                spin.Net > 0 ? Win : spin.Net < 0 ? Loss : Gold)));
        }
        body.Add(Button("CHALLENGES & REWARDS", "FloorChallenges", async (_, _) => await Open(() => new PlayerHubPage(session, HubSection.Challenges))));
        body.Add(Button("BACK TO FLOOR", "CloseFloorStats", (_, _) => { statistics = false; Render(); }));
    }

    private async Task Open(Func<Page> page)
    {
        if (opening) { return; }
        opening = true;
        try { await Navigation.PushAsync(page()); } finally { opening = false; }
    }
    internal static string Name(MobileSlotGame game) => game switch
    { MobileSlotGame.Reels => "Midnight Reels", MobileSlotGame.NeonVault => "Neon Vault", _ => "Nine Lives" };
    internal static Color Accent(MobileSlotGame game) => game switch
    { MobileSlotGame.NeonVault => Color.FromArgb("#8CD5F2"), MobileSlotGame.NineLives => Color.FromArgb("#E7AECE"), _ => Gold };
}
