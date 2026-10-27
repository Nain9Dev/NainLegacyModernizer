using System.Globalization;
using System.Text.Json;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>Field names to skip and legacy-to-modern name aliases. Names are matched after normalisation.</summary>
public sealed record JsonParityOptions(
    IReadOnlySet<string>? Ignore = null,
    IReadOnlyDictionary<string, string>? Map = null);

public static class JsonParityComparer
{
    public const string Missing = "<missing>";

    /// <exception cref="JsonException">Either document is not valid JSON.</exception>
    public static IReadOnlyList<DiscrepancyDetail> Compare(string legacyJson, string modernJson, JsonParityOptions? options = null)
    {
        options ??= new JsonParityOptions();
        var ignore = new HashSet<string>((options.Ignore ?? new HashSet<string>()).Select(Normalise), StringComparer.Ordinal);
        var map = (options.Map ?? new Dictionary<string, string>())
            .ToDictionary(p => Normalise(p.Key), p => Normalise(p.Value), StringComparer.Ordinal);

        // Ignoring a legacy name also ignores the modern name it is mapped to.
        foreach (var (legacyName, modernName) in map)
        {
            if (ignore.Contains(legacyName))
            {
                ignore.Add(modernName);
            }
        }

        var legacy = Flatten(legacyJson, ignore, map, applyMap: true);
        var modern = Flatten(modernJson, ignore, map, applyMap: false);

        var diffs = new List<DiscrepancyDetail>();
        foreach (var (key, legacyField) in legacy)
        {
            if (!modern.TryGetValue(key, out var modernField))
            {
                diffs.Add(new DiscrepancyDetail(legacyField.Display, legacyField.Value, Missing));
            }
            else if (!SameValue(legacyField, modernField))
            {
                diffs.Add(new DiscrepancyDetail(legacyField.Display, legacyField.Value, modernField.Value));
            }
        }

        foreach (var (key, modernField) in modern)
        {
            if (!legacy.ContainsKey(key))
            {
                diffs.Add(new DiscrepancyDetail(modernField.Display, Missing, modernField.Value));
            }
        }

        return diffs;
    }

    private static string Normalise(string name) =>
        name.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

    private readonly record struct Field(string Display, string Value, JsonValueKind Kind);

    private static Dictionary<string, Field> Flatten(string json, HashSet<string> ignore, Dictionary<string, string> map, bool applyMap)
    {
        using var document = JsonDocument.Parse(json);
        var fields = new Dictionary<string, Field>(StringComparer.Ordinal);
        Walk(document.RootElement, string.Empty, string.Empty, fields, ignore, map, applyMap);
        return fields;
    }

    private static void Walk(
        JsonElement element,
        string display,
        string key,
        Dictionary<string, Field> fields,
        HashSet<string> ignore,
        Dictionary<string, string> map,
        bool applyMap)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var normalised = Normalise(property.Name);
                    if (ignore.Contains(normalised))
                    {
                        continue;
                    }

                    var segment = applyMap && map.TryGetValue(normalised, out var alias) ? alias : normalised;
                    Walk(
                        property.Value,
                        display.Length == 0 ? property.Name : $"{display}.{property.Name}",
                        key.Length == 0 ? segment : $"{key}.{segment}",
                        fields,
                        ignore,
                        map,
                        applyMap);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Walk(item, $"{display}[{index}]", $"{key}[{index}]", fields, ignore, map, applyMap);
                    index++;
                }

                break;
            default:
                var name = display.Length == 0 ? "$" : display;
                fields[key.Length == 0 ? "$" : key] = new Field(name, Render(element), element.ValueKind);
                break;
        }
    }

    private static string Render(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Null => "null",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => element.GetRawText(),
    };

    private static bool SameValue(Field legacy, Field modern)
    {
        if (legacy.Kind == JsonValueKind.Number && modern.Kind == JsonValueKind.Number)
        {
            var parsedLegacy = decimal.TryParse(legacy.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var l);
            var parsedModern = decimal.TryParse(modern.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var m);
            return parsedLegacy && parsedModern ? l == m : string.Equals(legacy.Value, modern.Value, StringComparison.Ordinal);
        }

        var legacyIsBool = legacy.Kind is JsonValueKind.True or JsonValueKind.False;
        var modernIsBool = modern.Kind is JsonValueKind.True or JsonValueKind.False;
        if (legacy.Kind != modern.Kind && !(legacyIsBool && modernIsBool))
        {
            return false;
        }

        return string.Equals(legacy.Value, modern.Value, StringComparison.Ordinal);
    }
}
