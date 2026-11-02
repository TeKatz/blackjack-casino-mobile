using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobileBlackjackSessionTests
{
    [Fact]
    public void NaturalBlackjack_CreditsNetOnce_AndRejectsFurtherActions()
    {
        var session = Session(100m, Rank.Ace, Rank.Ten, Rank.King, Rank.Seven);
        session.StartRound(10m);
        Assert.Equal(RoundPhase.Settled, session.Round!.Phase);
        Assert.Equal(115m, session.Bankroll);
        Assert.Equal(115m, session.AvailableCash);
        Assert.Equal(0m, session.Committed);
        Assert.Throws<InvalidOperationException>(() => session.Act(PlayerAction.Stand));
        Assert.Equal(115m, session.Bankroll);
    }

    [Fact]
    public void HoleCardAndDealerTotal_RemainHiddenUntilSettlement()
    {
        var session = Session(100m, Rank.Ten, Rank.Ten, Rank.Eight, Rank.Eight);
        session.StartRound(10m);
        Assert.Equal(2, session.DealerCards.Count);
        Assert.NotNull(session.DealerCards[0]);
        Assert.Null(session.DealerCards[1]);
        Assert.Equal(10, session.VisibleDealerValue.Total);
        Assert.Equal(90m, session.AvailableCash);
        session.Act(PlayerAction.Stand);
        Assert.All(session.DealerCards, card => Assert.NotNull(card));
        Assert.Equal(18, session.VisibleDealerValue.Total);
        Assert.Equal(100m, session.Bankroll);
    }

    [Fact]
    public void EntireBankrollWager_CannotDoubleOrSplit_AndCannotRestartActiveRound()
    {
        var session = Session(10m, Rank.Eight, Rank.Ten, Rank.Eight, Rank.Seven);
        session.StartRound(10m);
        BlackjackRound round = session.Round!;
        Assert.Equal(0m, session.AvailableCash);
        Assert.DoesNotContain(PlayerAction.DoubleDown, session.AvailableActions());
        Assert.DoesNotContain(PlayerAction.Split, session.AvailableActions());
        Assert.Throws<InvalidOperationException>(() => session.Act(PlayerAction.Split));
        Assert.Throws<InvalidOperationException>(() => session.Act(PlayerAction.DoubleDown));
        Assert.Throws<InvalidOperationException>(() => session.StartRound(1m));
        Assert.Throws<InvalidOperationException>(session.RefillPracticeCash);
        Assert.Same(round, session.Round);
        Assert.Single(round.PlayerHands);
        Assert.Equal(10m, round.PlayerHands[0].Bet);
    }

    [Fact]
    public void SplitAndDouble_ReserveExtraStakes_AndSettleBothHandsTogether()
    {
        var session = Session(30m, Rank.Eight, Rank.Ten, Rank.Eight, Rank.Six,
            Rank.Three, Rank.Two, Rank.Ten, Rank.Five);
        session.StartRound(10m);
        session.Act(PlayerAction.Split);
        Assert.Equal(20m, session.Committed);
        Assert.Equal(10m, session.AvailableCash);
        session.Act(PlayerAction.DoubleDown);
        Assert.Equal(1, session.Round!.ActiveHandIndex);
        Assert.Equal(30m, session.Committed);
        Assert.Equal(0m, session.AvailableCash);
        Assert.DoesNotContain(PlayerAction.DoubleDown, session.AvailableActions());
        session.Act(PlayerAction.Stand);
        Assert.Equal(2, session.Round!.Result!.Hands.Count);
        Assert.Equal(-10m, session.Round!.Result!.TotalNet);
        Assert.Equal(20m, session.Bankroll);
        Assert.Equal(20m, session.AvailableCash);
        Assert.Equal(0m, session.Committed);
    }

    [Fact]
    public void Insurance_ReservesHalfStake_AndUsesCoreNetPayout()
    {
        var session = Session(15m, Rank.Seven, Rank.Ace, Rank.Eight, Rank.King);
        session.StartRound(10m);
        Assert.Equal(RoundPhase.InsuranceDecision, session.Round!.Phase);
        Assert.True(session.CanTakeInsurance);
        Assert.Null(session.DealerCards[1]);
        session.TakeInsurance();
        Assert.Equal(RoundPhase.Settled, session.Round!.Phase);
        Assert.Equal(10m, session.Round!.Result!.InsuranceNet);
        Assert.Equal(0m, session.Round!.Result!.TotalNet);
        Assert.Equal(15m, session.Bankroll);
        Assert.Equal(0m, session.Committed);
        Assert.Throws<InvalidOperationException>(session.TakeInsurance);
        Assert.Equal(15m, session.Bankroll);
    }

    [Fact]
    public void UnaffordableInsurance_CanBeDeclined_AndEmptyWalletCanBeRefilled()
    {
        var session = Session(10m, Rank.Seven, Rank.Ace, Rank.Eight, Rank.King);
        session.StartRound(10m);
        Assert.False(session.CanTakeInsurance);
        Assert.Throws<InvalidOperationException>(session.TakeInsurance);
        session.DeclineInsurance();
        Assert.Equal(0m, session.Bankroll);
        session.RefillPracticeCash();
        Assert.Equal(BlackjackSession.StartingCash, session.AvailableCash);
        Assert.Null(session.Round);
    }

    [Fact]
    public void LosingInsurance_IsIncludedInReservedFundsBeforePlayerActions()
    {
        var session = Session(15m, Rank.Five, Rank.Ace, Rank.Six, Rank.Nine, Rank.Ten);
        session.StartRound(10m);
        session.TakeInsurance();
        Assert.Equal(RoundPhase.PlayerTurn, session.Round!.Phase);
        Assert.Equal(15m, session.Committed);
        Assert.Equal(0m, session.AvailableCash);
        Assert.DoesNotContain(PlayerAction.DoubleDown, session.AvailableActions());
        session.Act(PlayerAction.Hit);
        Assert.Equal(RoundPhase.Settled, session.Round!.Phase);
        Assert.Equal(20m, session.Bankroll);
        Assert.Equal(20m, session.AvailableCash);
    }

    [Fact]
    public void Bust_LosesOnlyStake_AndSurrenderLosesHalfStake()
    {
        var bust = Session(100m, Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight, Rank.King);
        bust.StartRound(10m);
        bust.Act(PlayerAction.Hit);
        Assert.Equal(90m, bust.Bankroll);
        Assert.Equal(0m, bust.Committed);
        var surrender = Session(100m, Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight);
        surrender.StartRound(10m);
        surrender.Act(PlayerAction.Surrender);
        Assert.Equal(95m, surrender.AvailableCash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void InvalidWagers_DoNotCreateRoundOrChangeWallet(int wager)
    {
        var session = new BlackjackSession();
        Assert.Throws<ArgumentOutOfRangeException>(() => session.StartRound(wager));
        Assert.Null(session.Round);
        Assert.Equal(1_000m, session.Bankroll);
    }

    [Fact]
    public void FractionalWagers_AndUnaffordableWagers_AreRejected()
    {
        var session = new BlackjackSession(10m);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.StartRound(1.001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.StartRound(1.50m));
        Assert.Throws<InvalidOperationException>(() => session.StartRound(11m));
        Assert.Throws<InvalidOperationException>(session.RefillPracticeCash);
        Assert.Equal(10m, session.AvailableCash);
    }

    [Fact]
    public void SubsequentRounds_ReuseShoe_AndDoNotRecreditPreviousResult()
    {
        int created = 0;
        var shoe = new FixedShoe(Rank.Ace, Rank.Ten, Rank.King, Rank.Seven,
            Rank.Ten, Rank.Ten, Rank.Eight, Rank.Eight);
        var session = new BlackjackSession(100m, () => { created++; return shoe; });
        session.StartRound(10m);
        Assert.Equal(115m, session.Bankroll);
        session.StartRound(10m);
        Assert.Equal(115m, session.Bankroll);
        session.Act(PlayerAction.Stand);
        Assert.Equal(115m, session.AvailableCash);
        Assert.Equal(1, created);
    }

    private static BlackjackSession Session(decimal cash, params Rank[] cards) =>
        new(cash, () => new FixedShoe(cards));

    private sealed class FixedShoe(params Rank[] ranks) : ICardShoe
    {
        private readonly Queue<Card> cards = new(ranks.Select(rank => new Card(rank, Suit.Spades)));
        public int Remaining => cards.Count;
        public Card Draw() => cards.Dequeue();
    }
}
