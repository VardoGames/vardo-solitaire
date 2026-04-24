namespace Solitaire.Core.Rules;

public readonly record struct GameRulesConfig(
    StockDrawMode StockDraw,
    ScoringMode Scoring,
    bool VegasCumulative,
    bool TimedGame)
{
    public static GameRulesConfig ClassicWindows { get; } = new(
        StockDrawMode.DrawThree,
        ScoringMode.Standard,
        VegasCumulative: false,
        TimedGame: false);
}
