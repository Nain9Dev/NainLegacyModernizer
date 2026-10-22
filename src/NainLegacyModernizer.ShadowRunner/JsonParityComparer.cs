using System.Globalization;
using System.Text.Json;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>Rules applied while comparing a legacy and a modern response body.</summary>
public sealed record ParityComparisonOptions
{
    public static ParityComparisonOptions Default { get; } = new();

    /// <summary>Legacy property name to modern property name.</summary>
    public IReadOnlyDictionary<string, string> FieldAliases { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Property names or dotted paths excluded from the comparison.</summary>
    public IReadOnlyList<string> IgnoredFields { get; init; } = [];

    /// <summary>Absolute difference tolerated between two numeric values.</summary>
    public decimal DecimalTolerance { get; init; }

    public bool IgnoreCase { get; init; } = true;

    /// <summary>Treat a numeric string ("12.50") as equal to the number 12.50. Legacy stacks often stringify decimals.</summary>
    public bool CoerceNumericStrings { get; init; } = true;

    public static ParityComparisonOptions ForRoute(MigrationRoute route, decimal decimalTolerance = 0m)
    {
        ArgumentNullException.ThrowIfNull(route);
        return new ParityComparisonOptions
        {
            FieldAliases = route.FieldAliases,
            IgnoredFields = route.IgnoredFields,
            DecimalTolerance = decimalTolerance,
        };
    }
}

/// <summary>
/// Structural JSON comparison that tolerates the usual legacy-vs-modern noise:
/// property casing, renamed fields, generated metadata and sub-cent rounding.
/// Falls back to a trimmed text comparison when a body is not JSON.
/// </summary>
public static class JsonParityComparer
{
    private const int MaxRenderedValueLength = 200;
    private const string BodyField = "$body";

    public static IReadOnlyList<DiscrepancyDetail> Compare(string? legacyBody, string? modernBody, ParityComparisonOptions? options = null)
    {
        options ??= ParityComparisonOptions.Default;

        var legacyText = legacyBody?.Trim() ?? string.Empty;
        var modernText = modernBody?.Trim() ?? string.Empty;

        using var legacyDoc = TryParse(legacyText);
        using var modernDoc = TryParse(modernText);

        if (legacyDoc is null || modernDoc is null)
        {
            return string.Equals(legacyText, modernText, StringComparison.Ordinal)
                ? []
                : [new DiscrepancyDetail(BodyField, Render(legacyText), Render(modernText))];
        }

        var diffs = new List<DiscrepancyDetail>();
        var ctx = new Context(options, diffs);
        CompareElement(ctx, string.Empty, legacyDoc.RootElement, modernDoc.RootElement);
        return diffs;
    }

    private static JsonDocument? TryParse(string text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CompareElement(Context ctx, string path, JsonElement legacy, JsonElement modern)
    {
        if (legacy.ValueKind == JsonValueKind.Object && modern.ValueKind == JsonValueKind.Object)
        {
            CompareObject(ctx, path, legacy, modern);
            return;
        }

        if (legacy.ValueKind == JsonValueKind.Array && modern.ValueKind == JsonValueKind.Array)
        {
            CompareArray(ctx, path, legacy, modern);
            return;
        }

        if (TryAsNumber(ctx.Options, legacy, out var legacyNumber) && TryAsNumber(ctx.Options, modern, out var modernNumber))
        {
            if (Math.Abs(legacyNumber - modernNumber) > ctx.Options.DecimalTolerance)
            {
                ctx.Add(path, Render(legacy), Render(modern), DiscrepancyKind.ValueMismatch);
            }

            return;
        }

        if (IsBoolean(legacy) && IsBoolean(modern))
        {
            if (legacy.ValueKind != modern.ValueKind)
            {
                ctx.Add(path, Render(legacy), Render(modern), DiscrepancyKind.ValueMismatch);
            }

            return;
        }

        if (legacy.ValueKind != modern.ValueKind)
        {
            ctx.Add(path, Render(legacy), Render(modern), DiscrepancyKind.TypeMismatch);
            return;
        }

        if (legacy.ValueKind == JsonValueKind.String &&
            !string.Equals(legacy.GetString(), modern.GetString(), StringComparison.Ordinal))
        {
            ctx.Add(path, Render(legacy), Render(modern), DiscrepancyKind.ValueMismatch);
        }
    }

    private static void CompareObject(Context ctx, string path, JsonElement legacy, JsonElement modern)
    {
        var comparer = ctx.Options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var modernProps = new Dictionary<string, JsonProperty>(comparer);
        foreach (var prop in modern.EnumerateObject())
        {
            modernProps.TryAdd(prop.Name, prop);
        }

        var matchedModern = new HashSet<string>(comparer);

        foreach (var legacyProp in legacy.EnumerateObject())
        {
            var childPath = Join(path, legacyProp.Name);
            if (ctx.IsIgnored(legacyProp.Name, childPath))
            {
                continue;
            }

            var modernName = ctx.Options.FieldAliases.TryGetValue(legacyProp.Name, out var alias) ? alias : legacyProp.Name;

            if (!modernProps.TryGetValue(modernName, out var modernProp))
            {
                ctx.Add(childPath, Render(legacyProp.Value), null, DiscrepancyKind.MissingInModern);
                continue;
            }

            matchedModern.Add(modernProp.Name);
            CompareElement(ctx, childPath, legacyProp.Value, modernProp.Value);
        }

        foreach (var modernProp in modern.EnumerateObject())
        {
            if (matchedModern.Contains(modernProp.Name))
            {
                continue;
            }

            var childPath = Join(path, modernProp.Name);
            if (ctx.IsIgnored(modernProp.Name, childPath))
            {
                continue;
            }

            ctx.Add(childPath, null, Render(modernProp.Value), DiscrepancyKind.MissingInLegacy);
        }
    }

    private static void CompareArray(Context ctx, string path, JsonElement legacy, JsonElement modern)
    {
        var legacyLength = legacy.GetArrayLength();
        var modernLength = modern.GetArrayLength();
        if (legacyLength != modernLength)
        {
            ctx.Add(
                Join(path, "length"),
                legacyLength.ToString(CultureInfo.InvariantCulture),
                modernLength.ToString(CultureInfo.InvariantCulture),
                DiscrepancyKind.ValueMismatch);
            return;
        }

        var index = 0;
        using var legacyItems = legacy.EnumerateArray();
        using var modernItems = modern.EnumerateArray();
        while (legacyItems.MoveNext() && modernItems.MoveNext())
        {
            CompareElement(ctx, $"{path}[{index}]", legacyItems.Current, modernItems.Current);
            index++;
        }
    }

    private static bool TryAsNumber(ParityComparisonOptions options, JsonElement element, out decimal value)
    {
        value = 0m;
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetDecimal(out value))
                {
                    return true;
                }

                if (element.TryGetDouble(out var dbl) && !double.IsNaN(dbl) && !double.IsInfinity(dbl))
                {
                    value = (decimal)dbl;
                    return true;
                }

                return false;

            case JsonValueKind.String when options.CoerceNumericStrings:
                return decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);

            default:
                return false;
        }
    }

    private static bool IsBoolean(JsonElement element) =>
        element.ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static string Join(string path, string name) =>
        path.Length == 0 ? name : $"{path}.{name}";

    private static string Render(JsonElement element) => Render(element.GetRawText());

    private static string Render(string text) =>
        text.Length <= MaxRenderedValueLength ? text : string.Concat(text.AsSpan(0, MaxRenderedValueLength), "…");

    private sealed class Context(ParityComparisonOptions options, List<DiscrepancyDetail> diffs)
    {
        public ParityComparisonOptions Options { get; } = options;

        public void Add(string path, string? legacy, string? modern, DiscrepancyKind kind) =>
            diffs.Add(new DiscrepancyDetail(path.Length == 0 ? "$" : path, legacy, modern, kind));

        public bool IsIgnored(string name, string path)
        {
            foreach (var ignored in Options.IgnoredFields)
            {
                if (string.Equals(ignored, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ignored, path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
