using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NainLegacyModernizer.LegacyMonolith;
using NainLegacyModernizer.ModernApi;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class IntegrationEndpointsTests
{
    [Fact]
    public async Task LegacyMonolith_ReturnsUnmigratedInvoice_InLegacyFormat()
    {
        await using var factory = new WebApplicationFactory<LegacyMonolithProgram>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/invoices/INV-900");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        // Check for PascalCase properties in legacy response
        Assert.Contains("\"InvoiceId\":", content);
        Assert.Contains("\"GeneratedBy\":\"Monolith.IIS.v4.8\"", content);
    }

    [Fact]
    public async Task ModernApi_ReturnsCarvedOrderSlice_InStandardCamelCase()
    {
        await using var factory = new WebApplicationFactory<ModernApiProgram>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders/ORD-777");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        // Check for camelCase properties in modern response
        Assert.Contains("\"id\":", content);
        Assert.Contains("\"customerReference\":", content);
        Assert.Contains("\"source\":\"ModernApi.Net10\"", content);
    }

    [Fact]
    public async Task ModernApi_RejectsNonPositiveOrder_WithBadRequest()
    {
        await using var factory = new WebApplicationFactory<ModernApiProgram>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            orderId = "INVALID-1",
            totalAmount = -10.00m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
