using Solitaire.Core.Rules;

namespace Solitaire.Core.Klondike;

public static class KlondikeScoring
{
    public static int InitialScore(GameRulesConfig rules, int? vegasCarriedBalance)
    {
        return rules.Scoring switch
        {
            ScoringMode.None or ScoringMode.Standard => 0,
            ScoringMode.Vegas => rules.VegasCumulative
                ? vegasCarriedBalance ?? -52
                : -52,
            _ => 0
        };
    }

    public static int DeltaRecycleStock(GameRulesConfig rules)
    {
        if (rules.Scoring != ScoringMode.Standard)
            return 0;
        return rules.StockDraw == StockDrawMode.DrawThree ? -20 : -100;
    }

    public static int DeltaWasteToTableau(GameRulesConfig rules) =>
        rules.Scoring == ScoringMode.Standard ? 5 : 0;

    public static int DeltaCardToFoundation(GameRulesConfig rules) =>
        rules.Scoring switch
        {
            ScoringMode.Standard => 10,
            ScoringMode.Vegas => 5,
            _ => 0
        };

    public static int DeltaFoundationToTableau(GameRulesConfig rules) =>
        rules.Scoring switch
        {
            ScoringMode.Standard => -15,
            ScoringMode.Vegas => -5,
            _ => 0
        };

    public static int DeltaRevealTableau(GameRulesConfig rules) =>
        rules.Scoring == ScoringMode.Standard ? 5 : 0;
}
