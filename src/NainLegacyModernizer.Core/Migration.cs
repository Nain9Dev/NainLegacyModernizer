namespace NainLegacyModernizer.Core;

/// <summary>
/// Stage of a route inside the strangler-fig migration.
/// The order of the members is the order in which a route normally advances.
/// </summary>
public enum MigrationMode
{
    /// <summary>All traffic is served by the legacy backend.</summary>
    Legacy = 0,

    /// <summary>Legacy serves the caller; the same request is replayed against the modern backend and compared.</summary>
    Shadow = 1,

    /// <summary>A configured percentage of requests is served by the modern backend.</summary>
    Canary = 2,

    /// <summary>All traffic is served by the modern backend.</summary>
    Modern = 3,
}

/// <summary>Backend that actually produced the response returned to the caller.</summary>
public enum BackendTarget
{
    Legacy,
    Modern,
}

/// <summary>A request the control panel can replay to generate demo traffic for a route.</summary>
public sealed record SampleRequest(string Method, string Path, string? JsonBody = null);

/// <summary>A migratable slice of the legacy surface, identified by a path pattern.</summary>
public sealed record MigrationRoute
{
    public required string Id { get; init; }

    /// <summary>ASP.NET Core route pattern, e.g. <c>/api/orders/{**rest}</c>.</summary>
    public required string PathPattern { get; init; }

    public string Description { get; init; } = string.Empty;

    public MigrationMode Mode { get; init; } = MigrationMode.Legacy;

    /// <summary>Share of requests (0-100) sent to the modern backend while in <see cref="MigrationMode.Canary"/>.</summary>
    public int CanaryPercent { get; init; } = 10;

    public IReadOnlyList<SampleRequest> SampleRequests { get; init; } = [];

    /// <summary>Legacy property name to modern property name, applied before comparing shadow responses.</summary>
    public IReadOnlyDictionary<string, string> FieldAliases { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Properties excluded from parity comparison (timestamps, generated ids, source markers).</summary>
    public IReadOnlyList<string> IgnoredFields { get; init; } = [];

    public MigrationRoute WithMode(MigrationMode mode) => this with { Mode = mode };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A migration route needs an Id.");
        }

        if (string.IsNullOrWhiteSpace(PathPattern) || PathPattern[0] != '/')
        {
            throw new InvalidOperationException($"Route '{Id}': PathPattern must start with '/'.");
        }

        if (CanaryPercent is < 0 or > 100)
        {
            throw new InvalidOperationException($"Route '{Id}': CanaryPercent must be between 0 and 100.");
        }
    }
}

/// <summary>Thresholds that decide when a shadowed route may be promoted.</summary>
public sealed record CutoverPolicy(int MinimumSamples = 50, decimal MinimumParityRatio = 0.995m)
{
    public static CutoverPolicy Default { get; } = new();

    public CutoverReadiness Evaluate(RouteSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var audited = summary.Matched + summary.Discrepancies + summary.ExecutionErrors;
        var ratio = audited == 0 ? 0m : (decimal)summary.Matched / audited;

        if (summary.Mode == MigrationMode.Modern)
        {
            return new CutoverReadiness(ReadinessState.Completed, audited, ratio, "Route already serves modern traffic.");
        }

        if (summary.Mode == MigrationMode.Legacy && audited == 0)
        {
            return new CutoverReadiness(ReadinessState.Collecting, 0, 0m, "Switch to Shadow to start collecting parity samples.");
        }

        if (audited < MinimumSamples)
        {
            return new CutoverReadiness(
                ReadinessState.Collecting,
                audited,
                ratio,
                $"Needs {MinimumSamples - audited} more shadow samples.");
        }

        if (summary.ExecutionErrors > 0)
        {
            return new CutoverReadiness(
                ReadinessState.Blocked,
                audited,
                ratio,
                $"{summary.ExecutionErrors} execution error(s) in the audited window.");
        }

        if (ratio < MinimumParityRatio)
        {
            return new CutoverReadiness(
                ReadinessState.Blocked,
                audited,
                ratio,
                $"Parity {ratio:P2} is below the {MinimumParityRatio:P2} threshold.");
        }

        return new CutoverReadiness(ReadinessState.Ready, audited, ratio, "Parity threshold met. Safe to promote.");
    }
}

public enum ReadinessState
{
    Collecting,
    Blocked,
    Ready,
    Completed,
}

public sealed record CutoverReadiness(ReadinessState State, int AuditedSamples, decimal ParityRatio, string Reason);
