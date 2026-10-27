using System.Diagnostics;
using System.Globalization;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>Raw answer of one backend, before latency is attached.</summary>
public sealed record ProbeResponse(int StatusCode, string? Body);

/// <summary>Calls one backend. Exceptions are captured by the auditor and become <see cref="ParityVerdict.ExecutionError"/>.</summary>
public delegate Task<ProbeResponse> BackendProbe(CancellationToken cancellationToken);

/// <summary>Identifies the request being audited.</summary>
public sealed record ShadowAuditRequest(string RouteId, string Method, string Path);

public interface IShadowAuditor
{
    /// <summary>Runs both probes in parallel and compares the outcome.</summary>
    Task<ShadowAuditResult> AuditAsync(
        ShadowAuditRequest request,
        BackendProbe legacyProbe,
        BackendProbe modernProbe,
        ParityComparisonOptions? comparison = null,
        CancellationToken cancellationToken = default);

    /// <summary>Compares an already observed legacy answer against a fresh modern probe.</summary>
    Task<ShadowAuditResult> AuditAsync(
        ShadowAuditRequest request,
        BackendObservation legacy,
        BackendProbe modernProbe,
        ParityComparisonOptions? comparison = null,
        CancellationToken cancellationToken = default);
}

public sealed class ShadowAuditor(TimeProvider? timeProvider = null) : IShadowAuditor
{
    private const string StatusField = "$status";

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    public async Task<ShadowAuditResult> AuditAsync(
        ShadowAuditRequest request,
        BackendProbe legacyProbe,
        BackendProbe modernProbe,
        ParityComparisonOptions? comparison = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(legacyProbe);
        ArgumentNullException.ThrowIfNull(modernProbe);

        var legacyTask = ObserveAsync(legacyProbe, cancellationToken);
        var modernTask = ObserveAsync(modernProbe, cancellationToken);
        await Task.WhenAll(legacyTask, modernTask).ConfigureAwait(false);

        return Build(request, await legacyTask.ConfigureAwait(false), await modernTask.ConfigureAwait(false), comparison);
    }

    public async Task<ShadowAuditResult> AuditAsync(
        ShadowAuditRequest request,
        BackendObservation legacy,
        BackendProbe modernProbe,
        ParityComparisonOptions? comparison = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(modernProbe);

        var modern = await ObserveAsync(modernProbe, cancellationToken).ConfigureAwait(false);
        return Build(request, legacy, modern, comparison);
    }

    public static async Task<BackendObservation> ObserveAsync(BackendProbe probe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(probe);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await probe(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return new BackendObservation(response.StatusCode, stopwatch.Elapsed, response.Body);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            stopwatch.Stop();
            var message = ex is OperationCanceledException ? "Timed out or cancelled." : $"{ex.GetType().Name}: {ex.Message}";
            return new BackendObservation(null, stopwatch.Elapsed, null, message);
        }
    }

    private ShadowAuditResult Build(
        ShadowAuditRequest request,
        BackendObservation legacy,
        BackendObservation modern,
        ParityComparisonOptions? comparison)
    {
        ParityVerdict verdict;
        IReadOnlyList<DiscrepancyDetail> discrepancies;

        if (!legacy.Succeeded || !modern.Succeeded)
        {
            verdict = ParityVerdict.ExecutionError;
            discrepancies = [];
        }
        else
        {
            var diffs = new List<DiscrepancyDetail>();
            if (legacy.StatusCode != modern.StatusCode)
            {
                diffs.Add(new DiscrepancyDetail(
                    StatusField,
                    legacy.StatusCode?.ToString(CultureInfo.InvariantCulture),
                    modern.StatusCode?.ToString(CultureInfo.InvariantCulture)));
            }

            diffs.AddRange(ShadowParityComparer.Compare(legacy.Body, modern.Body, comparison));
            discrepancies = diffs;
            verdict = diffs.Count == 0 ? ParityVerdict.Matched : ParityVerdict.Discrepancy;
        }

        return new ShadowAuditResult(
            Id: Guid.CreateVersion7().ToString("N"),
            RouteId: request.RouteId,
            Method: request.Method,
            RequestPath: request.Path,
            ObservedAtUtc: _time.GetUtcNow(),
            Verdict: verdict,
            Legacy: legacy,
            Modern: modern,
            Discrepancies: discrepancies);
    }
}
