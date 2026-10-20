using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ParityHtmlRendererTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static ParityReport Sample() => new(
        Now,
        "http://legacy.local",
        "http://modern.local",
        [
            new RouteParityResult("/api/orders/1", ParityVerdict.Matched, []),
            new RouteParityResult("/api/orders/2", ParityVerdict.Discrepancy, [new DiscrepancyDetail("total", "99.99", "100.00")]),
            new RouteParityResult("/api/orders/3", ParityVerdict.ExecutionError, [], "timeout"),
        ]);

    [Fact]
    public void Render_ProducesAStandaloneDocument()
    {
        var html = ParityHtmlRenderer.Render(Sample());

        Assert.StartsWith("<!doctype html>", html, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", html, StringComparison.Ordinal);
        Assert.DoesNotContain("url(", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_LeadsWithTheVerdict()
    {
        var notReady = ParityHtmlRenderer.Render(Sample());
        var ready = ParityHtmlRenderer.Render(new ParityReport(Now, "a", "b", [new RouteParityResult("/x", ParityVerdict.Matched, [])]));

        Assert.Contains("Not ready to cut over", notReady, StringComparison.Ordinal);
        Assert.Contains("Ready to cut over", ready, StringComparison.Ordinal);
        Assert.True(notReady.IndexOf("Not ready to cut over", StringComparison.Ordinal) < notReady.IndexOf("/api/orders/1", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_ShowsCountsPathsAndDifferingFields()
    {
        var html = ParityHtmlRenderer.Render(Sample());

        Assert.Contains("/api/orders/1", html, StringComparison.Ordinal);
        Assert.Contains("/api/orders/2", html, StringComparison.Ordinal);
        Assert.Contains("99.99", html, StringComparison.Ordinal);
        Assert.Contains("100.00", html, StringComparison.Ordinal);
        Assert.Contains("timeout", html, StringComparison.Ordinal);
        Assert.Contains("1 matched", html, StringComparison.Ordinal);
        Assert.Contains("1 different", html, StringComparison.Ordinal);
        Assert.Contains("1 failed", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_BarSegmentsAreProportionalToCounts()
    {
        var report = new ParityReport(
            Now,
            "a",
            "b",
            [
                new RouteParityResult("/1", ParityVerdict.Matched, []),
                new RouteParityResult("/2", ParityVerdict.Matched, []),
                new RouteParityResult("/3", ParityVerdict.Matched, []),
                new RouteParityResult("/4", ParityVerdict.Discrepancy, [new DiscrepancyDetail("f", "1", "2")]),
            ]);

        var html = ParityHtmlRenderer.Render(report);

        Assert.Contains("width=\"75\"", html, StringComparison.Ordinal);
        Assert.Contains("width=\"25\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EscapesEveryDynamicValue()
    {
        const string payload = "<script>alert(1)</script>";
        var report = new ParityReport(
            Now,
            payload,
            payload,
            [
                new RouteParityResult(payload, ParityVerdict.Discrepancy, [new DiscrepancyDetail(payload, payload, "\"quoted\" & more")]),
                new RouteParityResult("/x", ParityVerdict.ExecutionError, [], payload),
            ]);

        var html = ParityHtmlRenderer.Render(report);

        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&amp; more", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_MarksStateWithTextNotOnlyColour()
    {
        var html = ParityHtmlRenderer.Render(Sample());

        Assert.Contains("Matched", html, StringComparison.Ordinal);
        Assert.Contains("Different", html, StringComparison.Ordinal);
        Assert.Contains("Failed", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ListsIgnoredFieldsSoTheReportIsNotMisleading()
    {
        var report = new ParityReport(Now, "a", "b", [new RouteParityResult("/x", ParityVerdict.Matched, [])], ["generatedAt", "<b>"]);

        var html = ParityHtmlRenderer.Render(report);

        Assert.Contains("Ignored fields", html, StringComparison.Ordinal);
        Assert.Contains("<code>generatedAt</code>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_IsDeterministic()
    {
        Assert.Equal(ParityHtmlRenderer.Render(Sample()), ParityHtmlRenderer.Render(Sample()));
    }
}
