using System.Globalization;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ShadowAuditorTests
{
    [Fact]
    public async Task AuditAsync_WhenOutputsMatch_YieldsParityMatched()
    {
        var auditor = new ShadowAuditor();

        var legacyResponse = new LegacyDto("ORD-001", 100.00m, "EUR");
        var modernResponse = new ModernDto("ORD-001", 100.00m, "EUR");

        var result = await auditor.AuditAsync(
            requestPath: "/api/orders/ORD-001",
            legacyCall: () => Task.FromResult(legacyResponse),
            modernCall: () => Task.FromResult(modernResponse),
            comparer: (leg, mod) =>
            {
                var diffs = new List<DiscrepancyDetail>();
                if (leg.OrderId != mod.Id)
                {
                    diffs.Add(new DiscrepancyDetail("Id", leg.OrderId, mod.Id));
                }

                if (leg.Total != mod.TotalAmount)
                {
                    diffs.Add(new DiscrepancyDetail("TotalAmount", leg.Total.ToString(CultureInfo.InvariantCulture), mod.TotalAmount.ToString(CultureInfo.InvariantCulture)));
                }

                return diffs;
            });

        Assert.Equal(ParityVerdict.Matched, result.Verdict);
        Assert.Empty(result.Discrepancies);
        Assert.True(result.LegacyLatency >= TimeSpan.Zero);
        Assert.True(result.ModernLatency >= TimeSpan.Zero);
    }

    [Fact]
    public async Task AuditAsync_WhenTotalAmountDiverges_FlagsDiscrepancyWithDetails()
    {
        var auditor = new ShadowAuditor();

        var legacyResponse = new LegacyDto("ORD-002", 99.99m, "EUR");
        var modernResponse = new ModernDto("ORD-002", 100.00m, "EUR");

        var result = await auditor.AuditAsync(
            requestPath: "/api/orders/ORD-002",
            legacyCall: () => Task.FromResult(legacyResponse),
            modernCall: () => Task.FromResult(modernResponse),
            comparer: (leg, mod) =>
            {
                var diffs = new List<DiscrepancyDetail>();
                if (leg.Total != mod.TotalAmount)
                {
                    diffs.Add(new DiscrepancyDetail("TotalAmount", leg.Total.ToString(CultureInfo.InvariantCulture), mod.TotalAmount.ToString(CultureInfo.InvariantCulture)));
                }

                return diffs;
            });

        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        Assert.Single(result.Discrepancies);
        var diff = result.Discrepancies[0];
        Assert.Equal("TotalAmount", diff.Field);
        Assert.Equal("99.99", diff.LegacyValue);
        Assert.Equal("100.00", diff.ModernValue);
    }

    private sealed record LegacyDto(string OrderId, decimal Total, string Currency);
    private sealed record ModernDto(string Id, decimal TotalAmount, string Currency);
}
