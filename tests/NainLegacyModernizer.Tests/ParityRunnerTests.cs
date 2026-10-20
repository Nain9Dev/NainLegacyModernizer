using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.LegacyMonolith;
using NainLegacyModernizer.ModernApi;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ParityRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<HttpMethod> Methods { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            return respond(request);
        }
    }

    private static HttpClient Client(StubHandler handler) => new(handler) { BaseAddress = new Uri("http://stub.local") };

    private static Task<HttpResponseMessage> Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

    [Fact]
    public async Task RunAsync_EqualResponses_Matches()
    {
        using var legacy = Client(new StubHandler(_ => Json("""{"id":"A","total":1.0}""")));
        using var modern = Client(new StubHandler(_ => Json("""{"id":"A","total":1.00}""")));

        var result = await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal(ParityVerdict.Matched, result.Verdict);
        Assert.Empty(result.Discrepancies);
    }

    [Fact]
    public async Task RunAsync_DifferentStatusCodes_ReportsStatusPseudoField()
    {
        using var legacy = Client(new StubHandler(_ => Json("{}")));
        using var modern = Client(new StubHandler(_ => Json("{}", HttpStatusCode.NotFound)));

        var result = await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        Assert.Contains(new DiscrepancyDetail("$status", "200", "404"), result.Discrepancies);
    }

    [Fact]
    public async Task RunAsync_SameErrorStatusWithEqualBodies_Matches()
    {
        using var legacy = Client(new StubHandler(_ => Json("""{"error":"nope"}""", HttpStatusCode.NotFound)));
        using var modern = Client(new StubHandler(_ => Json("""{"error":"nope"}""", HttpStatusCode.NotFound)));

        var result = await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal(ParityVerdict.Matched, result.Verdict);
    }

    [Fact]
    public async Task RunAsync_RequestFails_IsExecutionErrorWithReason()
    {
        using var legacy = Client(new StubHandler(_ => throw new HttpRequestException("connection refused")));
        using var modern = Client(new StubHandler(_ => Json("{}")));

        var result = await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal(ParityVerdict.ExecutionError, result.Verdict);
        Assert.Contains("legacy", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connection refused", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_BodyIsNotJson_IsExecutionError()
    {
        using var legacy = Client(new StubHandler(_ => Json("<html>maintenance</html>")));
        using var modern = Client(new StubHandler(_ => Json("{}")));

        var result = await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal(ParityVerdict.ExecutionError, result.Verdict);
        Assert.Contains("JSON", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_OnlyEverSendsGet()
    {
        var legacyHandler = new StubHandler(_ => Json("{}"));
        var modernHandler = new StubHandler(_ => Json("{}"));
        using var legacy = Client(legacyHandler);
        using var modern = Client(modernHandler);

        await new ParityRunner(legacy, modern).RunAsync("/x", CancellationToken.None);

        Assert.Equal([HttpMethod.Get], legacyHandler.Methods);
        Assert.Equal([HttpMethod.Get], modernHandler.Methods);
    }

    [Fact]
    public async Task RunAllAsync_ContinuesAfterAFailedRoute()
    {
        using var legacy = Client(new StubHandler(r => r.RequestUri!.AbsolutePath == "/bad" ? throw new HttpRequestException("boom") : Json("""{"id":1}""")));
        using var modern = Client(new StubHandler(_ => Json("""{"id":1}""")));

        var report = await new ParityRunner(legacy, modern).RunAllAsync(["/a", "/bad", "/b"], "http://l", "http://m", Now, CancellationToken.None);

        Assert.Equal(["/a", "/bad", "/b"], report.Routes.Select(r => r.Path));
        Assert.Equal(2, report.Matched);
        Assert.Equal(1, report.Failed);
        Assert.False(report.IsReadyToCut);
        Assert.Equal(Now, report.GeneratedAtUtc);
    }

    [Fact]
    public async Task RunAsync_AgainstTheInRepoServices_ShowsTheRealDifferences()
    {
        await using var legacyFactory = new WebApplicationFactory<LegacyMonolithProgram>();
        await using var modernFactory = new WebApplicationFactory<ModernApiProgram>();
        using var legacy = legacyFactory.CreateClient();
        using var modern = modernFactory.CreateClient();
        var options = new JsonParityOptions(
            Ignore: new HashSet<string> { "Source" },
            Map: new Dictionary<string, string>
            {
                ["OrderID"] = "id",
                ["CustomerRef"] = "customerReference",
            });

        var result = await new ParityRunner(legacy, modern, options).RunAsync("/api/orders/ORD-1", CancellationToken.None);

        // Same id, same total (450.50), same currency. The services differ in the seeded customer
        // reference and in the item count that only the modern API exposes.
        Assert.Equal(ParityVerdict.Discrepancy, result.Verdict);
        Assert.Equal(
            ["CustomerRef", "itemsCount"],
            result.Discrepancies.Select(d => d.Field).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(result.Discrepancies, d => d.Field.Contains("Total", StringComparison.OrdinalIgnoreCase));
    }
}
