using System.Collections.Generic;
using Solitaire.Core.Rules;

namespace Solitaire.Core.Klondike;

public sealed class ResultsGroup
{
    public required GameRulesConfig Rules { get; init; }
    public MatchResult? BestByScore { get; set; }
    public MatchResult? BestByMoves { get; set; }
    public MatchResult? BestByTime { get; set; }
}

public readonly record struct ResultsUpdate(
    int? BestScore,
    bool NewScoreRecord,
    int? BestMoves,
    bool NewMovesRecord,
    double? BestElapsedSeconds,
    bool NewTimeRecord);

public sealed class ResultsBook
{
    private readonly List<ResultsGroup> _groups = new();

    public IReadOnlyList<ResultsGroup> Groups => _groups;

    public ResultsGroup? FindGroup(GameRulesConfig rules)
    {
        for (int i = 0; i < _groups.Count; i++)
        {
            if (_groups[i].Rules.Equals(rules))
                return _groups[i];
        }
        return null;
    }

    public ResultsGroup GetOrCreateGroup(GameRulesConfig rules)
    {
        var existing = FindGroup(rules);
        if (existing is not null)
            return existing;
        var created = new ResultsGroup { Rules = rules };
        _groups.Add(created);
        return created;
    }

    public void AddGroup(ResultsGroup group)
    {
        _groups.Add(group);
    }

    public ResultsUpdate Record(GameRulesConfig rules, MatchResult result)
    {
        var group = GetOrCreateGroup(rules);

        bool newScore = group.BestByScore is null || result.Score > group.BestByScore.Score;
        if (newScore)
            group.BestByScore = result;

        bool newMoves = group.BestByMoves is null || result.Moves < group.BestByMoves.Moves;
        if (newMoves)
            group.BestByMoves = result;

        bool newTime = false;
        if (rules.TimedGame)
        {
            newTime = group.BestByTime is null || result.ElapsedSeconds < group.BestByTime.ElapsedSeconds;
            if (newTime)
                group.BestByTime = result;
        }

        return new ResultsUpdate(
            group.BestByScore?.Score,
            newScore,
            group.BestByMoves?.Moves,
            newMoves,
            group.BestByTime?.ElapsedSeconds,
            newTime);
    }
}
