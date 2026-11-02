using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public sealed class DiceRoomPage : CasinoPage
{
    private readonly BlackjackSession session;
    private readonly VerticalStackLayout body = new()
    { Padding = new Thickness(20, 24), Spacing = 18, MaximumWidthRequest = 620, HorizontalOptions = LayoutOptions.Center };
    private string? message;
    private decimal stake = KniffelTable.Stakes[0];
    private decimal? confirmedStake;
    private bool restartCasual;
    private bool opening;
    private bool shop;

    public DiceRoomPage(BlackjackSession session)
    {
        this.session = session;
        Title = "Midnight Dice";
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
    }

    protected override void OnAppearing() { base.OnAppearing(); confirmedStake = null; restartCasual = false; Render(); }

    private void Render()
    {
        body.Children.Clear();
        body.Add(Header("THE DICE LOUNGE", "A little luck. A little strategy.", null, "club_nav_challenges.png"));
        body.Add(Text("Five dice. Thirteen boxes. Your next move.", 18));
        body.Add(Text($"{Cash(session.Bankroll)} play cash · {session.Profile.Coins:N0} coins", 17, Muted));
        if (message is not null) { body.Add(Box(Text(message, 16, Gold))); }
        if (shop) { Shop(); return; }

        var casual = new VerticalStackLayout { Spacing = 12 };
        casual.Add(Text("CASUAL", 22, Gold));
        casual.Add(Text("No entry fee. Take your time, save any turn and earn XP when you finish.", 16, Muted));
        casual.Add(Button(session.HasCasualDice ? "RESUME CASUAL GAME" : "START CASUAL GAME", "DiceCasual", async (_, _) =>
            await Open(false, () => { if (!session.HasCasualDice) { session.StartCasualDice(); } }), true));
        if (session.CasualDice is { Entries.Length: 13 })
        { casual.Add(Button("LAST CASUAL RESULT", "LastCasualDice", async (_, _) => await Open(false))); }
        if (session.HasCasualDice)
        {
            casual.Add(Button(restartCasual ? "CONFIRM · DISCARD CASUAL PROGRESS" : "START CASUAL OVER", "RestartCasualDice", async (_, _) =>
            {
                if (!restartCasual) { restartCasual = true; Render(); }
                else { await Open(false, () => session.StartCasualDice(true)); }
            }));
            if (restartCasual)
            {
                casual.Add(Text("Your current casual sheet will be replaced. No completion XP is awarded.", 15, Loss));
                casual.Add(Button("KEEP CURRENT GAME", "KeepCasualDice", (_, _) => { restartCasual = false; Render(); }));
            }
        }
        body.Add(Box(casual));

        var casino = new VerticalStackLayout { Spacing = 12 };
        casino.Add(Text("CASINO TABLE", 22, Gold));
        casino.Add(Text("Play 13 turns against Mara, Felix and June. Your entry fee uses play cash; Coins stay in your cosmetic and pack wallet.", 16, Muted));
        if (session.IsDiceTableActive)
        {
            KniffelTableState saved = session.DiceTable!;
            casino.Add(Text($"Entry {Cash(saved.Stake)} already paid · {saved.Player.Entries.Length}/13 turns scored", 16));
            casino.Add(Button("RESUME CASINO TABLE", "ResumeDiceTable", async (_, _) => await Open(true), true));
        }
        else
        {
            if (session.DiceTable is { Settled: true })
            { casino.Add(Button("LAST CASINO RESULT", "LastCasinoDice", async (_, _) => await Open(true))); }
            var picker = new Picker { Title = "Entry fee in play cash", TextColor = Colors.White, BackgroundColor = Panel, AutomationId = "DiceStake" };
            foreach (decimal amount in KniffelTable.Stakes) { picker.Items.Add(Cash(amount)); }
            picker.SelectedIndex = KniffelTable.Stakes.ToList().IndexOf(stake);
            picker.SelectedIndexChanged += (_, _) =>
            {
                if (picker.SelectedIndex >= 0) { stake = KniffelTable.Stakes[picker.SelectedIndex]; confirmedStake = null; Render(); }
            };
            casino.Add(picker);
            casino.Add(Text("PAYOUTS · includes any returned entry fee", 13, Gold));
            for (int place = 1; place <= 4; place++)
            { casino.Add(Text($"Place {place} · {Cash(KniffelTable.PrizeForPlace(stake, place))}", 16)); }
            casino.Add(Text("Tied scores share the prizes for their occupied places, rounded down to cents. Leaving saves the table; forfeiting loses the entry fee.", 14, Muted));
            if (session.IsRoundActive || session.HasSlotBonus)
            { casino.Add(Text("Finish your saved Blackjack round or slot bonus to enter this table. You can play Casual Dice now.", 16, Gold)); }
            if (confirmedStake == stake)
            {
                casino.Add(Text($"Pay {Cash(stake)} play cash and take your seat?", 19, Gold));
                casino.Add(Button("CONFIRM ENTRY", "ConfirmDiceEntry", async (_, _) => await Open(true, () => session.StartDiceTable(stake)), true));
                casino.Add(Button("CANCEL", "CancelDiceEntry", (_, _) => { confirmedStake = null; Render(); }));
            }
            else
            {
                var enter = Button($"ENTER TABLE · {Cash(stake)}", "EnterDiceTable", (_, _) => { confirmedStake = stake; Render(); }, true);
                enter.IsEnabled = !session.IsRoundActive && !session.HasSlotBonus && session.Bankroll >= stake;
                casino.Add(enter);
                if (session.Bankroll < stake) { casino.Add(Text("Choose a lower entry fee or build your play cash at Blackjack.", 15, Muted)); }
            }
        }
        body.Add(Box(casino));
        body.Add(Button("DICE COLLECTION & SHOP", "OpenDiceShop", (_, _) => { shop = true; Render(); }));
        body.Add(Text("CLASSIC RULES", 15, Gold));
        body.Add(Text("Up to three rolls per turn. Tap dice to hold them, then record one open box. Upper section: reach 63 for +35. Full House 25, Small Straight 30, Large Straight 40, Five of a Kind 50. Further committed five-of-a-kinds give +50 and the maximum in any open box, using the existing classic rules.", 15, Muted));
        body.Add(Button("BACK TO LOBBY", "DiceRoomBack", async (_, _) => await Navigation.PopAsync()));
    }

    private void Shop()
    {
        body.Add(Text("DICE COLLECTION", 24, Gold));
        body.Add(Text("Appearance only. Every set uses identical rules and dice chances.", 16, Muted));
        foreach (CosmeticDefinition item in CosmeticCatalog.All.Where(item => item.Kind == CosmeticKind.DiceSet
            && (item.ShopAvailable || session.Profile.OwnedCosmeticIds.Contains(item.Id))))
        {
            bool owned = session.Profile.OwnedCosmeticIds.Contains(item.Id);
            bool equipped = session.Profile.EquippedDiceSetId == item.Id;
            var row = new VerticalStackLayout { Spacing = 12 };
            row.Add(Text(item.Name, 22, Gold));
            var sample = new HorizontalStackLayout { Spacing = 8 };
            foreach (int value in new[] { 1, 5, 6 })
            { var die = DiceFace.Create(value, item.VisualVariant); die.WidthRequest = 62; sample.Add(die); }
            row.Add(sample);
            row.Add(Text(item.Description, 15, Muted));
            var action = Button(equipped ? "EQUIPPED" : owned ? "EQUIP" : $"BUY & EQUIP · {item.Price:N0} COINS", "DiceSet-" + item.Id,
                (_, _) =>
                {
                    try
                    {
                        if (owned) { session.EquipDiceSet(item.Id); } else { session.PurchaseDiceSet(item.Id); }
                        message = item.Name + " equipped.";
                    }
                    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                    { message = exception.Message.Split('\n')[0]; }
                    Render();
                });
            action.IsEnabled = !equipped && (owned || session.Profile.Coins >= item.Price && session.Profile.Prestige >= item.RequiredPrestige);
            row.Add(action);
            body.Add(Box(row));
        }
        body.Add(Button("BACK TO DICE ROOM", "CloseDiceShop", (_, _) => { shop = false; Render(); }));
    }

    private async Task Open(bool casino, Action? prepare = null)
    {
        if (opening) { return; }
        opening = true;
        try { prepare?.Invoke(); message = null; await Navigation.PushAsync(new DiceGamePage(session, casino)); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { message = exception.Message.Split('\n')[0]; Render(); }
        finally { opening = false; }
    }
}
