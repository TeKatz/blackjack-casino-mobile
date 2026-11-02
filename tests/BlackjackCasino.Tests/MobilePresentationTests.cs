using BlackjackCasino.Core;
using BlackjackCasino.Mobile.Game;

namespace BlackjackCasino.Tests;

public sealed class MobilePresentationTests
{
    [Fact]
    public void AppearancePurchaseRollsBackOnSaveFailureAndNeverChangesAnOpenHand()
    {
        bool fail=false;
        var session=new BlackjackSession(createShoe:()=>new FixedShoe(),persist:_=>{if(fail){throw new IOException("full");}});
        session.Profile.Coins=3000;
        session.StartRound(1);
        string before=MobileJson.Serialize(session.Export());
        fail=true;
        Assert.Throws<InvalidOperationException>(()=>session.UseAppearance("cardset-art-deco",true));
        Assert.Equal(before,MobileJson.Serialize(session.Export()));
        fail=false;session.UseAppearance("cardset-art-deco",true);
        Assert.Equal(1200,session.Profile.Coins);Assert.Equal(1000m,session.Bankroll);
        Assert.Equal("cardset-art-deco",session.Profile.EquippedCardSetId);
        var cards=session.DealerCards.ToArray();
        session.SetReducedInterfaceMotion(true);
        session=BlackjackSession.Restore(session.Export());
        Assert.Equal(cards,session.DealerCards);Assert.True(session.Settings.ReduceInterfaceMotion);
        Assert.Equal("cardset-art-deco",session.Profile.EquippedCardSetId);
    }
    private sealed class FixedShoe : ICardShoe
    {
        private readonly Queue<Card> cards=new(new[]{Rank.Five,Rank.Nine,Rank.Six,Rank.Seven,Rank.Ten}.Select(rank=>new Card(rank,Suit.Spades)));
        public int Remaining=>cards.Count;
        public Card Draw()=>cards.Dequeue();
    }
}
