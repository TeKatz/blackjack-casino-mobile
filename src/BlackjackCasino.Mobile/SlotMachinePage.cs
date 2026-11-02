using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class SlotMachinePage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly MobileSlotGame game;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(16, 22), Spacing = 16, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private readonly List<View> cells = [];
    private CancellationTokenSource animation = new();
    private decimal wager;
    private bool busy;
    private bool paytable;
    private string? message;

    public SlotMachinePage(BlackjackSession session, MobileSlotGame game)
    {
        this.session = session; this.game = game; wager = session.SlotBet(game);
        Title = CasinoFloorPage.Name(game); BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
    }
    protected override void OnAppearing()
    { base.OnAppearing(); animation.Dispose(); animation = new(); wager = session.SlotBet(game); Render(); }
    protected override void OnDisappearing() { animation.Cancel(); base.OnDisappearing(); }

    private void Render()
    {
        body.Children.Clear(); cells.Clear();
        Color accent = CasinoFloorPage.Accent(game);
        body.Add(Text(Title!.ToUpperInvariant(), 29, accent));
        body.Add(Text($"{Cash(session.Bankroll)} PLAY CASH", 24));
        if (message is not null) { body.Add(Box(Text(message, 16, Gold))); }
        if (paytable) { Paytable(); return; }
        int free = session.FreeSlotSpins(game);
        if (free > 0)
        { body.Add(Box(Text($"{free} FREE SPINS SAVED\nTrigger wager {Cash(session.SlotBet(game))} · No cash charged", 19, Win), true)); }
        if (game == MobileSlotGame.NeonVault)
        { body.Add(Text($"Vault energy · {session.NeonEnergy} / {NeonVaultRules.VaultEnergyTarget}{(session.NeonOverrideArmed ? " · OVERRIDE READY" : "")}", 16, accent)); }
        MobileSlotReceipt? last = session.SlotHistory.FirstOrDefault(spin => spin.Game == game);
        AddGrid(last);
        if (last is not null)
        {
            string headline = last.Net > 0 ? "NET WIN · " + Net(last.Net) : last.Net < 0 ? "NET LOSS · " + Net(last.Net) : "EVEN · NO NET CHANGE";
            body.Add(Box(Text(headline, 23, last.Net > 0 ? Win : last.Net < 0 ? Loss : Gold), true));
            body.Add(Text($"{(last.IsBonus ? "Free spin" : "Paid spin")} · Bet {Cash(last.Bet)}\nCharged {Cash(last.Stake)} · Returned {Cash(last.Payout)}", 17));
            foreach (string item in last.Events) { body.Add(Text(item, 14, accent)); }
            if (last.BonusAwarded > 0) { body.Add(Text($"+{last.BonusAwarded} FREE SPINS AWARDED", 21, Win)); }
            body.Add(Text($"+{last.Award.PlayerExperience} XP · +{last.Award.SeasonExperience} season XP · +{last.Award.CoinsEarned} coins", 15, Gold));
            body.Add(Text($"Saved result · {last.CompletedAt.ToLocalTime():g}", 13, Muted));
        }
        else { body.Add(Text("Choose a play-cash wager. Your first result will appear here.", 16, Muted)); }
        bool locked = session.IsRoundActive || session.IsDiceTableActive;
        if (locked) { body.Add(Text("Finish your Blackjack round or paid Dice table before spinning.", 16, Gold)); }
        if (free == 0)
        {
            IReadOnlyList<decimal> bets = Bets(game);
            var picker = new Picker { Title = "Wager in play cash", TextColor = Colors.White, BackgroundColor = Panel, AutomationId = "SlotBet" };
            foreach (decimal amount in bets) { picker.Items.Add(Cash(amount)); }
            picker.SelectedIndex = bets.ToList().IndexOf(wager);
            picker.IsEnabled = !busy && !locked;
            picker.SelectedIndexChanged += (_, _) => { if (picker.SelectedIndex >= 0) { wager = bets[picker.SelectedIndex]; Render(); } };
            body.Add(picker);
        }
        var spin = Button(busy ? "SHOWING SAVED RESULT…" : free > 0 ? "PLAY FREE SPIN" : $"SPIN · {Cash(wager)}", "SpinSlot", async (_, _) => await Spin(), true);
        spin.IsEnabled = !busy && !locked && (free > 0 || session.Bankroll >= wager);
        body.Add(spin);
        if (!busy && !locked && free == 0 && session.Bankroll < wager)
        { body.Add(Text("Choose an affordable wager. If play cash is below $1 and no bonus is active, you can refill it.", 15, Muted)); }
        if (!busy && !locked && session.Bankroll < 1 && !session.HasSlotBonus)
        {
            body.Add(Button("REFILL PLAY CASH", "SlotRefill", (_, _) =>
            {
                try { session.RefillPracticeCash(); message = "Practice cash refilled. Your profile and statistics are retained."; }
                catch (InvalidOperationException exception) { message = exception.Message; }
                Render();
            }));
        }
        body.Add(Button("PAYTABLE & FEATURES", "SlotPaytable", (_, _) => { if (!busy) { paytable = true; Render(); } }));
        body.Add(Text("Each tap plays one spin. Leaving saves bonus progress. Reopening a result does not charge, draw or pay again.", 14, Muted));
        body.Add(Button("BACK TO FLOOR", "SlotBack", async (_, _) => await Navigation.PopAsync()));
    }

    private void AddGrid(MobileSlotReceipt? result)
    {
        int columns = game == MobileSlotGame.NeonVault ? 5 : 3;
        int rows = game == MobileSlotGame.Reels ? 1 : game == MobileSlotGame.NeonVault ? 4 : 3;
        var grid = new Grid { ColumnSpacing = 5, RowSpacing = 5 };
        for (int i = 0; i < columns; i++) { grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); }
        for (int i = 0; i < rows; i++) { grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
        for (int i = 0; i < columns * rows; i++)
        {
            int? symbol = result?.Symbols[i];
            string name = symbol is null ? "—" : SymbolName(game, symbol.Value);
            var content = new VerticalStackLayout { Spacing = 2, Padding = 3 };
            if (symbol.HasValue)
            { content.Add(new GraphicsView { Drawable = new SlotSymbolArt(game, symbol.Value), HeightRequest = rows == 1 ? 75 : 37, HorizontalOptions = LayoutOptions.Fill }); }
            else { content.Add(new BoxView { HeightRequest = rows == 1 ? 75 : 37, Color = Panel }); }
            Label label = Text(name, game == MobileSlotGame.NeonVault ? 10 : 13, CasinoFloorPage.Accent(game));
            label.HorizontalTextAlignment = TextAlignment.Center;
            content.Add(label);
            bool highlight = result?.HighlightCells.Contains(i) == true;
            var cell = new Border { Content = content, MinimumHeightRequest = rows == 1 ? 124 : 65,
                BackgroundColor = Panel, Stroke = new SolidColorBrush(highlight ? Gold : Color.FromArgb("#315244")),
                StrokeThickness = highlight ? 2 : 1, StrokeShape = new RoundRectangle { CornerRadius = 9 }, AutomationId = "SlotCell" + i };
            SemanticProperties.SetDescription(cell, $"Row {i / columns + 1}, reel {i % columns + 1}: {name}");
            cells.Add(cell); grid.Add(cell, i % columns, i / columns);
        }
        body.Add(grid);
    }

    private async Task Spin()
    {
        if (busy) { return; }
        busy = true;
        try
        {
            session.SpinSlot(game, wager);
            wager = session.SlotBet(game); message = null; Render();
            if (!session.Settings.ReduceSlotAnimations)
            {
                CancellationToken token = animation.Token;
                int columns = game == MobileSlotGame.NeonVault ? 5 : 3;
                View[] frames = cells.ToArray();
                foreach (View cell in frames) { cell.Opacity = .25; }
                try
                {
                    for (int reel = 0; reel < columns; reel++)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.WhenAll(frames.Where((_, index) => index % columns == reel)
                            .Select(cell => cell.FadeToAsync(1, 140, Easing.CubicOut)));
                    }
                }
                finally { foreach (View cell in frames) { cell.Opacity = 1; } }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { message = exception.Message.Split('\n')[0]; }
        finally { busy = false; Render(); }
    }

    private void Paytable()
    {
        body.Add(Text("PAYTABLE & FEATURES", 21, Gold));
        body.Add(Text("All returns include any returned stake. Your net result is the total return minus cash charged. Symbols use the original weighted Core draws.", 15, Muted));
        if (game == MobileSlotGame.Reels)
        {
            foreach (SlotSymbolDefinition symbol in SlotMachineRules.Symbols)
            { body.Add(Box(Text($"Three {symbol.Name} · {symbol.TripleMultiplier}×\n{symbol.Stops} of {SlotMachineRules.ReelStops} stops per reel", 18))); }
            body.Add(Text("Exactly two Cherries return 1×. One centre payline; all other non-triples return zero. Theoretical return for the original strips: 97.0458984375% over long play, not a promise for a session.", 15, Muted));
        }
        else if (game == MobileSlotGame.NeonVault)
        {
            body.Add(Text("1,024 ways, left to right from reel 1. Three or more consecutive matching reels pay; Wild substitutes. Each matching way multiplies the raw symbol award by the original Core factor 0.019, then all ways are added.", 15, Muted));
            foreach (NeonVaultSymbolDefinition symbol in NeonVaultRules.Symbols)
            { body.Add(Box(Text($"{symbol.Name} · Weight {symbol.Weight}/{NeonVaultRules.TotalWeight}\nRaw 3 / 4 / 5 reel awards: {symbol.ThreeReels} / {symbol.FourReels} / {symbol.FiveReels}×", 16))); }
            body.Add(Text("3 / 4 / 5+ Vault scatters: 2 / 10 / 50× plus 8 / 12 / 16 free spins. Bonus spins keep the trigger bet and can retrigger. Middle-reel Wild expansion is guaranteed in free spins, otherwise a 35% check. Blackouts add Wilds. Energy from bonus Wilds arms a next-spin Security Override at 12.", 15, Muted));
            body.Add(Text("Access Key jackpots: 3 keys Mini 25×, 4 Minor 75×, 5+ Major 250×. Five or more keys covering every reel give Grand 1,000×. Jackpot evaluation uses the original base grid.", 15, Muted));
        }
        else
        {
            body.Add(Text("Five lines: top, middle, bottom and both diagonals. Three matching symbols pay; Pounce Wild substitutes. Bonus symbols do not form line wins.", 15, Muted));
            foreach (NineLivesSymbolDefinition symbol in NineLivesRules.Symbols)
            { body.Add(Box(Text($"{symbol.Name} · {symbol.LineMultiplier}× per matching line\nPaid-spin weight {symbol.Weight}/{NineLivesRules.TotalWeight}", 17))); }
            body.Add(Text("Three or more Nine Lives symbols award nine free spins and can retrigger. The wager stays locked to the trigger bet. Bonus draws use the original separate bonus weights. If a Wild is present, Pounce checks 40% on paid spins and 65% on bonus spins to convert another eligible cell. Full Grid Frenzy returns the higher of 5,000× or the line total.", 15, Muted));
        }
        body.Add(Button("BACK TO MACHINE", "CloseSlotPaytable", (_, _) => { paytable = false; Render(); }));
    }

    private static IReadOnlyList<decimal> Bets(MobileSlotGame game) => game switch
    { MobileSlotGame.NeonVault => NeonVaultRules.Bets, MobileSlotGame.NineLives => NineLivesRules.Bets, _ => [1, 5, 10, 25, 50, 100, 250, 500, 1000] };
    private static string SymbolName(MobileSlotGame game, int symbol) => game switch
    {
        MobileSlotGame.Reels => SlotMachineRules.Symbols.Single(item => (int)item.Symbol == symbol).Name,
        MobileSlotGame.NeonVault => NeonVaultRules.Symbols.Single(item => (int)item.Symbol == symbol).Name,
        _ => NineLivesRules.Symbols.Single(item => (int)item.Symbol == symbol).Name,
    };
}
