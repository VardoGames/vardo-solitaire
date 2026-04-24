using Solitaire.Core.Cards;

public static class CardPaths
{
    const string BasePath = "res://sprites/cards/";

    public static string FacePng(Card card)
    {
        string folder = card.Suit switch
        {
            Suit.Clubs => "Clover",
            Suit.Diamonds => "Diamond",
            Suit.Hearts => "Heart",
            Suit.Spades => "Spade",
            _ => "Heart"
        };
        string suffix = card.Suit switch
        {
            Suit.Clubs => "clover",
            Suit.Diamonds => "diamond",
            Suit.Hearts => "heart",
            Suit.Spades => "spade",
            _ => "heart"
        };
        return $"{BasePath}{folder}/card_{(int)card.Rank}_{suffix}.png";
    }
}
