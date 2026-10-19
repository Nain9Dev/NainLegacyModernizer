using System.Text.Json;

namespace NainLegacyModernizer.LegacyMonolith;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Simulate legacy ASP.NET PascalCase contract
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null; // PascalCase
        });

        var app = builder.Build();

        app.MapGet("/api/invoices/{id}", (string id) =>
        {
            return Results.Ok(new
            {
                InvoiceId = id,
                Status = "LegacyProcessed",
                AmountDue = 1250.00m,
                Currency = "EUR",
                GeneratedBy = "Monolith.IIS.v4.8",
            });
        });

        app.MapGet("/api/orders/{id}", (string id) =>
        {
            // Legacy implementation of orders (to be strangled)
            return Results.Ok(new
            {
                OrderID = id,
                CustomerRef = "LEGACY-CUST-001",
                TotalAmount = 450.50m,
                Currency = "EUR",
                Source = "LegacyMonolith",
            });
        });

        app.MapPost("/api/orders", (LegacyOrderRequest request) =>
        {
            return Results.Created($"/api/orders/{request.OrderID}", new
            {
                OrderID = request.OrderID,
                Status = "CreatedInLegacyDB",
                Total = request.Amount,
            });
        });

        app.Run();
    }
}

public sealed record LegacyOrderRequest(string OrderID, decimal Amount);

public partial class LegacyMonolithProgram;
