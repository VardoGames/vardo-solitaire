using Solitaire.Core.Cards;
using Solitaire.Core.Klondike;

namespace Solitaire.Core.Tests;

public static class TestDecks
{
    public static List<Card> Ordered52() => DeckFactory.CreateOrderedDeck();
}
