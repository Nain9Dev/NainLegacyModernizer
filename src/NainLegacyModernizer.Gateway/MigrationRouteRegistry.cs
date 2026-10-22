using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.Gateway;

/// <summary>Runtime view of the migration routes. Mode changes made from the panel land here and apply to the next request.</summary>
public sealed class MigrationRouteRegistry
{
    /// <summary>Synthetic route id used for traffic that matched no configured pattern.</summary>
    public const string FallbackRouteId = "legacy-fallback";

    private readonly Lock _gate = new();
    private readonly List<string> _configuredOrder;
    private MigrationRoute[] _ordered;

    public MigrationRouteRegistry(IEnumerable<MigrationRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var list = routes.ToList();
        foreach (var route in list)
        {
            route.Validate();
        }

        var duplicate = list.GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate migration route id '{duplicate.Key}'.");
        }

        _configuredOrder = list.Select(r => r.Id).ToList();
        _ordered = Order(list);
    }

    /// <summary>Routes in configuration order, for display.</summary>
    public IReadOnlyList<MigrationRoute> Routes
    {
        get
        {
            var snapshot = _ordered;
            return _configuredOrder
                .Select(id => snapshot.First(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
    }

    public MigrationRoute? Get(string id) =>
        _ordered.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Most specific route whose pattern matches the request path, or null for the catch-all fallback.</summary>
    public MigrationRoute? Match(PathString path)
    {
        var value = path.HasValue ? path.Value! : "/";
        foreach (var route in _ordered)
        {
            if (PathPatternMatcher.IsMatch(route.PathPattern, value))
            {
                return route;
            }
        }

        return null;
    }

    public MigrationRoute SetMode(string id, MigrationMode mode, int? canaryPercent = null)
    {
        lock (_gate)
        {
            var index = Array.FindIndex(_ordered, r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new KeyNotFoundException($"Unknown migration route '{id}'.");
            }

            var updated = _ordered[index].WithMode(mode);
            if (canaryPercent is { } percent)
            {
                updated = updated with { CanaryPercent = percent };
            }

            updated.Validate();

            var copy = (MigrationRoute[])_ordered.Clone();
            copy[index] = updated;
            _ordered = copy;
            return updated;
        }
    }

    private static MigrationRoute[] Order(List<MigrationRoute> routes) =>
        routes
            .Select((route, index) => (route, index, score: PathPatternMatcher.Specificity(route.PathPattern)))
            .OrderByDescending(x => x.score.literals)
            .ThenByDescending(x => x.score.segments)
            .ThenBy(x => x.index)
            .Select(x => x.route)
            .ToArray();
}

/// <summary>
/// Minimal path template matcher: literal segments, <c>{name}</c> single-segment parameters and a trailing
/// <c>{**name}</c> catch-all. Enough for strangler-fig carve-outs, and free of routing internals.
/// </summary>
public static class PathPatternMatcher
{
    public static bool IsMatch(string pattern, string path)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(path);

        var patternSegments = Split(pattern);
        var pathSegments = Split(path);

        var i = 0;
        for (; i < patternSegments.Length; i++)
        {
            var segment = patternSegments[i];
            if (segment.StartsWith("{**", StringComparison.Ordinal))
            {
                return true;
            }

            if (i >= pathSegments.Length)
            {
                return false;
            }

            if (segment.Length > 1 && segment[0] == '{' && segment[^1] == '}')
            {
                continue;
            }

            if (!string.Equals(segment, pathSegments[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return i == pathSegments.Length;
    }

    public static (int literals, int segments) Specificity(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var segments = Split(pattern);
        var literals = segments.Count(s => s.Length == 0 || s[0] != '{');
        return (literals, segments.Length);
    }

    private static string[] Split(string value) =>
        value.Split('/', StringSplitOptions.RemoveEmptyEntries);
}
