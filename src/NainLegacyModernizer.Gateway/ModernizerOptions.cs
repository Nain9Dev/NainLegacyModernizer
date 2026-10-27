using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.Gateway;

/// <summary>Configuration section <c>Modernizer</c>. Everything a client changes to point the harness at its own systems.</summary>
public sealed class ModernizerOptions
{
    public const string SectionName = "Modernizer";

    public string LegacyBaseAddress { get; set; } = "http://127.0.0.1:5000";

    public string ModernBaseAddress { get; set; } = "http://127.0.0.1:5001";

    /// <summary>Adds <c>X-Modernizer-Mode</c> and <c>X-Modernizer-Target</c> to every proxied response.</summary>
    public bool EmitDiagnosticHeaders { get; set; } = true;

    /// <summary>When true the shadow replay completes before the request handler returns. Useful for tests and scripted audits.</summary>
    public bool AwaitShadowReplay { get; set; }

    public int ShadowTimeoutSeconds { get; set; } = 10;

    /// <summary>Response bodies above this size are not compared and the audit is recorded as an execution error.</summary>
    public int MaxCapturedBodyBytes { get; set; } = 1_048_576;

    /// <summary>Absolute numeric difference tolerated by the parity comparer (0 = exact).</summary>
    public decimal DecimalTolerance { get; set; }

    public int StoreCapacity { get; set; } = 2_000;

    public CutoverOptions Cutover { get; set; } = new();

    public List<RouteConfig> Routes { get; set; } = [];

    public IReadOnlyList<MigrationRoute> BuildRoutes()
    {
        var routes = new List<MigrationRoute>(Routes.Count);
        foreach (var config in Routes)
        {
            var route = new MigrationRoute
            {
                Id = config.Id ?? string.Empty,
                PathPattern = config.PathPattern ?? string.Empty,
                Description = config.Description ?? string.Empty,
                Mode = config.Mode,
                CanaryPercent = config.CanaryPercent,
                FieldAliases = new Dictionary<string, string>(config.FieldAliases, StringComparer.OrdinalIgnoreCase),
                IgnoredFields = config.IgnoredFields.ToArray(),
                SampleRequests = config.SampleRequests
                    .Select(s => new SampleRequest(s.Method ?? "GET", s.Path ?? "/", s.JsonBody))
                    .ToArray(),
            };
            route.Validate();
            routes.Add(route);
        }

        return routes;
    }

    public CutoverPolicy BuildCutoverPolicy() => new(Cutover.MinimumSamples, Cutover.MinimumParityRatio);

    public sealed class CutoverOptions
    {
        public int MinimumSamples { get; set; } = 50;

        public decimal MinimumParityRatio { get; set; } = 0.995m;
    }

    public sealed class RouteConfig
    {
        public string? Id { get; set; }

        public string? PathPattern { get; set; }

        public string? Description { get; set; }

        public MigrationMode Mode { get; set; } = MigrationMode.Legacy;

        public int CanaryPercent { get; set; } = 10;

        public Dictionary<string, string> FieldAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> IgnoredFields { get; set; } = [];

        public List<SampleRequestConfig> SampleRequests { get; set; } = [];
    }

    public sealed class SampleRequestConfig
    {
        public string? Method { get; set; }

        public string? Path { get; set; }

        public string? JsonBody { get; set; }
    }
}
