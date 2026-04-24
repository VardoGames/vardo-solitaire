using System;

namespace Solitaire.Core.Klondike;

public sealed class MatchResult
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required int Score { get; init; }
    public required int Moves { get; init; }
    public required double ElapsedSeconds { get; init; }
}
