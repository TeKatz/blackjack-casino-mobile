using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class CasinoVaultPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(22, 24), Spacing = 18, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private readonly Grid root = new();
    private GraphicsView? door;
    private bool confirm;
    private bool busy;
    private string? message;

    public CasinoVaultPage(BlackjackSession session)
    {
        this.session = session;
        Title = "Casino Vault";
        BackgroundColor = Ink;
        root.Add(new ScrollView { Content=body });Content=root;
    }
    protected override void OnAppearing() { base.OnAppearing(); confirm = false; Render(); }
    private void Render()
    {
        body.Children.Clear();
        CasinoVaultSnapshot vault = session.Vault;
        Color accent = Color.FromArgb(vault.Tier.Accent);
        body.Add(Header("YOUR CASINO VAULT", "Good things build up.", null, "club_nav_shop.png"));
        door=new GraphicsView {Drawable=new VaultDoorArt(vault.Progress),HeightRequest=174};body.Add(door);
        body.Add(Text(vault.Tier.Name, 24));
        body.Add(Text($"{Cash(session.Bankroll)} play cash · {session.Profile.Coins:N0} coins", 16, Muted));
        if (message is not null) { body.Add(Box(Text(message, 18, Gold))); }
        body.Add(Text($"{Cash(vault.CurrentAmount)} / {Cash(vault.Tier.Capacity)}", 27, accent));
        body.Add(new ProgressBar { Progress = (double)vault.Progress, ProgressColor = accent, BackgroundColor = Panel });
        int tierIndex = CasinoVaultTierCatalog.All.ToList().FindIndex(t => t.Id == vault.Tier.Id);
        CollectionRarity pack = (CollectionRarity)Math.Clamp(tierIndex, 0, 4);
        body.Add(Text($"Full Vault reward · {Cash(vault.Tier.Capacity)} + {vault.Tier.FullBonusRate:P0} bonus\nPlus one {pack} card pack", 19, Gold));
        body.Add(Text($"{vault.ContributionRate:P0} of new eligible play-cash wagers fills the Vault. Contributions do not deduct extra cash. Free spins contribute no wager. The Vault must be full to open; overflow while full is discarded.", 16, Muted));
        bool locked = session.IsRoundActive || session.IsDiceTableActive;
        if (locked) { body.Add(Text("Finish your saved Blackjack round or paid Dice table before opening the Vault.", 16, Gold)); }
        if (confirm && vault.IsFull && !locked)
        {
            body.Add(Text("Open this full Vault and save its cash and card pack now?", 20, Gold));
            body.Add(Button("CONFIRM · OPEN VAULT", "ConfirmOpenVault", async (_, _) =>
            {
                if (busy) { return; }
                busy = true;
                try { session.OpenCasinoVault(); confirm = false; message = "Cash and card pack saved."; }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message.Split('\n')[0]; }
                finally { busy = false; Render(); }
                if(session.LastVaultOpening is not null && !VisualMotion.Reduced() && door is { } wheel){VisualMotion.Celebrate(root,Gold);await wheel.RotateToAsync(360,700,Easing.CubicOut);wheel.Rotation=0;}
            }, true));
            body.Add(Button("KEEP VAULT CLOSED", "CancelOpenVault", (_, _) => { confirm = false; Render(); }));
        }
        else
        {
            var open = Button(vault.IsFull ? "OPEN FULL VAULT" : "VAULT IS FILLING", "OpenFullVault", (_, _) => { confirm = true; Render(); }, true);
            open.IsEnabled = vault.IsFull && !locked && !busy;
            body.Add(open);
        }
        if (session.LastVaultOpening is { } last)
        {
            body.Add(Box(Text($"LAST OPENING · {last.OpenedAt.ToLocalTime():g}\nBase {Cash(last.BaseAmount)} · Bonus {Cash(last.BonusAmount)}\nTotal returned {Cash(last.TotalPayout)}\nAlready saved to your play-cash wallet.", 18, Win)));
        }
        body.Add(Text("VAULT CAREER", 14, Gold));
        body.Add(Text($"Opened {vault.VaultsOpened:N0} · Returned {Cash(vault.LifetimePayouts)}\nBonuses {Cash(vault.LifetimeBonuses)} · Biggest return {Cash(vault.BiggestPayout)}", 17));
        body.Add(Text("TIERS · UNLOCKED BY LIFETIME ELIGIBLE WAGER", 14, Gold));
        foreach (CasinoVaultTierDefinition tier in CasinoVaultTierCatalog.All)
        {
            bool unlocked = vault.LifetimeEligibleWager >= tier.RequiredLifetimeWager;
            body.Add(Box(Text($"{tier.Name} · {(unlocked ? "UNLOCKED" : "LOCKED")}\nCapacity {Cash(tier.Capacity)} · Bonus {tier.FullBonusRate:P0}\nLifetime wager required {Cash(tier.RequiredLifetimeWager)}", 16, unlocked ? Color.FromArgb(tier.Accent) : Muted)));
        }
        body.Add(Text("New tiers apply to a fresh Vault cycle. A partially filled Vault keeps its current capacity. Wagers from before 0.6 establish the career tier but do not create retroactive Vault cash.", 15, Muted));
        body.Add(Button("OPEN ALBUM", "VaultAlbum", async (_, _) => await Navigation.PushAsync(new AlbumPage(session))));
        body.Add(Button("BACK", "VaultBack", async (_, _) => await Navigation.PopAsync()));
    }
}
