using System;
using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;
using Xunit;

namespace Solitaire.Core.Tests;

public class ResultsBookTests
{
    private static MatchResult MakeResult(int score, int moves, double elapsed, DateTimeOffset? ts = null)
        => new()
        {
            TimestampUtc = ts ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Score = score,
            Moves = moves,
            ElapsedSeconds = elapsed,
        };

    private static GameRulesConfig TimedRules => new(
        StockDrawMode.DrawThree,
        ScoringMode.Standard,
        VegasCumulative: false,
        TimedGame: true);

    private static GameRulesConfig UntimedRules => new(
        StockDrawMode.DrawOne,
        ScoringMode.Standard,
        VegasCumulative: false,
        TimedGame: false);

    [Fact]
    public void FirstRecord_CreatesGroup_AndFillsAllApplicableSlots()
    {
        var book = new ResultsBook();
        var upd = book.Record(TimedRules, MakeResult(200, 80, 120));

        Assert.True(upd.NewScoreRecord);
        Assert.True(upd.NewMovesRecord);
        Assert.True(upd.NewTimeRecord);
        Assert.Equal(200, upd.BestScore);
        Assert.Equal(80, upd.BestMoves);
        Assert.Equal(120, upd.BestElapsedSeconds);

        var group = book.FindGroup(TimedRules);
        Assert.NotNull(group);
        Assert.Equal(200, group!.BestByScore!.Score);
        Assert.Equal(80, group.BestByMoves!.Moves);
        Assert.Equal(120, group.BestByTime!.ElapsedSeconds);
    }

    [Fact]
    public void HigherScore_ReplacesBestByScore_OthersIndependent()
    {
        var book = new ResultsBook();
        book.Record(TimedRules, MakeResult(200, 80, 120));
        var upd = book.Record(TimedRules, MakeResult(300, 100, 200));

        Assert.True(upd.NewScoreRecord);
        Assert.False(upd.NewMovesRecord);
        Assert.False(upd.NewTimeRecord);
        Assert.Equal(300, upd.BestScore);
        Assert.Equal(80, upd.BestMoves);
        Assert.Equal(120, upd.BestElapsedSeconds);
    }

    [Fact]
    public void EqualScore_DoesNotReplace()
    {
        var book = new ResultsBook();
        book.Record(TimedRules, MakeResult(200, 80, 120));
        var upd = book.Record(TimedRules, MakeResult(200, 90, 130));

        Assert.False(upd.NewScoreRecord);
        Assert.False(upd.NewMovesRecord);
        Assert.False(upd.NewTimeRecord);
        Assert.Equal(200, upd.BestScore);
        Assert.Equal(80, upd.BestMoves);
        Assert.Equal(120, upd.BestElapsedSeconds);
    }

    [Fact]
    public void FewerMoves_ReplacesBestByMoves()
    {
        var book = new ResultsBook();
        book.Record(TimedRules, MakeResult(200, 80, 120));
        var upd = book.Record(TimedRules, MakeResult(150, 65, 140));

        Assert.False(upd.NewScoreRecord);
        Assert.True(upd.NewMovesRecord);
        Assert.False(upd.NewTimeRecord);
        Assert.Equal(65, upd.BestMoves);
    }

    [Fact]
    public void FasterTime_ReplacesBestByTime()
    {
        var book = new ResultsBook();
        book.Record(TimedRules, MakeResult(200, 80, 120));
        var upd = book.Record(TimedRules, MakeResult(150, 95, 90));

        Assert.True(upd.NewTimeRecord);
        Assert.Equal(90, upd.BestElapsedSeconds);
    }

    [Fact]
    public void UntimedRules_DoNotWriteBestByTime()
    {
        var book = new ResultsBook();
        var upd = book.Record(UntimedRules, MakeResult(200, 80, 60));

        Assert.True(upd.NewScoreRecord);
        Assert.True(upd.NewMovesRecord);
        Assert.False(upd.NewTimeRecord);
        Assert.Null(upd.BestElapsedSeconds);

        var group = book.FindGroup(UntimedRules);
        Assert.Null(group!.BestByTime);
    }

    [Fact]
    public void DifferentRules_AreIsolated()
    {
        var book = new ResultsBook();
        book.Record(TimedRules, MakeResult(500, 50, 60));
        var upd = book.Record(UntimedRules, MakeResult(100, 100, 300));

        Assert.True(upd.NewScoreRecord);
        Assert.True(upd.NewMovesRecord);
        Assert.Equal(100, upd.BestScore);
        Assert.Equal(100, upd.BestMoves);

        var timed = book.FindGroup(TimedRules);
        Assert.Equal(500, timed!.BestByScore!.Score);
    }

    [Fact]
    public void GetOrCreateGroup_IsIdempotent()
    {
        var book = new ResultsBook();
        var a = book.GetOrCreateGroup(TimedRules);
        var b = book.GetOrCreateGroup(TimedRules);
        Assert.Same(a, b);
        Assert.Single(book.Groups);
    }
}
