using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Mobile.Game;

public sealed class MobileSettings
{
    public bool ReducePackAnimations { get; set; }
    public bool ReduceDiceAnimations { get; set; }
    public bool LargeCards { get; set; }
    public bool ConfirmSurrender { get; set; } = true;
    public decimal LastWager { get; set; } = 25m;
}

public enum ReplayCommand { Hit, Stand, DoubleDown, Split, Surrender, TakeInsurance, DeclineInsurance }

public sealed class ShoeState
{
    public List<Card> Cards { get; set; } = [];
    public int Position { get; set; }
    public bool Standard { get; set; }
}

public sealed class RoundJournal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal BankrollBefore { get; set; }
    public decimal Bet { get; set; }
    public int ShoeStart { get; set; }
    public List<ReplayCommand> Commands { get; set; } = [];
    public bool Settled { get; set; }
}

public sealed class MobileState
{
    public int Version { get; set; } = 1;
    public decimal Bankroll { get; set; } = BlackjackSession.StartingCash;
    public PlayerProfileState Profile { get; set; } = new();
    public MobileSettings Settings { get; set; } = new();
    public ShoeState? Shoe { get; set; }
    public RoundJournal? Journal { get; set; }
    public List<RoundHistoryEntry> History { get; set; } = [];
    public int RevealedCards { get; set; }
    public ProgressionAward LastAward { get; set; } = ProgressionAward.None;
    public KniffelState? CasualDice { get; set; }
    public KniffelTableState? DiceTable { get; set; }
    public ProgressionAward CasualDiceAward { get; set; } = ProgressionAward.None;
    public ProgressionAward DiceTableAward { get; set; } = ProgressionAward.None;
}

public static class MobileJson
{
    public static readonly JsonSerializerOptions Options = new() { MaxDepth = 64 };
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Deserialize<T>(string text) => JsonSerializer.Deserialize<T>(text, Options)
        ?? throw new InvalidDataException("Empty saved data.");
    public static MobileState Clone(MobileState state) => Deserialize<MobileState>(Serialize(state));
}

internal sealed class SavedShoe(ShoeState state) : ICardShoe
{
    public ShoeState State { get; } = state;
    public int Remaining => State.Cards.Count - State.Position;
    public Card Draw()
    {
        if (Remaining <= 0) { throw new InvalidDataException("Saved shoe is exhausted."); }
        return State.Cards[State.Position++];
    }
}

