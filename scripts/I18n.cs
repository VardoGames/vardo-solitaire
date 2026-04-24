#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public static class I18n
{
    public const string LocaleEn = "en";
    public const string LocaleRu = "ru";
    public const string LocaleZh = "zh";

    static readonly object Lock = new();
    static Dictionary<string, string> _english = new();
    static Dictionary<string, string> _active = new();
    static string _code = LocaleEn;
    static bool _loaded;

    public static string LanguageCode => _code;

    public static void Init()
    {
        lock (Lock)
        {
            if (_loaded) return;
            using var f = FileAccess.Open("res://locale/ui.json", FileAccess.ModeFlags.Read);
            if (f is null)
            {
                _loaded = true;
                return;
            }
            using var doc = JsonDocument.Parse(f.GetAsText());
            var root = doc.RootElement;
            _english = ReadLocale(root, LocaleEn);
            string? stored = UserSettingsStore.LoadLanguageOrNull();
            _code = stored is LocaleEn or LocaleRu or LocaleZh
                ? stored
                : MapOsToLanguageCode();
            _active = ReadLocale(root, _code);
            if (_active.Count == 0) _active = new Dictionary<string, string>(_english);
            ApplyToTranslationServer();
            _loaded = true;
        }
    }

    public static void SetLanguageCode(string? storedOrNull)
    {
        lock (Lock) { if (!_loaded) Init(); }
        if (!_loaded) return;
        _code = storedOrNull is LocaleEn or LocaleRu or LocaleZh
            ? storedOrNull
            : MapOsToLanguageCode();
        using var f = FileAccess.Open("res://locale/ui.json", FileAccess.ModeFlags.Read);
        if (f is null) return;
        using var doc = JsonDocument.Parse(f.GetAsText());
        _active = ReadLocale(doc.RootElement, _code);
        if (_active.Count == 0) _active = new Dictionary<string, string>(_english);
        ApplyToTranslationServer();
    }

    static void ApplyToTranslationServer() => TranslationServer.SetLocale(_code);

    static string MapOsToLanguageCode()
    {
        var sys = OS.GetLocale();
        if (string.IsNullOrEmpty(sys))
            return LocaleEn;
        if (sys.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return LocaleRu;
        if (sys.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return LocaleZh;
        return LocaleEn;
    }

    static Dictionary<string, string> ReadLocale(JsonElement root, string locale)
    {
        if (!root.TryGetProperty(locale, out var node) || node.ValueKind != JsonValueKind.Object)
        {
            if (locale != LocaleEn && root.TryGetProperty(LocaleEn, out var en) && en.ValueKind == JsonValueKind.Object)
                return ToFlat(en);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        return ToFlat(node);
    }

    static Dictionary<string, string> ToFlat(JsonElement objectNode)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in objectNode.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.String)
                d[p.Name] = p.Value.GetString() ?? string.Empty;
        }
        return d;
    }

    public static string T(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        if (_active != null && _active.TryGetValue(id, out var t) && t.Length > 0) return t;
        if (_english != null && _english.TryGetValue(id, out t) && t.Length > 0) return t;
        return id;
    }

    public static string Tf(string id, params object?[] args) =>
        string.Format(T(id), args);
}
