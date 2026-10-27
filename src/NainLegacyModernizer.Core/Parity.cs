namespace NainLegacyModernizer.Core;

public enum ParityVerdict
{
    Matched,
    Discrepancy,
    ExecutionError,
}

public enum DiscrepancyKind
{
    ValueMismatch,
    TypeMismatch,
    MissingInModern,
    MissingInLegacy,
}

public sealed record DiscrepancyDetail(
    string Field,
    string? LegacyValue,
    string? ModernValue,
    DiscrepancyKind Kind = DiscrepancyKind.ValueMismatch);

/// <summary>One side of a shadow comparison, as observed on the wire.</summary>
public sealed record BackendObservation(
    int? StatusCode,
    TimeSpan Latency,
    string? Body,
    string? Error = null)
{
    public bool Succeeded => Error is null && StatusCode is not null;
}

/// <summary>Outcome of replaying one request against both backends.</summary>
public sealed record ShadowAuditResult(
    string Id,
    string RouteId,
    string Method,
    string RequestPath,
    DateTimeOffset ObservedAtUtc,
    ParityVerdict Verdict,
    BackendObservation Legacy,
    BackendObservation Modern,
    IReadOnlyList<DiscrepancyDetail> Discrepancies)
{
    public TimeSpan LegacyLatency => Legacy.Latency;

    public TimeSpan ModernLatency => Modern.Latency;
}

/// <summary>One request served by the gateway, whatever the mode.</summary>
public sealed record TrafficSample(
    string RouteId,
    string Method,
    string Path,
    MigrationMode Mode,
    BackendTarget Target,
    int StatusCode,
    TimeSpan Latency,
    DateTimeOffset ObservedAtUtc,
    string? AuditId = null);

public sealed record LatencyStats(int Count, double P50Ms, double P95Ms, double MeanMs)
{
    public static LatencyStats Empty { get; } = new(0, 0, 0, 0);

    public static LatencyStats From(IReadOnlyList<double> sortedMs)
    {
        ArgumentNullException.ThrowIfNull(sortedMs);
        if (sortedMs.Count == 0)
        {
            return Empty;
        }

        return new LatencyStats(
            sortedMs.Count,
            Percentile(sortedMs, 0.50),
            Percentile(sortedMs, 0.95),
            Math.Round(sortedMs.Average(), 2));
    }

    private static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        var rank = (int)Math.Ceiling(p * sorted.Count) - 1;
        rank = Math.Clamp(rank, 0, sorted.Count - 1);
        return Math.Round(sorted[rank], 2);
    }
}

/// <summary>Aggregated view of a route, computed by the audit store.</summary>
public sealed record RouteSummary(
    string RouteId,
    MigrationMode Mode,
    int TotalRequests,
    int ServedByLegacy,
    int ServedByModern,
    int Matched,
    int Discrepancies,
    int ExecutionErrors,
    LatencyStats LegacyLatency,
    LatencyStats ModernLatency,
    DateTimeOffset? LastSeenUtc)
{
    public int AuditedSamples => Matched + Discrepancies + ExecutionErrors;

    public decimal ParityRatio => AuditedSamples == 0 ? 0m : (decimal)Matched / AuditedSamples;

    public static RouteSummary Empty(string routeId, MigrationMode mode) =>
        new(routeId, mode, 0, 0, 0, 0, 0, 0, LatencyStats.Empty, LatencyStats.Empty, null);
}
