namespace NainLegacyModernizer.Core;

public sealed record RouteParityResult(
    string Path,
    ParityVerdict Verdict,
    IReadOnlyList<DiscrepancyDetail> Discrepancies,
    string? Error = null);

public sealed record ParityReport(
    DateTimeOffset GeneratedAtUtc,
    string LegacyBaseUrl,
    string ModernBaseUrl,
    IReadOnlyList<RouteParityResult> Routes,
    IReadOnlyList<string>? IgnoredFields = null)
{
    public int Total => Routes.Count;

    public int Matched => Routes.Count(r => r.Verdict == ParityVerdict.Matched);

    public int Discrepant => Routes.Count(r => r.Verdict == ParityVerdict.Discrepancy);

    public int Failed => Routes.Count(r => r.Verdict == ParityVerdict.ExecutionError);

    /// <summary>Fail closed: an empty report is never ready, and any non-matching route blocks the cut-over.</summary>
    public bool IsReadyToCut => Total > 0 && Matched == Total;
}
