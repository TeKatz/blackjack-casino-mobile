using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class SlotCheckpointTests
{
    [Fact]
    public void NeonRestoresEnergyArmedOverrideAndOriginalBetWithEquivalentNextOutcome()
    {
        var checkpoint = new NeonVaultCheckpoint { FreeSpinsRemaining = 2, BonusBet = 100, VaultEnergy = 11, SecurityOverrideArmed = true };
        NeonVaultSession first = NeonVaultSession.Restore(0, checkpoint, RandomStream());
        NeonVaultSession restored = NeonVaultSession.Restore(0, first.Checkpoint, RandomStream());
        NeonVaultSpinResult a = first.Spin(1), b = restored.Spin(50);
        Assert.Equal(a.FinalGrid, b.FinalGrid); Assert.Equal(a.Payout, b.Payout);
        Assert.Equal(a.SecurityOverrideReel, b.SecurityOverrideReel);
        Assert.NotNull(b.SecurityOverrideReel); Assert.Equal(100m, b.Bet); Assert.Equal(0m, b.Stake);
        Assert.Equal(first.Checkpoint, restored.Checkpoint);
    }

    [Fact]
    public void NineLivesRestoresBonusBetEvenWithNoCashAndNeverConsumesItOnCapture()
    {
        var saved = new NineLivesCheckpoint { BonusSpinsRemaining = 3, BonusBet = 25 };
        NineLivesSession session = NineLivesSession.Restore(0, saved, _ => 0);
        Assert.Equal(saved, session.Checkpoint); Assert.Equal(saved, session.Checkpoint);
        NineLivesSpinResult spin = session.Spin(1);
        Assert.True(spin.IsBonusSpin); Assert.Equal(25m, spin.Bet); Assert.Equal(0m, spin.Stake);
        Assert.Equal(2, session.Checkpoint.BonusSpinsRemaining);
        Assert.Equal(3, saved.BonusSpinsRemaining);
    }

    [Fact]
    public void InvalidCheckpointCountsVersionsBetsAndArmedStateAreRejected()
    {
        Assert.Throws<ArgumentException>(() => NeonVaultSession.Restore(0, new() { Version = 2 }));
        Assert.Throws<ArgumentException>(() => NeonVaultSession.Restore(0, new() { FreeSpinsRemaining = -1 }));
        Assert.Throws<ArgumentException>(() => NeonVaultSession.Restore(0, new() { SecurityOverrideArmed = true }));
        Assert.Throws<ArgumentException>(() => NeonVaultSession.Restore(0, new() { VaultEnergy = -1 }));
        Assert.Throws<ArgumentException>(() => NeonVaultSession.Restore(0, new() { FreeSpinsRemaining = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NeonVaultSession.Restore(0, new() { BonusBet = -1 }));
        Assert.Throws<ArgumentException>(() => NineLivesSession.Restore(0, new() { Version = 2 }));
        Assert.Throws<ArgumentException>(() => NineLivesSession.Restore(0, new() { BonusSpinsRemaining = -1 }));
        Assert.Throws<ArgumentException>(() => NineLivesSession.Restore(0, new() { BonusSpinsRemaining = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NineLivesSession.Restore(0, new() { BonusBet = 7 }));
    }
    private static Func<int, int> RandomStream()
    {
        var random = new Random(578);
        return random.Next;
    }
}
