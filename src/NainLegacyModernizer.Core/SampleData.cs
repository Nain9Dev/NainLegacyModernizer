namespace NainLegacyModernizer.Core;

/// <summary>
/// Deterministic demo data shared by the bundled legacy and modern backends so that the same id yields the same
/// business values on both sides. Replace both backends with real systems when using the harness for a client.
/// </summary>
public static class SampleData
{
    public static readonly IReadOnlyDictionary<string, (string Name, decimal Price)> Products =
        new Dictionary<string, (string, decimal)>(StringComparer.OrdinalIgnoreCase)
        {
            ["SKU-100"] = ("Industrial bearing 6204-2RS", 150.00m),
            ["SKU-200"] = ("Hydraulic seal kit", 150.50m),
            ["SKU-300"] = ("Stainless hex bolt M12 (box)", 42.75m),
            ["SKU-400"] = ("Servo drive 750W", 489.00m),
        };

    public const decimal VatRate = 0.21m;

    public static Order OrderFor(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var seed = StableHash(id);
        var skus = Products.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var lineCount = 1 + (seed % 3);
        var items = new List<OrderItem>(lineCount);
        for (var i = 0; i < lineCount; i++)
        {
            var sku = skus[(seed + i) % skus.Length];
            var quantity = 1 + ((seed / (i + 1)) % 4);
            items.Add(new OrderItem(sku, quantity, Money.Eur(Products[sku].Price)));
        }

        return new Order(id, $"CUST-{500 + (seed % 9)}", items, new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero).AddDays(seed % 120));
    }

    public static (string Name, decimal NetBalance) CustomerFor(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        // CUST-501 lands on a half-cent VAT amount on purpose (1250.50 * 0.21 = 262.605): legacy rounds away from
        // zero, modern rounds to even. The shadow audit must surface that before cutover.
        return id.ToUpperInvariant() switch
        {
            "CUST-501" => ("Talleres Aranda S.L.", 1250.50m),
            "CUST-502" => ("Northwind Logistics", 980.00m),
            _ => ($"Customer {id}", 100m + (StableHash(id) % 900)),
        };
    }

    public static decimal Quote(string sku, int quantity)
    {
        ArgumentNullException.ThrowIfNull(sku);

        var unit = Products.TryGetValue(sku, out var product) ? product.Price : 99.00m;
        var discount = quantity >= 10 ? 0.90m : 1.00m;
        return Math.Round(unit * quantity * discount, 2, MidpointRounding.ToEven);
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in value)
            {
                hash = (hash * 31) + char.ToUpperInvariant(c);
            }

            return Math.Abs(hash);
        }
    }
}
