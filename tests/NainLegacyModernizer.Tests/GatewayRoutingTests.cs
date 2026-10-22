using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.Gateway;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class GatewayRoutingTests
{
    [Fact]
    public async Task LegacyRoute_IsServedByMonolith_WithDiagnosticHeaders()
    {
        await using var fixture = new GatewayFixture();

        var response = await fixture.Client.GetAsync("/api/invoices/INV-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Legacy", response.Headers.GetValues(StranglerProxy.ModeHeader).Single());
        Assert.Equal("Legacy", response.Headers.GetValues(StranglerProxy.TargetHeader).Single());
        Assert.Contains("\"GeneratedBy\":\"Monolith.IIS.v4.8\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnmatchedPath_FallsBackToLegacy()
    {
        await using var fixture = new GatewayFixture();

        var response = await fixture.Client.GetAsync("/something/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // legacy answers, with its own 404
        Assert.Equal("Legacy", response.Headers.GetValues(StranglerProxy.TargetHeader).Single());

        var store = fixture.Services.GetRequiredService<IAuditStore>();
        Assert.Equal(MigrationRouteRegistry.FallbackRouteId, store.GetRecentTraffic(1).Single().RouteId);
    }

    [Fact]
    public async Task ModernRoute_IsServedByModernApi()
    {
        await using var fixture = new GatewayFixture();

        var response = await fixture.Client.GetAsync("/api/pricing/quote?sku=SKU-100&qty=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Modern", response.Headers.GetValues(StranglerProxy.TargetHeader).Single());
        Assert.Contains("\"source\":\"ModernApi.Net10\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, "Legacy")]
    [InlineData(100, "Modern")]
    public async Task CanaryRoute_HonoursPercentage(int percent, string expectedTarget)
    {
        await using var fixture = new GatewayFixture(s => s["Modernizer:Routes:3:CanaryPercent"] = percent.ToString(System.Globalization.CultureInfo.InvariantCulture));

        for (var i = 0; i < 5; i++)
        {
            var response = await fixture.Client.GetAsync("/api/catalog/SKU-100");
            Assert.Equal("Canary", response.Headers.GetValues(StranglerProxy.ModeHeader).Single());
            Assert.Equal(expectedTarget, response.Headers.GetValues(StranglerProxy.TargetHeader).Single());
        }
    }

    [Fact]
    public async Task ShadowRoute_ServesLegacy_AndRecordsMatchedAudit()
    {
        await using var fixture = new GatewayFixture();

        var response = await fixture.Client.GetAsync("/api/orders/ORD-1001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Shadow", response.Headers.GetValues(StranglerProxy.ModeHeader).Single());
        Assert.Equal("Legacy", response.Headers.GetValues(StranglerProxy.TargetHeader).Single());
        Assert.Contains("\"OrderID\":\"ORD-1001\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var audit = fixture.Services.GetRequiredService<IAuditStore>().GetAudits("orders").Single();
        Assert.Equal(ParityVerdict.Matched, audit.Verdict);
        Assert.Equal(200, audit.Modern.StatusCode);
        Assert.Contains("\"id\":\"ORD-1001\"", audit.Modern.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShadowRoute_ReplaysPostBody_ToModern()
    {
        await using var fixture = new GatewayFixture();

        var response = await fixture.Client.PostAsJsonAsync("/api/orders", new { orderId = "ORD-9001", totalAmount = 199.90m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var audit = fixture.Services.GetRequiredService<IAuditStore>().GetAudits("orders").Single();
        Assert.Equal(ParityVerdict.Matched, audit.Verdict);
        Assert.Equal(201, audit.Modern.StatusCode);
        Assert.Contains("199.9", audit.Modern.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShadowRoute_DetectsRoundingBug_AsDiscrepancy()
    {
        await using var fixture = new GatewayFixture();

        await fixture.Client.GetAsync("/api/customers/CUST-501");

        var audit = fixture.Services.GetRequiredService<IAuditStore>().GetAudits("customers").Single();
        Assert.Equal(ParityVerdict.Discrepancy, audit.Verdict);
        var tax = Assert.Single(audit.Discrepancies, d => d.Field == "TaxAmount");
        Assert.Equal("262.61", tax.LegacyValue);
        Assert.Equal("262.60", tax.ModernValue);
        Assert.Contains(audit.Discrepancies, d => d.Field == "GrossBalance");
    }

    [Fact]
    public async Task ShadowRoute_WhenModernIsDown_CallerStillGetsLegacy_AndAuditIsExecutionError()
    {
        await using var fixture = new GatewayFixture(modernReachable: false);

        var response = await fixture.Client.GetAsync("/api/orders/ORD-1002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var audit = fixture.Services.GetRequiredService<IAuditStore>().GetAudits("orders").Single();
        Assert.Equal(ParityVerdict.ExecutionError, audit.Verdict);
        Assert.Contains("unreachable", audit.Modern.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangingMode_FromPanel_AppliesToNextRequest()
    {
        await using var fixture = new GatewayFixture();

        var before = await fixture.Client.GetAsync("/api/invoices/INV-1");
        Assert.Equal("Legacy", before.Headers.GetValues(StranglerProxy.TargetHeader).Single());

        var change = await fixture.Client.PutAsJsonAsync("/_modernizer/api/routes/invoices/mode", new { mode = "Modern" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var after = await fixture.Client.GetAsync("/api/invoices/INV-1");
        Assert.Equal("Modern", after.Headers.GetValues(StranglerProxy.TargetHeader).Single());
    }

    [Fact]
    public async Task ChangingMode_UnknownRoute_Is404_AndInvalidCanary_Is400()
    {
        await using var fixture = new GatewayFixture();

        var missing = await fixture.Client.PutAsJsonAsync("/_modernizer/api/routes/nope/mode", new { mode = "Modern" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var invalid = await fixture.Client.PutAsJsonAsync("/_modernizer/api/routes/orders/mode", new { mode = "Canary", canaryPercent = 150 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Overview_ReportsRoutesSummariesAndReadiness()
    {
        await using var fixture = new GatewayFixture();

        for (var i = 0; i < 3; i++)
        {
            await fixture.Client.GetAsync($"/api/orders/ORD-{i}");
        }

        using var doc = JsonDocument.Parse(await fixture.Client.GetStringAsync("/_modernizer/api/overview"));
        var routes = doc.RootElement.GetProperty("routes");
        Assert.Equal(5, routes.GetArrayLength());

        var orders = routes.EnumerateArray().Single(r => r.GetProperty("id").GetString() == "orders");
        Assert.Equal("Shadow", orders.GetProperty("mode").GetString());
        Assert.Equal(3, orders.GetProperty("summary").GetProperty("matched").GetInt32());
        Assert.Equal("Ready", orders.GetProperty("readiness").GetProperty("state").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("totals").GetProperty("auditedSamples").GetInt32());
    }

    [Fact]
    public async Task Panel_IsServedFromEmbeddedResources()
    {
        await using var fixture = new GatewayFixture();

        var html = await fixture.Client.GetAsync("/_modernizer/");
        var css = await fixture.Client.GetAsync("/_modernizer/app.css");
        var noSlash = await fixture.Client.GetAsync("/_modernizer");
        var missing = await fixture.Client.GetAsync("/_modernizer/nope.js");

        Assert.Equal(HttpStatusCode.OK, html.StatusCode);
        Assert.Equal("text/html", html.Content.Headers.ContentType?.MediaType);
        Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, noSlash.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task AuditsEndpoint_FiltersByVerdict_AndClears()
    {
        await using var fixture = new GatewayFixture();

        await fixture.Client.GetAsync("/api/orders/ORD-1");
        await fixture.Client.GetAsync("/api/customers/CUST-501");

        using var discrepancies = JsonDocument.Parse(await fixture.Client.GetStringAsync("/_modernizer/api/audits?verdict=Discrepancy"));
        Assert.Equal(1, discrepancies.RootElement.GetArrayLength());
        Assert.Equal("customers", discrepancies.RootElement[0].GetProperty("routeId").GetString());

        var clear = await fixture.Client.DeleteAsync("/_modernizer/api/audits");
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);

        using var empty = JsonDocument.Parse(await fixture.Client.GetStringAsync("/_modernizer/api/audits"));
        Assert.Equal(0, empty.RootElement.GetArrayLength());
    }
}
