using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;
using Microsoft.Maui.Controls.Shapes;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class SlotMachinePage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly MobileSlotGame game;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(16, 22), Spacing = 16, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private readonly List<SlotReelColumn> reels = [];
    private readonly VerticalStackLayout header = new() { Padding = new Thickness(18,10), Spacing = 5 };
    private readonly VerticalStackLayout dock = new() { Padding = new Thickness(14,10), Spacing = 8 };
    private readonly Grid root = new() { RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Star }, new RowDefinition { Height = GridLength.Auto } } };
    private CancellationTokenSource animation = new();
    private bool compact;
    private decimal wager;
    private bool busy;
    private bool paytable;
    private string? message;

    public SlotMachinePage(BlackjackSession session, MobileSlotGame game)
    {
        this.session = session; this.game = game; wager = session.SlotBet(game);
        Title = CasinoFloorPage.Name(game); BackgroundColor = Ink;
        StageStart = game == MobileSlotGame.NeonVault ? "#060A17" : "#100D18";
        StageEnd = game == MobileSlotGame.NeonVault ? "#14102A" : "#25122C";
        if (game == MobileSlotGame.NineLives)
        { var background = new Image { Source = "club_cats_background.png", Aspect = Aspect.AspectFill, Opacity = .45, InputTransparent = true }; Grid.SetRowSpan(background,3); root.Add(background); }
        header.BackgroundColor = Color.FromArgb("#090E16"); dock.Background = Gradient("#251A30", "#100E1A");
        root.Add(header); root.Add(new ScrollView { Content = body },0,1); root.Add(dock,0,2); Content = root;
    }
    protected override void OnAppearing()
    { base.OnAppearing(); animation.Dispose(); animation = new(); wager = session.SlotBet(game); Render(); }
    protected override void OnDisappearing() { animation.Cancel(); base.OnDisappearing(); }

    protected override void OnSizeAllocated(double width,double height)
    {base.OnSizeAllocated(width,height);bool next=width>height&&height<550;if(next!=compact){compact=next;Render();}}

    private void Render()
    {
        body.Children.Clear(); reels.Clear(); header.Children.Clear(); dock.Children.Clear();
        Color accent = CasinoFloorPage.Accent(game);
        header.Add(Columns(Metric("THE MIDNIGHT CLUB", Title!), Metric("PLAY CASH", Cash(session.Bankroll), Gold)));
        if (message is not null) { body.Add(Box(Text(message, 16, Gold))); }
        if (paytable) { Paytable(); return; }
        int free = session.FreeSlotSpins(game);
        if (free > 0)
        { body.Add(Pill($"{free} FREE SPINS · WAGER {Cash(session.SlotBet(game))}", Win)); }
        if (game == MobileSlotGame.NeonVault)
        { body.Add(Text($"Vault energy · {session.NeonEnergy} / {NeonVaultRules.VaultEnergyTarget}{(session.NeonOverrideArmed ? " · OVERRIDE READY" : "")}", 16, accent)); }
        MobileSlotReceipt? last = session.SlotHistory.FirstOrDefault(spin => spin.Game == game);
        AddGrid(last);
        if (last is not null)
        {
            string headline = last.Net > 0 ? "NET WIN · " + Net(last.Net) : last.Net < 0 ? "NET LOSS · " + Net(last.Net) : "EVEN · NO NET CHANGE";
            body.Add(Box(Text(headline, 22, last.Net > 0 ? Win : last.Net < 0 ? Loss : Gold), true));
            body.Add(Columns(Metric("CHARGED", Cash(last.Stake)), Metric("RETURNED", Cash(last.Payout), last.Payout > 0 ? Gold : Muted)));
            foreach (string item in last.Events.Take(2)) { body.Add(Text(item, 11, accent)); }
            if (last.BonusAwarded > 0) { body.Add(Text($"+{last.BonusAwarded} FREE SPINS AWARDED", 21, Win)); }
            body.Add(Text($"+{last.Award.PlayerExperience} XP · +{last.Award.SeasonExperience} SEASON XP · +{last.Award.CoinsEarned} COINS", 10, Gold));
            
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
            dock.Add(picker);
        }
        var spin = Button(busy ? "SPINNING…" : free > 0 ? "PLAY FREE SPIN" : $"SPIN · {Cash(wager)}", "SpinSlot", async (_, _) => await Spin(), true);
        spin.IsEnabled = !busy && !locked && (free > 0 || session.Bankroll >= wager);
        dock.Add(spin);
        if(compact&&dock.Children.Count==2){var first=(View)dock.Children[0];dock.Children.Clear();dock.Add(Columns(first,spin));}
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
        
        body.Add(Button("LEAVE MACHINE", "SlotBack", async (_, _) => await Navigation.PopAsync()));
    }

    private void AddGrid(MobileSlotReceipt? result)
    {
        int columns = game == MobileSlotGame.NeonVault ? 5 : 3;
        int rows = game == MobileSlotGame.Reels ? 3 : game == MobileSlotGame.NeonVault ? 4 : 3;
        double cellHeight = compact ? (game == MobileSlotGame.NeonVault ? 28 : 36) : game == MobileSlotGame.NeonVault ? 62 : game == MobileSlotGame.NineLives ? 78 : 58;
        var grid = new Grid { ColumnSpacing = game == MobileSlotGame.Reels ? 5 : 2, IsClippedToBounds = true };
        for (int reel = 0; reel < columns; reel++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            int[] symbols = game == MobileSlotGame.Reels
                ? new[] { (result?.Symbols[reel] ?? reel) + 1, result?.Symbols[reel] ?? 4, (result?.Symbols[reel] ?? reel) + 2 }.Select(i => i % 5).ToArray()
                : Enumerable.Range(0,rows).Select(row => result?.Symbols[row * columns + reel] ?? (row + reel) % 8).ToArray();
            var column = new SlotReelColumn(game, symbols, cellHeight); reels.Add(column); grid.Add(column,reel);
        }
        var cabinet = new Grid { IsClippedToBounds = true }; cabinet.Add(grid);
        if (game == MobileSlotGame.Reels)
        {
            cabinet.Add(new BoxView { HeightRequest = 1.5, Color = Gold, VerticalOptions = LayoutOptions.Center, InputTransparent = true });
            var line = Text("▸",20,Gold); line.VerticalOptions = LayoutOptions.Center; line.InputTransparent = true; cabinet.Add(line);
        }
        var bezel = new Border { Content = cabinet, Padding = game == MobileSlotGame.Reels ? 9 : 5,
            BackgroundColor = Color.FromArgb("#05060B"), Stroke = Gradient("#6D4A28", "#F1D393"), StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, Shadow = new Shadow { Brush = CasinoFloorPage.Accent(game), Opacity = .2f, Radius = 20 } };
        var shell = new VerticalStackLayout { Padding = 12, Spacing = 10 };
        var marquee = Text(game == MobileSlotGame.Reels ? "✦  M I D N I G H T  ✦" : game == MobileSlotGame.NeonVault ? "N E O N   V A U L T" : "N I N E   L I V E S", 17, CasinoFloorPage.Accent(game));
        marquee.HorizontalTextAlignment = TextAlignment.Center; shell.Add(marquee); shell.Add(bezel);
        var footer = Text(game == MobileSlotGame.Reels ? "THREE REELS  ·  ONE PAYLINE" : game == MobileSlotGame.NeonVault ? "1,024 WAYS  ·  VAULT CLASS" : "THREE REELS  ·  FIVE LUCKY LINES", 8,Gold);
        footer.HorizontalTextAlignment = TextAlignment.Center; shell.Add(footer);
        body.Add(new Border { Content = shell, Background = Gradient(game == MobileSlotGame.NeonVault ? "#18203D" : "#302238", "#100D19"),
            Stroke = new SolidColorBrush(CasinoFloorPage.Accent(game).WithAlpha(.6f)), StrokeShape = new RoundRectangle { CornerRadius = 22 }, StrokeThickness = 1,
            Shadow = new Shadow { Brush = Colors.Black, Radius = 16, Opacity = .45f, Offset = new Point(0,6) } });
    }

    private async Task Spin()
    {
        if (busy) { return; }
        busy = true;
        try
        {
            session.SpinSlot(game, wager);
            wager = session.SlotBet(game); message = null; Render();
            if (!session.Settings.ReduceSlotAnimations && !VisualMotion.Reduced())
            {
                CancellationToken token = animation.Token;
                var moving = reels.ToArray();
                await Task.WhenAll(moving.Select((reel,index) => reel.Spin(index,token)));
                MobileSlotReceipt? result = session.SlotHistory.FirstOrDefault(spin => spin.Game == game);
                if (result is { Payout: > 0 })
                {
                    VisualMotion.Celebrate(root, CasinoFloorPage.Accent(game));
                    await Task.WhenAll(moving.Select(reel => reel.WinPulse(Gold)));
                }
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
