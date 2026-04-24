#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Solitaire.Core.Rules;

public static class UserSettingsStore
{
    public const string RelativePath = "user://settings.json";
    public const int SettingsFileVersion = 3;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    sealed class Dto
    {
        public int Version { get; set; }
        public StockDrawMode StockDraw { get; set; }
        public ScoringMode Scoring { get; set; }
        public bool VegasCumulative { get; set; }
        public bool TimedGame { get; set; }
        public string? ScreenOrientation { get; set; }
        public string? Language { get; set; }
    }

    public static GameRulesConfig LoadRulesOrDefaults()
    {
        var dto = TryReadDto();
        if (dto is null || dto.Version < 1)
            return GameRulesConfig.ClassicWindows;
        return new GameRulesConfig(dto.StockDraw, dto.Scoring, dto.VegasCumulative, dto.TimedGame);
    }

    public static DisplayServer.ScreenOrientation LoadScreenOrientationOrDefault()
    {
        Dto? dto = TryReadDto();
        if (dto is null || string.IsNullOrEmpty(dto.ScreenOrientation))
            return DisplayServer.ScreenOrientation.Portrait;
        return ParseScreenOrientation(dto.ScreenOrientation);
    }

    public static void Save(GameRulesConfig rules, DisplayServer.ScreenOrientation screenOrientation, string? language = null)
    {
        var dto = new Dto
        {
            Version = SettingsFileVersion,
            StockDraw = rules.StockDraw,
            Scoring = rules.Scoring,
            VegasCumulative = rules.VegasCumulative,
            TimedGame = rules.TimedGame,
            ScreenOrientation = ToStorageString(screenOrientation),
            Language = language,
        };
        string text = JsonSerializer.Serialize(dto, JsonOptions);
        using var f = FileAccess.Open(RelativePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning("UserSettingsStore: could not open user://settings.json for write.");
            return;
        }

        f.StoreString(text);
    }

    public static void WriteTemplateIfMissing(GameRulesConfig rules, DisplayServer.ScreenOrientation? screenOrientation = null)
    {
        if (FileAccess.FileExists(RelativePath))
            return;
        Save(rules, screenOrientation ?? DisplayServer.ScreenOrientation.Portrait, language: null);
    }

    public static string? LoadLanguageOrNull()
    {
        Dto? dto = TryReadDto();
        if (dto is null || dto.Version < 3) return null;
        return dto.Language;
    }

    static Dto? TryReadDto()
    {
        if (!FileAccess.FileExists(RelativePath))
            return null;
        using var f = FileAccess.Open(RelativePath, FileAccess.ModeFlags.Read);
        if (f == null)
            return null;
        try
        {
            return JsonSerializer.Deserialize<Dto>(f.GetAsText(), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string ToStorageString(DisplayServer.ScreenOrientation o) =>
        o switch
        {
            DisplayServer.ScreenOrientation.Landscape => "landscape",
            DisplayServer.ScreenOrientation.Sensor => "sensor",
            _ => "portrait",
        };

    static DisplayServer.ScreenOrientation ParseScreenOrientation(string s) =>
        s switch
        {
            "landscape" => DisplayServer.ScreenOrientation.Landscape,
            "sensor" => DisplayServer.ScreenOrientation.Sensor,
            _ => DisplayServer.ScreenOrientation.Portrait,
        };
}
