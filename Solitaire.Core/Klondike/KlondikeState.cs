using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Solitaire.Core.Cards;
using Solitaire.Core.Rules;

namespace Solitaire.Core.Klondike;

public sealed class KlondikeState
{
    KlondikeState(
        GameRulesConfig rules,
        List<PlacedCard>[] tableau,
        List<Card>[] foundation,
        List<Card> stock,
        List<Card> waste,
        int score,
        int moves = 0)
    {
        Rules = rules;
        Tableau = tableau;
        Foundation = foundation;
        Stock = stock;
        Waste = waste;
        Score = score;
        Moves = moves;
    }

    public GameRulesConfig Rules { get; }

    public int Score { get; private set; }

    public int Moves { get; private set; }

    public List<PlacedCard>[] Tableau { get; }

    public List<Card>[] Foundation { get; }

    public List<Card> Stock { get; }

    public List<Card> Waste { get; }

    public static KlondikeState NewShuffled(GameRulesConfig rules, Random random, int? vegasCarriedBalance = null)
    {
        List<Card> deck = DeckFactory.CreateOrderedDeck();
        DeckFactory.Shuffle(deck, random);
        return NewFromPreshuffledDeck(rules, deck, vegasCarriedBalance);
    }

    public static KlondikeState NewFromPreshuffledDeck(
        GameRulesConfig rules,
        IReadOnlyList<Card> deck,
        int? vegasCarriedBalance = null)
    {
        if (deck.Count != 52)
            throw new ArgumentException("Expected exactly 52 cards.", nameof(deck));

        int initialScore = KlondikeScoring.InitialScore(rules, vegasCarriedBalance);

        var tableau = new List<PlacedCard>[7];
        int i = 0;
        for (int column = 0; column < 7; column++)
        {
            tableau[column] = new List<PlacedCard>(column + 1);
            for (int row = 0; row < column; row++)
                tableau[column].Add(new PlacedCard(deck[i++], false));
            tableau[column].Add(new PlacedCard(deck[i++], true));
        }

        var stock = new List<Card>(24);
        for (; i < 52; i++)
            stock.Add(deck[i]);

        var foundation = new List<Card>[4];
        for (int pile = 0; pile < 4; pile++)
            foundation[pile] = new List<Card>();

        return new KlondikeState(rules, tableau, foundation, stock, new List<Card>(), initialScore);
    }

    public KlondikeState Clone()
    {
        var tableau = new List<PlacedCard>[7];
        for (int col = 0; col < 7; col++)
        {
            var src = Tableau[col];
            tableau[col] = new List<PlacedCard>(src.Count);
            for (int i = 0; i < src.Count; i++)
                tableau[col].Add(src[i]);
        }

        var foundation = new List<Card>[4];
        for (int p = 0; p < 4; p++)
        {
            foundation[p] = new List<Card>(Foundation[p].Count);
            foundation[p].AddRange(Foundation[p]);
        }

        var stock = new List<Card>(Stock.Count);
        stock.AddRange(Stock);
        var waste = new List<Card>(Waste.Count);
        waste.AddRange(Waste);

        return new KlondikeState(Rules, tableau, foundation, stock, waste, Score, Moves);
    }

    public bool AreInvariantsHeld()
    {
        foreach (List<PlacedCard> col in Tableau)
        {
            if (!KlondikeRules.TableauFaceUpSuffixInvariant(col))
                return false;
            if (!KlondikeRules.TableauOpenRunBuildValid(col))
                return false;
        }

        if (!KlondikeRules.FoundationPilesOrdered(Foundation))
            return false;
        if (!KlondikeRules.AllCardsExactlyStandardDeckOnce(this))
            return false;
        return true;
    }

    public bool TryAdvanceStock()
    {
        if (Stock.Count > 0)
        {
            int k = (int)Rules.StockDraw;
            int take = Math.Min(k, Stock.Count);
            for (int i = 0; i < take; i++)
            {
                int last = Stock.Count - 1;
                Card c = Stock[last];
                Stock.RemoveAt(last);
                Waste.Add(c);
            }

            Moves++;
            return true;
        }

        if (Waste.Count > 0)
        {
            for (int i = Waste.Count - 1; i >= 0; i--)
                Stock.Add(Waste[i]);
            Waste.Clear();
            Score += KlondikeScoring.DeltaRecycleStock(Rules);
            Moves++;
            return true;
        }

        return false;
    }

    public bool TryMoveTableauRun(int fromColumn, int startIndex, int toColumn)
    {
        if (fromColumn == toColumn)
            return false;
        if ((uint)fromColumn >= 7u || (uint)toColumn >= 7u)
            return false;

        List<PlacedCard> src = Tableau[fromColumn];
        List<PlacedCard> dst = Tableau[toColumn];
        if (startIndex < 0 || startIndex >= src.Count)
            return false;
        if (!KlondikeRules.IsValidFaceUpRun(src, startIndex))
            return false;

        PlacedCard bottomMoving = src[startIndex];
        if (dst.Count == 0)
        {
            if (bottomMoving.Card.Rank != Rank.King)
                return false;
        }
        else
        {
            PlacedCard dstTop = dst[^1];
            if (!dstTop.IsFaceUp)
                return false;
            if (!KlondikeRules.CanPlaceTableauOnTableau(bottomMoving.Card, dstTop.Card))
                return false;
        }

        int n = src.Count - startIndex;
        List<PlacedCard> moving = src.GetRange(startIndex, n);
        src.RemoveRange(startIndex, n);
        dst.AddRange(moving);
        if (RevealTableauTopIfNeeded(src))
            Score += KlondikeScoring.DeltaRevealTableau(Rules);
        Moves++;
        return true;
    }

    public bool TryMoveTableauToFoundation(int column)
    {
        if ((uint)column >= 7u)
            return false;
        List<PlacedCard> col = Tableau[column];
        if (col.Count == 0)
            return false;
        PlacedCard top = col[^1];
        if (!top.IsFaceUp)
            return false;

        Card c = top.Card;
        int pile = KlondikeRules.FoundationPileIndex(c);
        List<Card> f = Foundation[pile];
        if (!KlondikeRules.CanAddToFoundation(c, f))
            return false;

        col.RemoveAt(col.Count - 1);
        f.Add(c);
        Score += KlondikeScoring.DeltaCardToFoundation(Rules);
        if (RevealTableauTopIfNeeded(col))
            Score += KlondikeScoring.DeltaRevealTableau(Rules);
        Moves++;
        return true;
    }

    public bool TryMoveWasteToTableau(int column)
    {
        if ((uint)column >= 7u)
            return false;
        if (Waste.Count == 0)
            return false;

        Card c = Waste[^1];
        List<PlacedCard> dst = Tableau[column];
        if (dst.Count == 0)
        {
            if (c.Rank != Rank.King)
                return false;
        }
        else
        {
            PlacedCard dstTop = dst[^1];
            if (!dstTop.IsFaceUp)
                return false;
            if (!KlondikeRules.CanPlaceTableauOnTableau(c, dstTop.Card))
                return false;
        }

        Waste.RemoveAt(Waste.Count - 1);
        dst.Add(new PlacedCard(c, true));
        Score += KlondikeScoring.DeltaWasteToTableau(Rules);
        Moves++;
        return true;
    }

    public bool TryMoveWasteToFoundation()
    {
        if (Waste.Count == 0)
            return false;
        Card c = Waste[^1];
        int pile = KlondikeRules.FoundationPileIndex(c);
        List<Card> f = Foundation[pile];
        if (!KlondikeRules.CanAddToFoundation(c, f))
            return false;

        Waste.RemoveAt(Waste.Count - 1);
        f.Add(c);
        Score += KlondikeScoring.DeltaCardToFoundation(Rules);
        Moves++;
        return true;
    }

    public bool TryMoveFoundationToTableau(int foundationPile, int column)
    {
        if ((uint)foundationPile >= 4u || (uint)column >= 7u)
            return false;

        List<Card> f = Foundation[foundationPile];
        if (f.Count == 0)
            return false;

        Card c = f[^1];
        List<PlacedCard> dst = Tableau[column];
        if (dst.Count == 0)
        {
            if (c.Rank != Rank.King)
                return false;
        }
        else
        {
            PlacedCard dstTop = dst[^1];
            if (!dstTop.IsFaceUp)
                return false;
            if (!KlondikeRules.CanPlaceTableauOnTableau(c, dstTop.Card))
                return false;
        }

        f.RemoveAt(f.Count - 1);
        dst.Add(new PlacedCard(c, true));
        Score += KlondikeScoring.DeltaFoundationToTableau(Rules);
        Moves++;
        return true;
    }

    static bool RevealTableauTopIfNeeded(List<PlacedCard> column)
    {
        if (column.Count == 0)
            return false;
        PlacedCard t = column[^1];
        if (t.IsFaceUp)
            return false;
        column[^1] = t with { IsFaceUp = true };
        return true;
    }

    public bool IsGameWon()
    {
        if (!AreInvariantsHeld()) return false;
        int f = 0;
        for (int p = 0; p < 4; p++) f += Foundation[p].Count;
        return f == 52;
    }

    public ActiveMatchSnapshot ToActiveMatchSnapshot()
    {
        var tab = new IReadOnlyList<PlacedCard>[7];
        for (int c = 0; c < 7; c++)
        {
            var col = Tableau[c];
            var copy = new List<PlacedCard>(col.Count);
            for (int i = 0; i < col.Count; i++) copy.Add(col[i]);
            tab[c] = copy;
        }
        var found = new IReadOnlyList<Card>[4];
        for (int p = 0; p < 4; p++)
        {
            var f = Foundation[p];
            var copy = new List<Card>(f.Count);
            for (int i = 0; i < f.Count; i++) copy.Add(f[i]);
            found[p] = copy;
        }
        var stock = new List<Card>(Stock.Count);
        for (int i = 0; i < Stock.Count; i++) stock.Add(Stock[i]);
        var waste = new List<Card>(Waste.Count);
        for (int i = 0; i < Waste.Count; i++) waste.Add(Waste[i]);
        return new ActiveMatchSnapshot
        {
            Rules = Rules,
            Score = Score,
            Moves = Moves,
            Tableau = tab,
            Foundation = found,
            Stock = stock,
            Waste = waste,
        };
    }

    public static bool TryFromActiveMatchSnapshot(
        [NotNullWhen(true)] ActiveMatchSnapshot? snap,
        [NotNullWhen(true)] out KlondikeState? state)
    {
        state = null;
        if (snap is null) return false;
        if (snap.Moves < 0) return false;
        if (snap.Tableau is null || snap.Tableau.Length != 7) return false;
        if (snap.Foundation is null || snap.Foundation.Length != 4) return false;
        if (snap.Stock is null || snap.Waste is null) return false;
        for (int c = 0; c < 7; c++) if (snap.Tableau[c] is null) return false;
        for (int p = 0; p < 4; p++) if (snap.Foundation[p] is null) return false;
        var tableau = new List<PlacedCard>[7];
        for (int c = 0; c < 7; c++)
        {
            var col = snap.Tableau[c]!;
            tableau[c] = new List<PlacedCard>(col.Count);
            for (int i = 0; i < col.Count; i++) tableau[c].Add(col[i]);
        }
        var foundation = new List<Card>[4];
        for (int p = 0; p < 4; p++)
        {
            var fp = snap.Foundation[p]!;
            foundation[p] = new List<Card>(fp.Count);
            for (int i = 0; i < fp.Count; i++) foundation[p].Add(fp[i]);
        }
        var stock = new List<Card>(snap.Stock.Count);
        for (int i = 0; i < snap.Stock.Count; i++) stock.Add(snap.Stock[i]);
        var waste = new List<Card>(snap.Waste.Count);
        for (int i = 0; i < snap.Waste.Count; i++) waste.Add(snap.Waste[i]);
        var s = new KlondikeState(snap.Rules, tableau, foundation, stock, waste, snap.Score, snap.Moves);
        if (!s.AreInvariantsHeld()) return false;
        state = s;
        return true;
    }
}
