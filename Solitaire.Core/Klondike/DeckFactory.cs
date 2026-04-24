using Solitaire.Core.Cards;

namespace Solitaire.Core.Klondike;

public static class DeckFactory
{
    public static List<Card> CreateOrderedDeck()
    {
        var deck = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
                deck.Add(new Card(suit, rank));
        }

        return deck;
    }

    public static void Shuffle(List<Card> deck, Random random)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }
}
