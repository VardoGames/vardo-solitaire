using Solitaire.Core.Cards;

namespace Solitaire.Core.Klondike;

public static class KlondikeRules
{
    public static bool IsRedSuit(Suit suit) =>
        suit is Suit.Diamonds or Suit.Hearts;

    public static bool OppositeColor(Card a, Card b) =>
        IsRedSuit(a.Suit) != IsRedSuit(b.Suit);

    public static bool CanPlaceTableauOnTableau(Card moving, Card destinationTop) =>
        OppositeColor(moving, destinationTop) && moving.Rank == destinationTop.Rank - 1;

    public static bool IsValidFaceUpRun(IReadOnlyList<PlacedCard> column, int startIndex)
    {
        if (startIndex < 0 || startIndex >= column.Count || !column[startIndex].IsFaceUp)
            return false;
        for (int j = startIndex + 1; j < column.Count; j++)
        {
            if (!column[j].IsFaceUp)
                return false;
            if (!CanPlaceTableauOnTableau(column[j].Card, column[j - 1].Card))
                return false;
        }

        return true;
    }

    public static bool CanAddToFoundation(Card card, IReadOnlyList<Card> pile)
    {
        if (pile.Count == 0)
            return card.Rank == Rank.Ace;
        Card top = pile[^1];
        return card.Suit == top.Suit && card.Rank == top.Rank + 1;
    }

    public static int FoundationPileIndex(Card card) => (int)card.Suit;

    public static bool TableauFaceUpSuffixInvariant(IReadOnlyList<PlacedCard> column)
    {
        bool seenFaceUp = false;
        for (int i = 0; i < column.Count; i++)
        {
            if (column[i].IsFaceUp)
                seenFaceUp = true;
            else if (seenFaceUp)
                return false;
        }

        return true;
    }

    public static bool TableauOpenRunBuildValid(IReadOnlyList<PlacedCard> column)
    {
        int k = 0;
        while (k < column.Count && !column[k].IsFaceUp)
            k++;
        for (int j = k + 1; j < column.Count; j++)
        {
            if (!CanPlaceTableauOnTableau(column[j].Card, column[j - 1].Card))
                return false;
        }

        return true;
    }

    public static bool FoundationPilesOrdered(IReadOnlyList<Card>[] foundation)
    {
        if (foundation.Length != 4)
            return false;
        for (int p = 0; p < 4; p++)
        {
            IReadOnlyList<Card> pile = foundation[p];
            var expectedSuit = (Suit)p;
            for (int i = 0; i < pile.Count; i++)
            {
                if (pile[i].Suit != expectedSuit)
                    return false;
                if (pile[i].Rank != (Rank)(i + 1))
                    return false;
            }
        }

        return true;
    }

    public static bool AllCardsExactlyStandardDeckOnce(KlondikeState state)
    {
        var seen = new HashSet<(Suit Suit, Rank Rank)>();
        foreach (List<PlacedCard> col in state.Tableau)
        {
            foreach (PlacedCard pc in col)
            {
                if (!seen.Add((pc.Card.Suit, pc.Card.Rank)))
                    return false;
            }
        }

        foreach (List<Card> pile in state.Foundation)
        {
            foreach (Card c in pile)
            {
                if (!seen.Add((c.Suit, c.Rank)))
                    return false;
            }
        }

        foreach (Card c in state.Stock)
        {
            if (!seen.Add((c.Suit, c.Rank)))
                return false;
        }

        foreach (Card c in state.Waste)
        {
            if (!seen.Add((c.Suit, c.Rank)))
                return false;
        }

        return seen.Count == 52;
    }
}
