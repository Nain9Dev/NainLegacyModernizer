using System.Globalization;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.LegacyMonolith;

public static class Program
{
    public static void Main(string[] args) => LegacyMonolithHost.Create(args).Run();
}

/// <summary>
/// Stand-in for the system being strangled. Emulates the contract quirks of an ASP.NET 4.x monolith:
/// PascalCase JSON, stringified decimals, ad-hoc field names and slow responses.
/// </summary>
public static class LegacyMonolithHost
{
    private const string Generator = "Monolith.IIS.v4.8";

    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);

        var app = builder.Build();

        app.MapGet("/api/invoices/{id}", async (string id) =>
        {
            await SimulateLegacyLatency();
            return Results.Ok(new
            {
                InvoiceId = id,
                Status = "LegacyProcessed",
                AmountDue = 1250.00m,
                Currency = "EUR",
                GeneratedBy = Generator,
            });
        });

        app.MapGet("/api/orders/{id}", async (string id) =>
        {
            await SimulateLegacyLatency();
            var order = SampleData.OrderFor(id);
            return Results.Ok(new
            {
                OrderID = order.Id,
                CustomerRef = order.CustomerReference,
                TotalAmount = order.TotalAmount.Amount.ToString("F2", CultureInfo.InvariantCulture),
                Currency = order.TotalAmount.Currency,
                Lines = order.Items.Select(i => new
                {
                    SKU = i.Sku,
                    Qty = i.Quantity,
                    UnitPrice = i.UnitPrice.Amount,
                }),
                Source = "LegacyMonolith",
                GeneratedBy = Generator,
            });
        });

        app.MapPost("/api/orders", async (CreateOrderRequest request) =>
        {
            await SimulateLegacyLatency();
            return Results.Created($"/api/orders/{request.OrderId}", new
            {
                OrderID = request.OrderId,
                Status = "Created",
                TotalAmount = request.TotalAmount,
                Currency = "EUR",
                Source = "LegacyMonolith",
            });
        });

        app.MapGet("/api/customers/{id}", async (string id) =>
        {
            await SimulateLegacyLatency();
            var (name, net) = SampleData.CustomerFor(id);
            var tax = Math.Round(net * SampleData.VatRate, 2, MidpointRounding.AwayFromZero);
            return Results.Ok(new
            {
                CustomerID = id,
                Name = name,
                NetBalance = net,
                TaxAmount = tax,
                GrossBalance = net + tax,
                Currency = "EUR",
                Source = "LegacyMonolith",
            });
        });

        app.MapGet("/api/catalog/{sku}", async (string sku) =>
        {
            await SimulateLegacyLatency();
            if (!SampleData.Products.TryGetValue(sku, out var product))
            {
                return Results.NotFound(new { Message = "Unknown SKU" });
            }

            return Results.Ok(new { SKU = sku.ToUpperInvariant(), Name = product.Name, Price = product.Price, Currency = "EUR", Source = "LegacyMonolith" });
        });

        app.MapGet("/api/pricing/quote", async (string sku, int qty) =>
        {
            await SimulateLegacyLatency();
            return Results.Ok(new { SKU = sku, Quantity = qty, Total = SampleData.Quote(sku, qty), Currency = "EUR", Source = "LegacyMonolith" });
        });

        return app;
    }

    private static Task SimulateLegacyLatency() => Task.Delay(Random.Shared.Next(18, 45));
}

public sealed record CreateOrderRequest(string OrderId, decimal TotalAmount);

public partial class LegacyMonolithProgram;
