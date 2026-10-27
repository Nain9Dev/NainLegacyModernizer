using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.Gateway.ControlPanel;

/// <summary>Wire shapes for the control panel. Latencies are milliseconds, enums are strings.</summary>
public sealed record RouteView(
    string Id,
    string PathPattern,
    string Description,
    MigrationMode Mode,
    int CanaryPercent,
    IReadOnlyList<SampleRequest> SampleRequests,
    RouteSummaryView Summary,
    CutoverReadiness Readiness);

public sealed record RouteSummaryView(
    int TotalRequests,
    int ServedByLegacy,
    int ServedByModern,
    int Matched,
    int Discrepancies,
    int ExecutionErrors,
    int AuditedSamples,
    decimal ParityRatio,
    LatencyStats LegacyLatency,
    LatencyStats ModernLatency,
    DateTimeOffset? LastSeenUtc);

public sealed record OverviewView(
    DateTimeOffset GeneratedAtUtc,
    string LegacyBaseAddress,
    string ModernBaseAddress,
    CutoverPolicy Policy,
    IReadOnlyList<RouteView> Routes,
    OverviewTotals Totals);

public sealed record OverviewTotals(
    int Routes,
    int RoutesModern,
    int RoutesReady,
    int TotalRequests,
    int AuditedSamples,
    int Matched,
    int Discrepancies,
    int ExecutionErrors,
    decimal ParityRatio,
    double LegacyP50Ms,
    double ModernP50Ms);

public sealed record TrafficView(
    string RouteId,
    string Method,
    string Path,
    MigrationMode Mode,
    BackendTarget Target,
    int StatusCode,
    double LatencyMs,
    DateTimeOffset ObservedAtUtc);

public sealed record ObservationView(int? StatusCode, double LatencyMs, string? Body, string? Error);

public sealed record AuditView(
    string Id,
    string RouteId,
    string Method,
    string RequestPath,
    DateTimeOffset ObservedAtUtc,
    ParityVerdict Verdict,
    ObservationView Legacy,
    ObservationView Modern,
    IReadOnlyList<DiscrepancyDetail> Discrepancies);

public sealed record ChangeModeRequest(MigrationMode Mode, int? CanaryPercent);

public static class PanelViews
{
    public static TrafficView ToView(this TrafficSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        return new TrafficView(
            sample.RouteId,
            sample.Method,
            sample.Path,
            sample.Mode,
            sample.Target,
            sample.StatusCode,
            Round(sample.Latency),
            sample.ObservedAtUtc);
    }

    public static AuditView ToView(this ShadowAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        return new AuditView(
            audit.Id,
            audit.RouteId,
            audit.Method,
            audit.RequestPath,
            audit.ObservedAtUtc,
            audit.Verdict,
            ToView(audit.Legacy),
            ToView(audit.Modern),
            audit.Discrepancies);
    }

    public static RouteView ToView(this MigrationRoute route, RouteSummary summary, CutoverPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(policy);

        return new RouteView(
            route.Id,
            route.PathPattern,
            route.Description,
            route.Mode,
            route.CanaryPercent,
            route.SampleRequests,
            new RouteSummaryView(
                summary.TotalRequests,
                summary.ServedByLegacy,
                summary.ServedByModern,
                summary.Matched,
                summary.Discrepancies,
                summary.ExecutionErrors,
                summary.AuditedSamples,
                Math.Round(summary.ParityRatio, 4),
                summary.LegacyLatency,
                summary.ModernLatency,
                summary.LastSeenUtc),
            policy.Evaluate(summary));
    }

    public static OverviewTotals Totals(IReadOnlyList<RouteView> routes, IReadOnlyList<RouteSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(summaries);

        var audited = summaries.Sum(s => s.AuditedSamples);
        var matched = summaries.Sum(s => s.Matched);

        return new OverviewTotals(
            routes.Count,
            routes.Count(r => r.Mode == MigrationMode.Modern),
            routes.Count(r => r.Readiness.State == ReadinessState.Ready),
            summaries.Sum(s => s.TotalRequests),
            audited,
            matched,
            summaries.Sum(s => s.Discrepancies),
            summaries.Sum(s => s.ExecutionErrors),
            audited == 0 ? 0m : Math.Round((decimal)matched / audited, 4),
            WeightedP50(summaries.Select(s => s.LegacyLatency)),
            WeightedP50(summaries.Select(s => s.ModernLatency)));
    }

    private static double WeightedP50(IEnumerable<LatencyStats> stats)
    {
        double weighted = 0;
        var count = 0;
        foreach (var s in stats)
        {
            weighted += s.P50Ms * s.Count;
            count += s.Count;
        }

        return count == 0 ? 0 : Math.Round(weighted / count, 2);
    }

    private static ObservationView ToView(BackendObservation observation) =>
        new(observation.StatusCode, Round(observation.Latency), observation.Body, observation.Error);

    private static double Round(TimeSpan span) => Math.Round(span.TotalMilliseconds, 2);
}
