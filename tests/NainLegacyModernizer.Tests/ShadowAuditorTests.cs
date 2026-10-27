using System.Globalization;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ShadowAuditorTests
{
    // The two scenarios below are the synthetic fixtures the static demo mirrors (demo/src/fixtures.ts):
    // ORD-001 agrees at 100.00 EUR, ORD-002 diverges (legacy 99.99 against modern 100.00). The demo's own tests read
    // this file to check that both stay in sync, so keep the ids, paths and amounts literal.
    private static readonly ShadowAuditRequest Request = new("orders", "GET", "/api/orders/ORD-001");
    private static readonly ShadowAuditRequest DivergingRequest = new("orders", "GET", "/api/orders/ORD-002");

    private static readonly ParityComparisonOptions Aliases = new()
    {
        FieldAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrderID"] = "id",
            ["Total"] = "totalAmount",
        },
    };

    [Fact]
    public async Task AuditAsync_WhenOutputsMatch_YieldsParityMatched()
    {
        var auditor = new ShadowAuditor();

        var result = await auditor.AuditAsync(
            Request,
            _ => Task.FromResult(new ProbeResponse(200, LegacyBody("ORD-001", 100.00m))),
            _ => Task.FromResult(new ProbeResponse(200, ModernBody("ORD-001", 100.00m))),
            Aliases);

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
            DivergingRequest,
            _ => Task.FromResult(new ProbeResponse(200, LegacyBody("ORD-002", 99.99m))),
            _ => Task.FromResult(new ProbeResponse(200, ModernBody("ORD-002", 100.00m))),
            Aliases);

        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        var diff = Assert.Single(result.Discrepancies);
        Assert.Equal("Total", diff.Field);
        Assert.Equal("\"99.99\"", diff.LegacyValue);
        Assert.Equal("100.00", diff.ModernValue);
    }

    /// <summary>PascalCase and a stringified decimal, as the monolith answers.</summary>
    private static string LegacyBody(string id, decimal total) =>
        $$"""{"OrderId":"{{id}}","Total":"{{total.ToString("F2", CultureInfo.InvariantCulture)}}","Currency":"EUR"}""";

    /// <summary>camelCase and a numeric decimal, as the modern API answers.</summary>
    private static string ModernBody(string id, decimal total) =>
        $$"""{"id":"{{id}}","totalAmount":{{total.ToString("F2", CultureInfo.InvariantCulture)}},"currency":"EUR"}""";

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
