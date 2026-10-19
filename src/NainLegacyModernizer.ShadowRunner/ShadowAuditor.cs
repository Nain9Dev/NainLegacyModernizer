using System.Diagnostics;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

public interface IShadowAuditor
{
    Task<ShadowAuditResult> AuditAsync<TLegacy, TModern>(
        string requestPath,
        Func<Task<TLegacy>> legacyCall,
        Func<Task<TModern>> modernCall,
        Func<TLegacy, TModern, IReadOnlyList<DiscrepancyDetail>> comparer);
}

public sealed class ShadowAuditor : IShadowAuditor
{
    public async Task<ShadowAuditResult> AuditAsync<TLegacy, TModern>(
        string requestPath,
        Func<Task<TLegacy>> legacyCall,
        Func<Task<TModern>> modernCall,
        Func<TLegacy, TModern, IReadOnlyList<DiscrepancyDetail>> comparer)
    {
        var legacySw = Stopwatch.StartNew();
        TLegacy legacyResult;
        try
        {
            legacyResult = await legacyCall();
        }
        finally
        {
            legacySw.Stop();
        }

        var modernSw = Stopwatch.StartNew();
        TModern modernResult;
        try
        {
            modernResult = await modernCall();
        }
        finally
        {
            modernSw.Stop();
        }

        var discrepancies = comparer(legacyResult, modernResult);
        var verdict = discrepancies.Count == 0 ? ParityVerdict.Matched : ParityVerdict.Discrepancy;

        return new ShadowAuditResult(
            RequestPath: requestPath,
            Verdict: verdict,
            LegacyLatency: legacySw.Elapsed,
            ModernLatency: modernSw.Elapsed,
            Discrepancies: discrepancies);
    }
}
