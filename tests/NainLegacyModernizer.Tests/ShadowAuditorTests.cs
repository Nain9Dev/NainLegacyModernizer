using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ShadowAuditorTests
{
    private static readonly ShadowAuditRequest Request = new("orders", "GET", "/api/orders/ORD-001");

    [Fact]
    public async Task AuditAsync_WhenOutputsMatch_YieldsParityMatched()
    {
        var auditor = new ShadowAuditor();

        var result = await auditor.AuditAsync(
            Request,
            _ => Task.FromResult(new ProbeResponse(200, """{"OrderID":"ORD-001","Total":"100.00"}""")),
            _ => Task.FromResult(new ProbeResponse(200, """{"id":"ORD-001","total":100.00}""")),
            new ParityComparisonOptions { FieldAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["OrderID"] = "id" } });

        Assert.Equal(ParityVerdict.Matched, result.Verdict);
        Assert.Empty(result.Discrepancies);
        Assert.Equal("orders", result.RouteId);
        Assert.True(result.LegacyLatency >= TimeSpan.Zero);
        Assert.True(result.ModernLatency >= TimeSpan.Zero);
    }

    [Fact]
    public async Task AuditAsync_WhenTotalAmountDiverges_FlagsDiscrepancyWithDetails()
    {
        var auditor = new ShadowAuditor();

        var result = await auditor.AuditAsync(
            Request,
            _ => Task.FromResult(new ProbeResponse(200, """{"total":99.99}""")),
            _ => Task.FromResult(new ProbeResponse(200, """{"total":100.00}""")));

        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        var diff = Assert.Single(result.Discrepancies);
        Assert.Equal("total", diff.Field);
        Assert.Equal("99.99", diff.LegacyValue);
        Assert.Equal("100.00", diff.ModernValue);
    }

    [Fact]
    public async Task AuditAsync_WhenStatusCodesDiffer_ReportsStatusDiscrepancy()
    {
        var auditor = new ShadowAuditor();

        var result = await auditor.AuditAsync(
            Request,
            _ => Task.FromResult(new ProbeResponse(200, """{"a":1}""")),
            _ => Task.FromResult(new ProbeResponse(404, """{"a":1}""")));

        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        Assert.Contains(result.Discrepancies, d => d.Field == "$status" && d.LegacyValue == "200" && d.ModernValue == "404");
    }

    [Fact]
    public async Task AuditAsync_WhenModernThrows_YieldsExecutionError_AndKeepsLegacyObservation()
    {
        var auditor = new ShadowAuditor();
        var legacy = new BackendObservation(200, TimeSpan.FromMilliseconds(12), """{"a":1}""");

        var result = await auditor.AuditAsync(
            Request,
            legacy,
            _ => throw new HttpRequestException("connection refused"));

        Assert.Equal(ParityVerdict.ExecutionError, result.Verdict);
        Assert.False(result.Modern.Succeeded);
        Assert.Contains("connection refused", result.Modern.Error, StringComparison.Ordinal);
        Assert.Equal(200, result.Legacy.StatusCode);
        Assert.Empty(result.Discrepancies);
    }

    [Fact]
    public async Task AuditAsync_WhenCancelled_RecordsTimeoutInsteadOfThrowing()
    {
        var auditor = new ShadowAuditor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await auditor.AuditAsync(
            Request,
            _ => Task.FromResult(new ProbeResponse(200, "{}")),
            async ct =>
            {
                await Task.Delay(1_000, ct);
                return new ProbeResponse(200, "{}");
            },
            cancellationToken: cts.Token);

        Assert.Equal(ParityVerdict.ExecutionError, result.Verdict);
        Assert.Equal("Timed out or cancelled.", result.Modern.Error);
    }
}
