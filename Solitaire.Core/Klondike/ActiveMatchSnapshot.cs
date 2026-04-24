using System.Collections.Generic;
using Solitaire.Core.Cards;
using Solitaire.Core.Rules;

namespace Solitaire.Core.Klondike;

public sealed class ActiveMatchSnapshot
{
    public required GameRulesConfig Rules { get; init; }
    public required int Score { get; init; }
    public int Moves { get; init; }
    public required IReadOnlyList<PlacedCard>[] Tableau { get; init; }
    public required IReadOnlyList<Card>[] Foundation { get; init; }
    public required IReadOnlyList<Card> Stock { get; init; }
    public required IReadOnlyList<Card> Waste { get; init; }
}
