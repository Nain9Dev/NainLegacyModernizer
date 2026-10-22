using System.Text.Json;
using System.Text.Json.Serialization;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ModernApi;

public static class Program
{
    public static void Main(string[] args) => ModernApiHost.Create(args).Run();
}

/// <summary>The carved-out slices rewritten on .NET 10: camelCase contract, typed domain, fast.</summary>
public static class ModernApiHost
{
    private const string Source = "ModernApi.Net10";

    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        var app = builder.Build();

        app.MapGet("/api/orders/{id}", (string id) =>
        {
            var order = SampleData.OrderFor(id);
            return Results.Ok(new
            {
                id = order.Id,
                customerReference = order.CustomerReference,
                totalAmount = order.TotalAmount.Amount,
                currency = order.TotalAmount.Currency,
                items = order.Items.Select(i => new { sku = i.Sku, quantity = i.Quantity, unitPrice = i.UnitPrice.Amount }),
                createdAtUtc = order.CreatedAtUtc,
                source = Source,
            });
        });

        app.MapPost("/api/orders", (CreateModernOrderRequest request) =>
        {
            if (request.TotalAmount <= 0)
            {
                return Results.BadRequest(new { error = "TotalAmount must be positive." });
            }

            return Results.Created($"/api/orders/{request.OrderId}", new
            {
                id = request.OrderId,
                status = "Created",
                totalAmount = request.TotalAmount,
                currency = "EUR",
                source = Source,
            });
        });

        app.MapGet("/api/customers/{id}", (string id) =>
        {
            var (name, net) = SampleData.CustomerFor(id);

            // Banker's rounding: differs from the monolith on exact half cents. The shadow audit must catch it.
            var tax = Math.Round(net * SampleData.VatRate, 2, MidpointRounding.ToEven);
            return Results.Ok(new
            {
                id,
                name,
                netBalance = net,
                taxAmount = tax,
                grossBalance = net + tax,
                currency = "EUR",
                source = Source,
            });
        });

        app.MapGet("/api/catalog/{sku}", (string sku) =>
        {
            if (!SampleData.Products.TryGetValue(sku, out var product))
            {
                return Results.NotFound(new { message = "Unknown SKU" });
            }

            return Results.Ok(new { sku = sku.ToUpperInvariant(), name = product.Name, price = product.Price, currency = "EUR", source = Source });
        });

        app.MapGet("/api/pricing/quote", (string sku, int qty) =>
            Results.Ok(new { sku, quantity = qty, total = SampleData.Quote(sku, qty), currency = "EUR", source = Source }));

        return app;
    }
}

public sealed record CreateModernOrderRequest(string OrderId, decimal TotalAmount);

public partial class ModernApiProgram;
