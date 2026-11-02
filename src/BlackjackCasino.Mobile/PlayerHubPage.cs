using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Mobile.Game;
using static BlackjackCasino.Mobile.CasinoStyle;

namespace BlackjackCasino.Mobile;

public enum HubSection { Profile, Challenges, Season, Shop, Settings }

public sealed class PlayerHubPage : ContentPage
{
    private readonly BlackjackSession session;
    private readonly HubSection section;
    private readonly VerticalStackLayout body = new()
    {
        Padding = new Thickness(22, 24), Spacing = 18, MaximumWidthRequest = 620,
        HorizontalOptions = LayoutOptions.Center,
    };
    private string? message;
    private int page;
    private string query = "";

    public PlayerHubPage(BlackjackSession session, HubSection section)
    {
        this.session = session;
        this.section = section;
        Title = section switch { HubSection.Profile => "Your profile", HubSection.Season => "Season pass",
            HubSection.Shop => "Title shop", HubSection.Settings => "Settings", _ => "Challenges" };
        BackgroundColor = Ink;
        Content = new ScrollView { Content = body };
        Render();
    }

    private void Render()
    {
        body.Children.Clear();
        body.Add(Text(Title ?? "Player hub", 30, Gold));
        body.Add(Text($"Level {session.Profile.Level} · {session.Profile.Coins:N0} coins", 17, Muted));
        if (message is not null) { body.Add(Box(Text(message, 16, Gold))); }
        switch (section)
        {
            case HubSection.Profile: Profile(); break;
            case HubSection.Challenges: Challenges(); break;
            case HubSection.Season: Season(); break;
            case HubSection.Shop: Shop(); break;
            case HubSection.Settings: Settings(); break;
        }
        body.Add(Button("BACK", "HubBack", async (_, _) => await Navigation.PopAsync()));
    }

    protected override void OnAppearing() { base.OnAppearing(); Render(); }

    private void Profile()
    {
        CosmeticDefinition title = CosmeticCatalog.Get(session.Profile.EquippedTitleId);
        body.Add(Text(session.Profile.DisplayName, 26));
        body.Add(Text(title.Name, 20, LobbyPage.TitleColor(title.VisualVariant)));
        body.Add(LobbyPage.LevelProgress(session));
        StatisticsSnapshot stats = session.Profile.LifetimeStatistics;
        var values = new VerticalStackLayout { Spacing = 10 };
        values.Add(Text($"Cash · {Cash(session.Bankroll)}", 20, Gold));
        values.Add(Text($"Rounds {stats.RoundsPlayed:N0} · Hands {stats.HandsPlayed:N0}"));
        values.Add(Text($"Won {stats.HandsWon:N0} · Lost {stats.HandsLost:N0} · Pushes {stats.Pushes:N0}"));
        values.Add(Text($"Blackjacks {stats.Blackjacks:N0} · Best win streak {stats.BestWinStreak:N0}"));
        values.Add(Text($"Wagered {Cash(stats.TotalWagered)} · Net {Net(stats.NetProfit)}"));
        values.Add(Text($"Biggest win {Cash(stats.BiggestWin)} · Biggest loss {Cash(stats.BiggestLoss)}"));
        values.Add(Text($"Doubles {stats.Doubles:N0} · Splits {stats.Splits:N0} · Surrenders {stats.Surrenders:N0}"));
        values.Add(Text($"Lifetime XP · {session.Profile.LifetimeExperience:N0}"));
        body.Add(Box(values));
        KniffelStatistics dice = session.Profile.Kniffel;
        var diceStats = new VerticalStackLayout { Spacing = 10 };
        diceStats.Add(Text("MIDNIGHT DICE", 20, Gold));
        diceStats.Add(Text($"Completed games {dice.CompletedGames:N0} · Best score {dice.BestScore:N0}"));
        diceStats.Add(Text($"Average score {(dice.CompletedGames == 0 ? 0 : (double)dice.TotalScore / dice.CompletedGames):N1}"));
        diceStats.Add(Text($"Five of a Kind {dice.FiveOfAKinds:N0} · Upper bonuses {dice.UpperBonuses:N0}"));
        diceStats.Add(Text($"Full Houses {dice.FullHouses:N0} · Large Straights {dice.LargeStraights:N0}"));
        diceStats.Add(Text($"Casino starts {dice.CasinoGamesStarted:N0} · Completed {dice.CasinoGamesCompleted:N0}"));
        diceStats.Add(Text($"Wins {dice.CasinoWins:N0} · Top-two finishes {dice.PodiumFinishes:N0} · Forfeits {dice.Forfeits:N0}"));
        diceStats.Add(Text($"Wagered {Cash(dice.TotalWagered)} · Returned {Cash(dice.TotalReturned)}"));
        diceStats.Add(Text($"Net {Net(dice.NetProfit)} · Biggest payout {Cash(dice.BiggestPayout)}"));
        body.Add(Box(diceStats));
        var slots = new VerticalStackLayout { Spacing = 10 };
        slots.Add(Text("CASINO FLOOR", 20, Gold));
        slots.Add(Text($"Midnight Reels · {session.Profile.Slots.Spins:N0} spins · Net {Net(session.Profile.Slots.NetProfit)}"));
        slots.Add(Text($"Neon Vault · {session.Profile.NeonVault.Spins:N0} spins · Net {Net(session.Profile.NeonVault.NetProfit)}"));
        slots.Add(Text($"Nine Lives · {session.Profile.NineLives.Spins:N0} spins · Net {Net(session.Profile.NineLives.NetProfit)}"));
        body.Add(Box(slots));
        body.Add(Button("FLOOR STATISTICS & SPIN HISTORY", "ProfileSlots", async (_, _) => await Navigation.PushAsync(new CasinoFloorPage(session, true))));
        body.Add(Button("OPEN MIDNIGHT DICE", "ProfileDice", async (_, _) => await Navigation.PushAsync(new DiceRoomPage(session))));
        body.Add(Button($"ALBUM · {session.Profile.Collection.OwnedCardIds.Count}/{CollectionCatalog.Cards.Count} CARDS · {session.Profile.Collection.Packs.Values.Sum()} PACKS",
            "ProfileAlbum", async (_, _) => await Navigation.PushAsync(new AlbumPage(session))));
        body.Add(Text("RECENT ROUNDS", 14, Gold));
        if (session.History.Count == 0) { body.Add(Text("Your first completed round will appear here.", 16, Muted)); }
        foreach (RoundHistoryEntry round in session.History.Take(10))
        {
            var row = new VerticalStackLayout { Spacing = 6 };
            row.Add(Text($"{round.CompletedAt.ToLocalTime():g} · {Net(round.TotalNet)}", 18, round.TotalNet >= 0 ? Win : Loss));
            row.Add(Text($"Bet {Cash(round.InitialBet)} · {round.PlayerHands.Count} hand(s) · Cash {Cash(round.BankrollAfter)}", 14, Muted));
            body.Add(Box(row));
        }
    }

    private void Search()
    {
        var entry = new SearchBar { Text = query, Placeholder = "Search, then tap Search", TextColor = Colors.White,
            PlaceholderColor = Muted, BackgroundColor = Panel, AutomationId = "HubSearch" };
        entry.SearchButtonPressed += (_, _) => { query = entry.Text ?? ""; page = 0; Render(); };
        body.Add(entry);
    }

    private void Challenges()
    {
        Search();
        ChallengeDefinition[] all = ChallengeCatalog.All.Where(BlackjackSession.SupportedChallenge)
            .Where(item => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => session.Profile.ClaimedChallengeIds.Contains(item.Id) ? 2
                : ChallengeCatalog.Complete(session.Profile, item) ? 0 : 1).ToArray();
        body.Add(Text("Blackjack, Midnight Dice, Midnight Reels, Neon Vault, Nine Lives and supported lifetime goals. Rewards use the existing Core, including packs you can open in the Album.", 15, Muted));
        foreach (ChallengeDefinition challenge in all.Skip(page * 10).Take(10))
        {
            bool claimed = session.Profile.ClaimedChallengeIds.Contains(challenge.Id);
            decimal progress = Math.Min(challenge.Goal, ChallengeCatalog.Progress(session.Profile, challenge));
            var row = new VerticalStackLayout { Spacing = 10 };
            row.Add(Text(challenge.Name, 20, Gold));
            row.Add(Text(challenge.Description, 15, Muted));
            row.Add(Text($"{progress:N0} / {challenge.Goal:N0} · {challenge.CoinReward:N0} coins + pack", 15));
            var claim = Button(claimed ? "CLAIMED" : "CLAIM REWARD", "Claim-" + challenge.Id,
                (_, _) => Run(() => session.ClaimChallenge(challenge.Id), "Reward saved to your profile."));
            claim.IsEnabled = !claimed && progress >= challenge.Goal;
            row.Add(claim);
            body.Add(Box(row));
        }
        Pager(all.Length);
    }

    private void Season()
    {
        var picker = new Picker { Title = "Choose a season", TextColor = Colors.White, BackgroundColor = Panel };
        foreach (SeasonDefinition item in SeasonCatalog.All) { picker.Items.Add(item.Name); }
        picker.SelectedIndex = SeasonCatalog.All.ToList().FindIndex(item => item.Id == session.Profile.ActiveSeasonId);
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedIndex < 0) { return; }
            page = 0;
            Run(() => session.SelectSeason(SeasonCatalog.All[picker.SelectedIndex].Id), "Active season saved.");
        };
        body.Add(picker);
        SeasonDefinition season = SeasonCatalog.Get(session.Profile.ActiveSeasonId);
        SeasonProgressState progress = session.Profile.Seasons[season.Id];
        body.Add(Text(season.Description, 16, Muted));
        body.Add(Text($"Tier {progress.Tier} / {season.Tiers.Count} · {progress.Experience:N0} season XP", 21, Gold));
        body.Add(Text("Rewards unlock automatically across Blackjack, Midnight Dice and the Casino Floor. Packs are available in the Album; dice sets can be equipped in the Dice room. Other cosmetics remain saved for later integrations.", 15, Muted));
        foreach (SeasonTierDefinition tier in season.Tiers.Skip(page * 10).Take(10))
        {
            string cosmetic = tier.CosmeticId is null ? "" : $" · {CosmeticCatalog.Get(tier.CosmeticId).Name}";
            body.Add(Box(Text($"{(tier.Tier <= progress.Tier ? "✓" : "○")} Tier {tier.Tier} · {tier.RequiredExperience:N0} XP\n{tier.Coins:N0} coins{cosmetic}", 17,
                tier.Tier <= progress.Tier ? Win : Muted)));
        }
        Pager(season.Tiers.Count);
    }

    private void Shop()
    {
        Search();
        body.Add(Text("Permanent cosmetic titles. Coins are earned from levels, season tiers and challenges.", 15, Muted));
        body.Add(Button("DICE SETS · VISIT MIDNIGHT DICE", "ShopDice", async (_, _) => await Navigation.PushAsync(new DiceRoomPage(session))));
        CosmeticDefinition[] titles = CosmeticCatalog.All.Where(item => item.Kind == CosmeticKind.Title
            && (item.ShopAvailable || session.Profile.OwnedCosmeticIds.Contains(item.Id)))
            .Where(item => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => session.Profile.OwnedCosmeticIds.Contains(item.Id) ? 0 : 1).ThenBy(item => item.Price).ToArray();
        foreach (CosmeticDefinition title in titles.Skip(page * 10).Take(10))
        {
            bool owned = session.Profile.OwnedCosmeticIds.Contains(title.Id);
            bool equipped = session.Profile.EquippedTitleId == title.Id;
            var row = new VerticalStackLayout { Spacing = 10 };
            row.Add(Text(title.Name, 22, LobbyPage.TitleColor(title.VisualVariant)));
            row.Add(Text(title.Description, 15, Muted));
            var button = Button(equipped ? "EQUIPPED" : owned ? "EQUIP" : $"BUY · {title.Price:N0} COINS", "Title-" + title.Id,
                (_, _) => Run(() =>
                {
                    if (!owned) { session.PurchaseTitle(title.Id); }
                    else { session.EquipTitle(title.Id); }
                }, "Title saved and equipped."));
            button.IsEnabled = !equipped && (owned || session.Profile.Coins >= title.Price && session.Profile.Prestige >= title.RequiredPrestige);
            row.Add(button);
            body.Add(Box(row));
        }
        Pager(titles.Length);
    }

    private void Settings()
    {
        var name = new Entry { Text = session.Profile.DisplayName, MaxLength = 48, TextColor = Colors.White,
            BackgroundColor = Panel, Placeholder = "Player name", AutomationId = "ProfileName" };
        body.Add(Text("PLAYER NAME", 14, Gold));
        body.Add(name);
        body.Add(Button("SAVE NAME", "SaveName", (_, _) => Run(() => session.Rename(name.Text ?? ""), "Name saved.")));
        AddSwitch("Larger playing cards", session.Settings.LargeCards,
            value => session.UpdateSettings(value, session.Settings.ConfirmSurrender), "LargeCards");
        AddSwitch("Confirm before surrender", session.Settings.ConfirmSurrender,
            value => session.UpdateSettings(session.Settings.LargeCards, value), "ConfirmSurrender");
        AddSwitch("Reduce pack animations", session.Settings.ReducePackAnimations,
            session.SetReducedPackAnimations, "ReducePackAnimations");
        AddSwitch("Reduce dice animations", session.Settings.ReduceDiceAnimations,
            session.SetReducedDiceAnimations, "ReduceDiceAnimations");
        AddSwitch("Reduce slot animations", session.Settings.ReduceSlotAnimations,
            session.SetReducedSlotAnimations, "ReduceSlotAnimations");
        body.Add(Text("Every accepted action saves your cash, profile, open Blackjack hand and Dice games on this device. A previous-save backup is kept for recovery. Uninstalling removes local data.", 15, Muted));
        body.Add(Text("Mobile preview · 0.6.0", 14, Gold));
    }

    private void AddSwitch(string caption, bool enabled, Action<bool> changed, string id)
    {
        var toggle = new Switch { IsToggled = enabled, OnColor = Gold, AutomationId = id };
        toggle.Toggled += (_, args) => Run(() => changed(args.Value), "Setting saved.");
        var row = new HorizontalStackLayout { Spacing = 16, Children = { toggle, Text(caption) } };
        body.Add(Box(row));
    }

    private void Pager(int count)
    {
        int pages = Math.Max(1, (count + 9) / 10);
        body.Add(Text($"Page {page + 1} of {pages}", 14, Muted));
        if (page > 0) { body.Add(Button("PREVIOUS", "PreviousPage", (_, _) => { page--; Render(); })); }
        if (page + 1 < pages) { body.Add(Button("NEXT", "NextPage", (_, _) => { page++; Render(); })); }
    }

    private void Run(Action action, string success)
    {
        try { action(); message = success; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        { message = exception.Message.Split('\n')[0]; }
        Render();
    }
}
