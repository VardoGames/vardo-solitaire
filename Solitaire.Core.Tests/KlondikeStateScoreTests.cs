using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;
using Xunit;

namespace Solitaire.Core.Tests;

public class KlondikeStateScoreTests
{
    [Fact]
    public void NewFromPreshuffledDeck_ClassicWindows_StartsAtZeroScore()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        Assert.Equal(0, s.Score);
    }

    [Fact]
    public void NewFromPreshuffledDeck_VegasNonCumulative_StartsAtMinus52()
    {
        var rules = new GameRulesConfig(
            StockDrawMode.DrawThree,
            ScoringMode.Vegas,
            VegasCumulative: false,
            TimedGame: false);

        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(rules, TestDecks.Ordered52());

        Assert.Equal(-52, s.Score);
    }

    [Fact]
    public void NewFromPreshuffledDeck_VegasCumulative_UsesCarriedBalance()
    {
        var rules = new GameRulesConfig(
            StockDrawMode.DrawThree,
            ScoringMode.Vegas,
            VegasCumulative: true,
            TimedGame: false);

        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(rules, TestDecks.Ordered52(), 120);

        Assert.Equal(120, s.Score);
    }

    [Fact]
    public void TryAdvanceStock_StandardDrawThree_RecycleAppliesMinus20()
    {
        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(
            GameRulesConfig.ClassicWindows,
            TestDecks.Ordered52());

        for (int i = 0; i < 8; i++)
            Assert.True(s.TryAdvanceStock());

        Assert.Empty(s.Stock);
        Assert.NotEmpty(s.Waste);

        Assert.True(s.TryAdvanceStock());
        Assert.Equal(-20, s.Score);
        Assert.True(s.AreInvariantsHeld());
    }

    [Fact]
    public void TryAdvanceStock_StandardDrawOne_RecycleAppliesMinus100()
    {
        var rules = new GameRulesConfig(
            StockDrawMode.DrawOne,
            ScoringMode.Standard,
            VegasCumulative: false,
            TimedGame: false);

        KlondikeState s = KlondikeState.NewFromPreshuffledDeck(rules, TestDecks.Ordered52());

        for (int i = 0; i < 24; i++)
            Assert.True(s.TryAdvanceStock());

        Assert.Empty(s.Stock);
        Assert.Equal(24, s.Waste.Count);

        Assert.True(s.TryAdvanceStock());
        Assert.Equal(-100, s.Score);
        Assert.True(s.AreInvariantsHeld());
    }
}
