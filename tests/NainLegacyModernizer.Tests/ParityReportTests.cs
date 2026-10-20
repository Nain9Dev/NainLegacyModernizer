using NainLegacyModernizer.Core;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ParityReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static RouteParityResult Matched(string path) => new(path, ParityVerdict.Matched, []);

    private static RouteParityResult Discrepant(string path) =>
        new(path, ParityVerdict.Discrepancy, [new DiscrepancyDetail("total", "1", "2")]);

    private static RouteParityResult Failed(string path) =>
        new(path, ParityVerdict.ExecutionError, [], "timeout");

    private static ParityReport Report(params RouteParityResult[] routes) =>
        new(Now, "http://legacy", "http://modern", routes);

    [Fact]
    public void Counts_SplitRoutesByVerdict()
    {
        var report = Report(Matched("/a"), Matched("/b"), Discrepant("/c"), Failed("/d"));

        Assert.Equal(4, report.Total);
        Assert.Equal(2, report.Matched);
        Assert.Equal(1, report.Discrepant);
        Assert.Equal(1, report.Failed);
    }

    [Fact]
    public void IsReadyToCut_WhenEveryRouteMatched_IsTrue()
    {
        Assert.True(Report(Matched("/a"), Matched("/b")).IsReadyToCut);
    }

    [Theory]
    [InlineData("discrepancy")]
    [InlineData("failure")]
    public void IsReadyToCut_WithAnyNonMatchedRoute_IsFalse(string kind)
    {
        var bad = kind == "discrepancy" ? Discrepant("/x") : Failed("/x");

        Assert.False(Report(Matched("/a"), bad).IsReadyToCut);
    }

    [Fact]
    public void IsReadyToCut_WithNoRoutes_IsFalse()
    {
        Assert.False(Report().IsReadyToCut);
    }
}
