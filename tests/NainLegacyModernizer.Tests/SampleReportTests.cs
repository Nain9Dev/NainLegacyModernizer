using Microsoft.AspNetCore.Mvc.Testing;
using NainLegacyModernizer.LegacyMonolith;
using NainLegacyModernizer.ModernApi;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

/// <summary>
/// REQ-PAR-16: the sample reports published with the demo are produced by running the parity runner
/// against the in-repo services. Set UPDATE_SAMPLES=1 to regenerate them.
/// </summary>
public sealed class SampleReportTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);

    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["OrderID"] = "id",
        ["CustomerRef"] = "customerReference",
    };

    [Fact]
    public async Task NotReadySample_MatchesTheCommittedFile()
    {
        var html = await RenderAsync(new JsonParityOptions(Ignore: new HashSet<string> { "Source" }, Map: Aliases));

        Assert.Contains("Not ready to cut over", html, StringComparison.Ordinal);
        AssertMatchesFile("parity-report-not-ready.html", html);
    }

    [Fact]
    public async Task ReadySample_MatchesTheCommittedFile()
    {
        var options = new JsonParityOptions(
            Ignore: new HashSet<string> { "CustomerRef", "ItemsCount", "Source" },
            Map: Aliases);

        var html = await RenderAsync(options);

        Assert.Contains("Ready to cut over", html, StringComparison.Ordinal);
        AssertMatchesFile("parity-report-ready.html", html);
    }

    private static async Task<string> RenderAsync(JsonParityOptions options)
    {
        await using var legacyFactory = new WebApplicationFactory<LegacyMonolithProgram>();
        await using var modernFactory = new WebApplicationFactory<ModernApiProgram>();
        using var legacy = legacyFactory.CreateClient();
        using var modern = modernFactory.CreateClient();

        var report = await new ParityRunner(legacy, modern, options).RunAllAsync(
            ["/api/orders/ORD-1", "/api/orders/ORD-2"],
            "LegacyMonolith (in-repo)",
            "ModernApi (in-repo)",
            FixedTime,
            CancellationToken.None);

        return ParityHtmlRenderer.Render(report);
    }

    private static void AssertMatchesFile(string name, string html)
    {
        var path = Path.Combine(RepositoryRoot(), "demo", "samples", name);
        if (Environment.GetEnvironmentVariable("UPDATE_SAMPLES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, html);
        }

        Assert.True(File.Exists(path), $"Missing {path}. Run the tests once with UPDATE_SAMPLES=1.");
        Assert.Equal(Normalise(html), Normalise(File.ReadAllText(path)));
    }

    private static string Normalise(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NainLegacyModernizer.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
