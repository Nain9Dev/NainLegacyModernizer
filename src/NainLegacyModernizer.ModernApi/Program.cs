using System.Text.Json;
using System.Text.Json.Serialization;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ModernApi;

public static class Program
{
    public static void Main(string[] args)
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
            var order = new Order(
                Id: id,
                CustomerReference: "MODERN-CUST-001",
                Items: new[]
                {
                    new OrderItem("SKU-100", 2, Money.Eur(150.00m)),
                    new OrderItem("SKU-200", 1, Money.Eur(150.50m)),
                },
                CreatedAtUtc: DateTimeOffset.UtcNow);

            return Results.Ok(new
            {
                id = order.Id,
                customerReference = order.CustomerReference,
                totalAmount = order.TotalAmount.Amount,
                currency = order.TotalAmount.Currency,
                source = "ModernApi.Net10",
                itemsCount = order.Items.Count,
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
                status = "CreatedInModernPostgres",
                total = request.TotalAmount,
                currency = "EUR",
            });
        });

        app.Run();
    }
}

public sealed record CreateModernOrderRequest(string OrderId, decimal TotalAmount);

public partial class ModernApiProgram;
