using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;
using Xunit;

namespace Solitaire.Core.Tests;

public class KlondikeStateInvariantTests
{
    [Fact]
    public void NewFromPreshuffledDeck_OrderedDeck_ClassicWindows_HoldsInvariants()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        Assert.True(s.AreInvariantsHeld());
    }

    [Fact]
    public void TryMoveTableauRun_SameColumn_DoesNotMutateState()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());
        int scoreBefore = s.Score;
        Assert.True(s.AreInvariantsHeld());

        bool ok = s.TryMoveTableauRun(0, 0, 0);

        Assert.False(ok);
        Assert.Equal(scoreBefore, s.Score);
        Assert.True(s.AreInvariantsHeld());
    }

    [Fact]
    public void TryMoveFoundationToTableau_EmptyFoundation_ReturnsFalse()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        Assert.False(s.TryMoveFoundationToTableau(0, 0));
        Assert.True(s.AreInvariantsHeld());
    }

    [Fact]
    public void TryMoveFoundationToTableau_InvalidTarget_DoesNotMutate()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        Assert.True(s.TryMoveTableauToFoundation(0));
        int score = s.Score;
        int foundationCount = s.Foundation[0].Count;

        Assert.False(s.TryMoveFoundationToTableau(0, 0));

        Assert.Equal(score, s.Score);
        Assert.Equal(foundationCount, s.Foundation[0].Count);
        Assert.True(s.AreInvariantsHeld());
    }

    [Fact]
    public void Clone_IsIndependentSnapshot()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        int stockBefore = s.Stock.Count;
        KlondikeState c = s.Clone();

        Assert.Equal(stockBefore, c.Stock.Count);
        Assert.Equal(s.Score, c.Score);
        Assert.True(c.AreInvariantsHeld());

        Assert.True(s.TryAdvanceStock());
        Assert.NotEqual(stockBefore, s.Stock.Count);
        Assert.Equal(stockBefore, c.Stock.Count);
        Assert.True(c.AreInvariantsHeld());
    }

    [Fact]
    public void TryMoveFoundationToTableau_LegalMove_StandardScoreMinus15()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        Assert.True(s.TryMoveTableauToFoundation(0));
        Assert.Equal(10, s.Score);

        Assert.True(s.TryMoveFoundationToTableau(0, 6));
        Assert.Equal(-5, s.Score);
        Assert.True(s.AreInvariantsHeld());
    }
}
