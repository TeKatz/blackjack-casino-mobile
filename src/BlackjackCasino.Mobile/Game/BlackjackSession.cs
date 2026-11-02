using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Mobile.Game;

/// <summary>Transactional mobile state; Core owns rules, progression and rewards.</summary>
public sealed class BlackjackSession
{
    public const decimal StartingCash = 1_000m;
    private readonly Func<ICardShoe> createShoe;
    private readonly Action<MobileState>? persist;
    private MobileState state = new();
    private SavedShoe? shoe;

    public BlackjackSession(decimal bankroll = StartingCash, Func<ICardShoe>? createShoe = null,
        Action<MobileState>? persist = null)
    {
        if (bankroll < 0) { throw new ArgumentOutOfRangeException(nameof(bankroll)); }
        state.Bankroll = bankroll;
        this.createShoe = createShoe ?? (() => new Shoe(Rules));
        this.persist = persist;
        _ = new ProgressionService(Profile);
    }

    public GameRules Rules { get; } = new();
    public decimal Bankroll => state.Bankroll;
    public PlayerProfileState Profile => state.Profile;
    public MobileSettings Settings => state.Settings;
    public IReadOnlyList<RoundHistoryEntry> History => state.History;
    public ProgressionAward LastAward => state.LastAward;
    public int RevealedCards => state.RevealedCards;
    public BlackjackRound? Round { get; private set; }
    public bool IsRoundActive => Round is { Phase: not RoundPhase.Settled };
    public decimal Committed => Round is { Phase: not RoundPhase.Settled } round
        ? round.PlayerHands.Sum(hand => hand.Bet) + round.InsuranceBet : 0m;
    public decimal AvailableCash => Bankroll - Committed;
    public bool CanTakeInsurance => Round is { Phase: RoundPhase.InsuranceDecision } round
        && AvailableCash >= round.InitialBet / 2m;

    public IReadOnlyList<Card?> DealerCards
    {
        get
        {
            BlackjackRound? round = Round;
            return round is null ? [] : round.DealerHand.Cards
                .Select((card, index) => index == 0 || round.Phase == RoundPhase.Settled ? (Card?)card : null).ToArray();
        }
    }
    public HandValue VisibleDealerValue => HandEvaluator.Evaluate(DealerCards
        .Where(card => card.HasValue).Select(card => card!.Value));

    public MobileState Export() => MobileJson.Clone(state);

    public static BlackjackSession Restore(MobileState saved, Action<MobileState>? persist = null)
    {
        var session = new BlackjackSession(persist: persist);
        session.Load(saved);
        return session;
    }

    public void StartRound(decimal bet) => Change(() =>
    {
        if (IsRoundActive) { throw new InvalidOperationException("Finish your current round first."); }
        if (bet < Rules.MinimumBet || bet > Rules.MaximumBet || decimal.Truncate(bet) != bet)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Choose a whole-dollar bet from $1 to $1,000.");
        }
        if (bet > Bankroll) { throw new InvalidOperationException("There is not enough play cash for that bet."); }
        if (shoe is null || shoe.Remaining == 0 || shoe.State.Standard &&
            (shoe.Remaining < (Rules.MaxSplitHands + 1) * 21 || shoe.State.Position >= shoe.State.Cards.Count * Rules.Penetration))
        {
            ICardShoe fresh = createShoe();
            var deck = new List<Card>();
            while (fresh.Remaining > 0)
            {
                if (deck.Count >= 416) { throw new InvalidOperationException("Unexpected shoe size."); }
                deck.Add(fresh.Draw());
            }
            state.Shoe = new ShoeState { Cards = deck, Standard = fresh is Shoe };
            shoe = new SavedShoe(state.Shoe);
        }
        state.Journal = new RoundJournal { Bet = bet, BankrollBefore = Bankroll, ShoeStart = shoe.State.Position };
        state.Settings.LastWager = bet;
        state.LastAward = ProgressionAward.None;
        Round = new BlackjackRound(Rules, shoe, bet);
        Round.Start();
        ApplySettlement();
    });

    public IReadOnlySet<PlayerAction> AvailableActions()
    {
        BlackjackRound? round = Round;
        if (round is null) { return new HashSet<PlayerAction>(); }
        return round.GetAvailableActions().Where(action => action is not (PlayerAction.DoubleDown or PlayerAction.Split)
            || AvailableCash >= round.ActiveHand!.Bet).ToHashSet();
    }

    public void Act(PlayerAction action) => Change(() =>
    {
        if (!AvailableActions().Contains(action)) { throw new InvalidOperationException("That action is unavailable for this hand or balance."); }
        Round!.Act(action);
        state.Journal!.Commands.Add((ReplayCommand)(int)action);
        ApplySettlement();
    });

    public void TakeInsurance() => Change(() =>
    {
        if (!CanTakeInsurance) { throw new InvalidOperationException("Insurance is unavailable or unaffordable."); }
        Round!.TakeInsurance();
        state.Journal!.Commands.Add(ReplayCommand.TakeInsurance);
        ApplySettlement();
    });

    public void DeclineInsurance() => Change(() =>
    {
        if (Round is not { Phase: RoundPhase.InsuranceDecision }) { throw new InvalidOperationException("There is no insurance decision pending."); }
        Round.DeclineInsurance();
        state.Journal!.Commands.Add(ReplayCommand.DeclineInsurance);
        ApplySettlement();
    });

    public void RefillPracticeCash() => Change(() =>
    {
        if (IsRoundActive || Bankroll >= Rules.MinimumBet) { throw new InvalidOperationException("Refill is available only between rounds when cash is below the minimum bet."); }
        state.Bankroll = StartingCash;
        state.Journal = null;
        state.LastAward = ProgressionAward.None;
        Round = null;
    });

    public void Rename(string name) => Change(() => Profile.DisplayName = ProfileIdentity.Validate(name));
    public void UpdateSettings(bool largeCards, bool confirmSurrender) => Change(() =>
    {
        Settings.LargeCards = largeCards;
        Settings.ConfirmSurrender = confirmSurrender;
    });
    public void ClaimChallenge(string id) => Change(() =>
    {
        ChallengeDefinition? challenge = ChallengeCatalog.TryGet(id);
        if (challenge is null || !SupportedChallenge(challenge)) { throw new InvalidOperationException("This challenge is not available on mobile yet."); }
        var claim = new ProgressionService(Profile).ClaimChallenge(id);
        if (!claim.Success) { throw new InvalidOperationException(claim.Message); }
    });
    public static bool SupportedChallenge(ChallengeDefinition challenge) =>
        (challenge.Category is ChallengeCategory.Blackjack or ChallengeCategory.Lifetime)
        && challenge.Metric != ChallengeMetric.LifetimePlaytimeSeconds;
    public void PurchaseTitle(string id) => Change(() =>
    {
        if (CosmeticCatalog.TryGet(id) is not { Kind: CosmeticKind.Title }) { throw new InvalidOperationException("Only titles are available in this shop."); }
        var result = new ProgressionService(Profile).PurchaseCosmetic(id);
        if (!result.Success) { throw new InvalidOperationException(result.Message); }
        _ = new ProgressionService(Profile).EquipCosmetic(id);
    });
    public void EquipTitle(string id) => Change(() =>
    {
        if (CosmeticCatalog.TryGet(id) is not { Kind: CosmeticKind.Title } || !new ProgressionService(Profile).EquipCosmetic(id))
        { throw new InvalidOperationException("Unlock this title before equipping it."); }
    });
    public void SelectSeason(string id) => Change(() =>
    {
        if (!new ProgressionService(Profile).SelectSeason(id)) { throw new InvalidOperationException("Unknown season."); }
    });

    public void SetReducedPackAnimations(bool reduced) => Change(() => Settings.ReducePackAnimations = reduced);
    public void PurchasePack(CollectionRarity rarity) => Change(() =>
    {
        var result = CollectionService.PurchasePack(Profile, rarity);
        if (!result.Success) { throw new InvalidOperationException(result.Message); }
    });
    public void OpenPack(CollectionRarity rarity, Random? random = null) => Change(() =>
    {
        var result = CollectionService.OpenPack(Profile, rarity, random);
        if (!result.Success) { throw new InvalidOperationException(result.Message); }
        state.RevealedCards = 0;
    });
    public void RevealNextCard() => Change(() =>
    {
        if (Profile.Collection.PendingOpening is null || state.RevealedCards >= CollectionCatalog.CardsPerPack)
        { throw new InvalidOperationException("There is no hidden card left in this pack."); }
        state.RevealedCards++;
    });
    public void RevealAllCards() => Change(() =>
    {
        if (Profile.Collection.PendingOpening is null) { throw new InvalidOperationException("There is no pack to reveal."); }
        state.RevealedCards = CollectionCatalog.CardsPerPack;
    });
    public void CompletePack(Guid openingId) => Change(() =>
    {
        if (state.RevealedCards != CollectionCatalog.CardsPerPack || !CollectionService.CompleteOpening(Profile, openingId))
        { throw new InvalidOperationException("Reveal this pack's five cards before finishing it."); }
        state.RevealedCards = 0;
    });
    public void ClaimThemeReward(string themeId) => Change(() =>
    {
        var result = CollectionService.ClaimThemeCompletionReward(Profile, themeId);
        if (!result.Success) { throw new InvalidOperationException(result.Message); }
    });

    private void ApplySettlement()
    {
        RoundJournal? journal = state.Journal;
        if (journal is null || journal.Settled || Round?.Result is not { } result) { return; }
        state.Bankroll += result.TotalNet;
        var hands = Round.PlayerHands.Select((hand, index) => new HistoricalHand(hand.Bet, hand.IsSplitHand,
            hand.IsDoubled, hand.IsSurrendered, hand.Cards.ToArray(), result.Hands[index].Outcome, result.Hands[index].Net)).ToArray();
        var history = new RoundHistoryEntry(journal.Id, journal.StartedAt, DateTimeOffset.UtcNow, journal.BankrollBefore,
            Bankroll, journal.Bet, Round.InsuranceBet, result.InsuranceNet, result.TotalNet, Rules,
            Round.DealerHand.Cards.ToArray(), hands, Round.ActionHistory.ToArray(), []);
        state.LastAward = new ProgressionService(Profile).ProcessRound(history);
        state.History.Insert(0, history);
        if (state.History.Count > 20) { state.History.RemoveAt(20); }
        journal.Settled = true;
    }

    private void Change(Action change)
    {
        MobileState before = Export();
        try { change(); persist?.Invoke(Export()); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Load(before);
            throw new InvalidOperationException("Could not save. The action was rolled back. Check free storage and try again.", exception);
        }
        catch
        {
            if (MobileJson.Serialize(state) != MobileJson.Serialize(before)) { Load(before); }
            throw;
        }
    }

    private void Load(MobileState saved)
    {
        Validate(saved);
        state = MobileJson.Clone(saved);
        _ = new ProgressionService(Profile);
        if (state.RevealedCards is < 0 or > CollectionCatalog.CardsPerPack
            || state.RevealedCards != 0 && Profile.Collection.PendingOpening is null)
        { throw new InvalidDataException("Invalid saved pack reveal."); }
        shoe = state.Shoe is null ? null : new SavedShoe(state.Shoe);
        Round = null;
        RoundJournal? journal = state.Journal;
        if (journal is null) { return; }
        if (shoe is null || journal.Id == Guid.Empty || journal.Bet < 1 || journal.Bet > 1_000
            || decimal.Truncate(journal.Bet) != journal.Bet || journal.BankrollBefore < journal.Bet
            || journal.ShoeStart < 0 || journal.ShoeStart >= shoe.State.Cards.Count || journal.Commands is null || journal.Commands.Count > 150)
        { throw new InvalidDataException("Invalid round journal."); }
        int expectedPosition = shoe.State.Position;
        shoe.State.Position = journal.ShoeStart;
        var round = new BlackjackRound(Rules, shoe, journal.Bet);
        try
        {
            round.Start();
            foreach (ReplayCommand command in journal.Commands)
            {
                decimal reserved = round.PlayerHands.Sum(hand => hand.Bet) + round.InsuranceBet;
                if (command == ReplayCommand.TakeInsurance)
                {
                    if (journal.BankrollBefore - reserved < journal.Bet / 2) { throw new InvalidDataException("Unaffordable saved insurance."); }
                    round.TakeInsurance();
                }
                else if (command == ReplayCommand.DeclineInsurance) { round.DeclineInsurance(); }
                else if (command is ReplayCommand.Hit or ReplayCommand.Stand or ReplayCommand.DoubleDown or ReplayCommand.Split or ReplayCommand.Surrender)
                {
                    if ((command is ReplayCommand.DoubleDown or ReplayCommand.Split) &&
                        journal.BankrollBefore - reserved < (round.ActiveHand?.Bet ?? decimal.MaxValue))
                    { throw new InvalidDataException("Unaffordable saved action."); }
                    round.Act((PlayerAction)(int)command);
                }
                else { throw new InvalidDataException("Unknown saved action."); }
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        { throw new InvalidDataException("Saved round cannot be replayed.", exception); }
        if (shoe.State.Position != expectedPosition || (round.Result is not null) != journal.Settled
            || Bankroll != journal.BankrollBefore + (round.Result?.TotalNet ?? 0m))
        { throw new InvalidDataException("Saved round and wallet do not agree."); }
        if (journal.Settled && (state.History.Count == 0 || state.History[0].RoundId != journal.Id))
        { throw new InvalidDataException("Missing settled history receipt."); }
        Round = round;
    }

    private static void Validate(MobileState saved)
    {
        if (saved.Version != 1) { throw new NotSupportedException("This mobile save belongs to a newer app version."); }
        if (saved.Bankroll < 0 || saved.Profile is null || saved.Settings is null || saved.History is null
            || saved.History.Count > 20 || saved.LastAward is null || saved.Profile.Level is < 0 or > 100
            || saved.Profile.LevelExperience < 0 || saved.Profile.Coins < 0 || saved.Profile.LifetimeExperience < 0
            || saved.Settings.LastWager < 1 || saved.Settings.LastWager > 1_000 || decimal.Truncate(saved.Settings.LastWager) != saved.Settings.LastWager)
        { throw new InvalidDataException("Invalid mobile profile."); }
        if (saved.Shoe is { } deck && (deck.Cards is null || deck.Cards.Count is < 1 or > 416
            || deck.Position < 0 || deck.Position > deck.Cards.Count || deck.Cards.Any(card =>
                !Enum.IsDefined(card.Rank) || !Enum.IsDefined(card.Suit))))
        { throw new InvalidDataException("Invalid saved cards."); }
    }
}
