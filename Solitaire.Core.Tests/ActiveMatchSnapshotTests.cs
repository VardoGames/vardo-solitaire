using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;
using Xunit;

namespace Solitaire.Core.Tests;

public class ActiveMatchSnapshotTests
{
    [Fact]
    public void ToActiveMatchSnapshot_TryFrom_RoundTrip_HoldsInvariantsAndScore()
    {
        var rng = new Random(42);
        var s0 = KlondikeState.NewShuffled(GameRulesConfig.ClassicWindows, rng, null);
        Assert.True(s0.AreInvariantsHeld());
        int score0 = s0.Score;
        var snap = s0.ToActiveMatchSnapshot();
        Assert.True(KlondikeState.TryFromActiveMatchSnapshot(snap, out var s1));
        Assert.NotNull(s1);
        Assert.Equal(score0, s1!.Score);
        Assert.True(s1.AreInvariantsHeld());
    }

    [Fact]
    public void Moves_RoundTripsThroughSnapshot()
    {
        var rng = new Random(7);
        var s0 = KlondikeState.NewShuffled(GameRulesConfig.ClassicWindows, rng, null);
        Assert.Equal(0, s0.Moves);
        s0.TryAdvanceStock();
        s0.TryAdvanceStock();
        Assert.Equal(2, s0.Moves);
        var snap = s0.ToActiveMatchSnapshot();
        Assert.Equal(2, snap.Moves);
        Assert.True(KlondikeState.TryFromActiveMatchSnapshot(snap, out var s1));
        Assert.Equal(2, s1!.Moves);
    }

    [Fact]
    public void Clone_PreservesMoves()
    {
        var rng = new Random(11);
        var s0 = KlondikeState.NewShuffled(GameRulesConfig.ClassicWindows, rng, null);
        s0.TryAdvanceStock();
        var s1 = s0.Clone();
        Assert.Equal(s0.Moves, s1.Moves);
    }
}
