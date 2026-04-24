#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;

public static class ResultsStore
{
    public const string RelativePath = "user://results.json";
    public const string TmpRelativePath = "user://results.json.tmp";
    public const string FileName = "results.json";
    public const string TmpFileName = "results.json.tmp";
    public const int FileVersion = 1;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    sealed class FileDto
    {
        public int Version { get; set; }
        public GroupDto[]? Groups { get; set; }
    }

    sealed class GroupDto
    {
        public RulesDto? Rules { get; set; }
        public MatchResultDto? BestByScore { get; set; }
        public MatchResultDto? BestByMoves { get; set; }
        public MatchResultDto? BestByTime { get; set; }
    }

    sealed class RulesDto
    {
        public StockDrawMode StockDraw { get; set; }
        public ScoringMode Scoring { get; set; }
        public bool VegasCumulative { get; set; }
        public bool TimedGame { get; set; }
    }

    sealed class MatchResultDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public int Score { get; set; }
        public int Moves { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    public static ResultsBook Load()
    {
        var book = new ResultsBook();
        if (!FileAccess.FileExists(RelativePath)) return book;
        using var f = FileAccess.Open(RelativePath, FileAccess.ModeFlags.Read);
        if (f is null)
        {
            GD.PushWarning("ResultsStore: could not open user://results.json for read.");
            return book;
        }
        FileDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FileDto>(f.GetAsText(), JsonOptions);
        }
        catch (JsonException ex)
        {
            GD.PushWarning($"ResultsStore: corrupted results.json ignored ({ex.Message}).");
            return book;
        }
        if (dto is null || dto.Version != FileVersion || dto.Groups is null) return book;
        foreach (var g in dto.Groups)
        {
            if (g?.Rules is null) continue;
            var rules = new GameRulesConfig(
                g.Rules.StockDraw, g.Rules.Scoring, g.Rules.VegasCumulative, g.Rules.TimedGame);
            var group = new ResultsGroup { Rules = rules };
            group.BestByScore = FromDto(g.BestByScore);
            group.BestByMoves = FromDto(g.BestByMoves);
            group.BestByTime = rules.TimedGame ? FromDto(g.BestByTime) : null;
            book.AddGroup(group);
        }
        return book;
    }

    public static void Save(ResultsBook book)
    {
        var dto = new FileDto
        {
            Version = FileVersion,
            Groups = new GroupDto[book.Groups.Count],
        };
        for (int i = 0; i < book.Groups.Count; i++)
        {
            var g = book.Groups[i];
            dto.Groups[i] = new GroupDto
            {
                Rules = new RulesDto
                {
                    StockDraw = g.Rules.StockDraw,
                    Scoring = g.Rules.Scoring,
                    VegasCumulative = g.Rules.VegasCumulative,
                    TimedGame = g.Rules.TimedGame,
                },
                BestByScore = ToDto(g.BestByScore),
                BestByMoves = ToDto(g.BestByMoves),
                BestByTime = ToDto(g.BestByTime),
            };
        }
        string text = JsonSerializer.Serialize(dto, JsonOptions);

        using (var f = FileAccess.Open(TmpRelativePath, FileAccess.ModeFlags.Write))
        {
            if (f is null)
            {
                GD.PushWarning("ResultsStore: could not open tmp file for write.");
                return;
            }
            f.StoreString(text);
        }

        using var dir = DirAccess.Open("user://");
        if (dir is null)
        {
            GD.PushWarning("ResultsStore: could not open user:// dir.");
            return;
        }
        if (dir.FileExists(FileName))
        {
            Error e = dir.Remove(FileName);
            if (e != Error.Ok)
            {
                GD.PrintErr($"ResultsStore: Remove old file failed: {e}");
                return;
            }
        }
        Error e2 = dir.Rename(TmpFileName, FileName);
        if (e2 != Error.Ok)
            GD.PrintErr($"ResultsStore: Rename tmp -> final failed: {e2}");
    }

    public static ResultsUpdate Record(GameRulesConfig rules, MatchResult result)
    {
        var book = Load();
        var upd = book.Record(rules, result);
        Save(book);
        return upd;
    }

    static MatchResultDto? ToDto(MatchResult? r)
    {
        if (r is null) return null;
        return new MatchResultDto
        {
            Timestamp = r.TimestampUtc,
            Score = r.Score,
            Moves = r.Moves,
            ElapsedSeconds = r.ElapsedSeconds,
        };
    }

    static MatchResult? FromDto(MatchResultDto? d)
    {
        if (d is null) return null;
        return new MatchResult
        {
            TimestampUtc = d.Timestamp,
            Score = d.Score,
            Moves = d.Moves,
            ElapsedSeconds = double.IsNaN(d.ElapsedSeconds) || d.ElapsedSeconds < 0 ? 0 : d.ElapsedSeconds,
        };
    }
}
