#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Solitaire.Core.Cards;
using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;

public static class ActiveMatchStore
{
    public const string RelativePath = "user://active_match.json";
    public const int FileVersion = 1;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    sealed class FileDto
    {
        public int Version { get; set; }
        public double ElapsedSeconds { get; set; }
        public int Score { get; set; }
        public int Moves { get; set; }
        public StockDrawMode StockDraw { get; set; }
        public ScoringMode Scoring { get; set; }
        public bool VegasCumulative { get; set; }
        public bool TimedGame { get; set; }
        public PlacedCardDto[][]? Tableau { get; set; }
        public CardDto[][]? Foundation { get; set; }
        public CardDto[]? Stock { get; set; }
        public CardDto[]? Waste { get; set; }
    }

    sealed class PlacedCardDto
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public bool IsFaceUp { get; set; }
    }

    sealed class CardDto
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
    }

    public static void Save(KlondikeState game, double elapsedSeconds)
    {
        if (game.IsGameWon())
        {
            Delete();
            return;
        }
        var snap = game.ToActiveMatchSnapshot();
        var tab = new PlacedCardDto[7][];
        for (int c = 0; c < 7; c++)
        {
            var col = snap.Tableau[c];
            tab[c] = new PlacedCardDto[col.Count];
            for (int i = 0; i < col.Count; i++)
            {
                var p = col[i];
                tab[c][i] = new PlacedCardDto
                {
                    Suit = p.Card.Suit, Rank = p.Card.Rank, IsFaceUp = p.IsFaceUp,
                };
            }
        }
        var fnd = new CardDto[4][];
        for (int p = 0; p < 4; p++)
        {
            var fp = snap.Foundation[p];
            fnd[p] = new CardDto[fp.Count];
            for (int i = 0; i < fp.Count; i++)
            {
                Card c0 = fp[i];
                fnd[p][i] = new CardDto { Suit = c0.Suit, Rank = c0.Rank };
            }
        }
        var dto = new FileDto
        {
            Version = FileVersion,
            ElapsedSeconds = elapsedSeconds,
            Score = snap.Score,
            Moves = snap.Moves,
            StockDraw = snap.Rules.StockDraw,
            Scoring = snap.Rules.Scoring,
            VegasCumulative = snap.Rules.VegasCumulative,
            TimedGame = snap.Rules.TimedGame,
            Tableau = tab,
            Foundation = fnd,
            Stock = new CardDto[snap.Stock.Count],
            Waste = new CardDto[snap.Waste.Count],
        };
        for (int i = 0; i < snap.Stock.Count; i++)
        {
            Card c0 = snap.Stock[i];
            dto.Stock![i] = new CardDto { Suit = c0.Suit, Rank = c0.Rank };
        }
        for (int i = 0; i < snap.Waste.Count; i++)
        {
            Card c0 = snap.Waste[i];
            dto.Waste![i] = new CardDto { Suit = c0.Suit, Rank = c0.Rank };
        }
        string text = JsonSerializer.Serialize(dto, JsonOptions);
        using var f = FileAccess.Open(RelativePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning("ActiveMatchStore: could not open user://active_match.json for write.");
            return;
        }
        f.StoreString(text);
    }

    public static void Delete()
    {
        if (!FileAccess.FileExists(RelativePath)) return;
        using DirAccess? dir = DirAccess.Open("user://");
        if (dir == null) return;
        if (dir.FileExists("active_match.json"))
        {
            Error e = dir.Remove("active_match.json");
            if (e != Error.Ok) GD.PrintErr($"ActiveMatchStore: Remove failed: {e}");
        }
    }

    public static bool TryLoad(out KlondikeState? state, out double elapsedSeconds, out GameRulesConfig rules)
    {
        state = null;
        elapsedSeconds = 0d;
        rules = GameRulesConfig.ClassicWindows;
        if (!FileAccess.FileExists(RelativePath)) return false;
        using var f = FileAccess.Open(RelativePath, FileAccess.ModeFlags.Read);
        if (f is null) return false;
        FileDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FileDto>(f.GetAsText(), JsonOptions);
        }
        catch (JsonException)
        {
            Delete();
            return false;
        }
        if (dto is null || dto.Version < 1 || dto.Tableau is null || dto.Foundation is null
            || dto.Stock is null || dto.Waste is null) return false;
        if (dto.Tableau.Length != 7 || dto.Foundation.Length != 4) return false;
        rules = new GameRulesConfig(
            dto.StockDraw, dto.Scoring, dto.VegasCumulative, dto.TimedGame);
        IReadOnlyList<PlacedCard>[] tab = new IReadOnlyList<PlacedCard>[7];
        for (int c = 0; c < 7; c++)
        {
            if (dto.Tableau[c] is null) return false;
            var arr = new PlacedCard[dto.Tableau[c].Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var p = dto.Tableau[c][i];
                var card = new Card(p.Suit, p.Rank);
                arr[i] = new PlacedCard(card, p.IsFaceUp);
            }
            tab[c] = arr;
        }
        IReadOnlyList<Card>[] found = new IReadOnlyList<Card>[4];
        for (int p = 0; p < 4; p++)
        {
            if (dto.Foundation[p] is null) return false;
            var arr = new Card[dto.Foundation[p].Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var c1 = dto.Foundation[p][i];
                arr[i] = new Card(c1.Suit, c1.Rank);
            }
            found[p] = arr;
        }
        var stockL = new List<Card>(dto.Stock.Length);
        for (int i = 0; i < dto.Stock.Length; i++)
        {
            var c1 = dto.Stock[i];
            stockL.Add(new Card(c1.Suit, c1.Rank));
        }
        var wasteL = new List<Card>(dto.Waste.Length);
        for (int i = 0; i < dto.Waste.Length; i++)
        {
            var c1 = dto.Waste[i];
            wasteL.Add(new Card(c1.Suit, c1.Rank));
        }
        var snap = new ActiveMatchSnapshot
        {
            Rules = rules,
            Score = dto.Score,
            Moves = dto.Moves < 0 ? 0 : dto.Moves,
            Tableau = tab,
            Foundation = found,
            Stock = stockL,
            Waste = wasteL,
        };
        if (!KlondikeState.TryFromActiveMatchSnapshot(snap, out var st))
        {
            Delete();
            return false;
        }
        if (st!.IsGameWon()) { Delete(); return false; }
        state = st;
        elapsedSeconds = dto.ElapsedSeconds;
        if (double.IsNaN(elapsedSeconds) || elapsedSeconds < 0) elapsedSeconds = 0d;
        return true;
    }
}
