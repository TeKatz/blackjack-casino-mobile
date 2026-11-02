using BlackjackCasino.Core;

namespace BlackjackCasino.Mobile.Game;

/// <summary>In-memory practice wallet and round lifetime; all game rules live in Core.</summary>
public sealed class BlackjackSession
{
    public const decimal StartingCash = 1_000m;
    private readonly Func<ICardShoe> createShoe;
    private ICardShoe? shoe;
    private bool settlementApplied;

    public BlackjackSession(decimal bankroll = StartingCash, Func<ICardShoe>? createShoe = null)
    {
        if (bankroll < 0) { throw new ArgumentOutOfRangeException(nameof(bankroll)); }
        Bankroll = bankroll;
        this.createShoe = createShoe ?? (() => new Shoe(Rules));
    }

    public GameRules Rules { get; } = new();
    public decimal Bankroll { get; private set; }
    public BlackjackRound? Round { get; private set; }
    public bool IsRoundActive => Round is { Phase: not RoundPhase.Settled };
    public decimal Committed => Round is { Phase: not RoundPhase.Settled } round
        ? round.PlayerHands.Sum(hand => hand.Bet) + round.InsuranceBet : 0m;
    public decimal AvailableCash => Bankroll - Committed;
    public bool CanTakeInsurance => Round is { Phase: RoundPhase.InsuranceDecision } round
        && AvailableCash >= round.InitialBet / 2m;

    // Presentation must not expose the hole card or a total including it before settlement.
    public IReadOnlyList<Card?> DealerCards
    {
        get
        {
            BlackjackRound? round = Round;
            return round is null ? [] : round.DealerHand.Cards
                .Select((card, index) => index == 0 || round.Phase == RoundPhase.Settled ? (Card?)card : null)
                .ToArray();
        }
    }
    public HandValue VisibleDealerValue => HandEvaluator.Evaluate(DealerCards
        .Where(card => card.HasValue).Select(card => card!.Value));

    public void StartRound(decimal bet)
    {
        if (IsRoundActive) { throw new InvalidOperationException("Finish your current round first."); }
        if (bet < Rules.MinimumBet || bet > Rules.MaximumBet || decimal.Truncate(bet) != bet)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Choose a whole-dollar bet from $1 to $1,000.");
        }
        if (bet > Bankroll) { throw new InvalidOperationException("There is not enough practice cash for that bet."); }

        if (shoe is null || shoe.Remaining == 0 || shoe is Shoe { ReachedCutCard: true }
            || shoe is Shoe standard && standard.Remaining < (Rules.MaxSplitHands + 1) * 21)
        {
            shoe = createShoe();
        }
        var next = new BlackjackRound(Rules, shoe, bet);
        next.Start();
        Round = next;
        settlementApplied = false;
        ApplySettlement();
    }

    public IReadOnlySet<PlayerAction> AvailableActions()
    {
        BlackjackRound? round = Round;
        if (round is null) { return new HashSet<PlayerAction>(); }
        return round.GetAvailableActions().Where(action =>
            action is not (PlayerAction.DoubleDown or PlayerAction.Split)
            || AvailableCash >= round.ActiveHand!.Bet).ToHashSet();
    }

    public void Act(PlayerAction action)
    {
        if (!AvailableActions().Contains(action))
        {
            throw new InvalidOperationException("That action is unavailable for this hand or balance.");
        }
        Round!.Act(action);
        ApplySettlement();
    }

    public void TakeInsurance()
    {
        if (!CanTakeInsurance) { throw new InvalidOperationException("Insurance is unavailable or unaffordable."); }
        Round!.TakeInsurance();
        ApplySettlement();
    }

    public void DeclineInsurance()
    {
        if (Round is not { Phase: RoundPhase.InsuranceDecision })
        {
            throw new InvalidOperationException("There is no insurance decision pending.");
        }
        Round.DeclineInsurance();
        ApplySettlement();
    }

    public void RefillPracticeCash()
    {
        if (IsRoundActive || Bankroll >= Rules.MinimumBet)
        {
            throw new InvalidOperationException("Practice cash can only be refilled between rounds when the balance is below the minimum bet.");
        }
        Bankroll = StartingCash;
        Round = null;
    }

    private void ApplySettlement()
    {
        if (settlementApplied || Round?.Result is not { } result) { return; }
        // The bankroll reserves stakes during play; Core returns NET, not gross winnings.
        Bankroll += result.TotalNet;
        settlementApplied = true;
    }
}
