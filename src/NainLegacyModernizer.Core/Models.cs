namespace NainLegacyModernizer.Core;

/// <summary>Sample domain used by the bundled legacy and modern demo backends.</summary>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Eur(decimal amount) => new(amount, "EUR");
}

public sealed record OrderItem(string Sku, int Quantity, Money UnitPrice)
{
    public Money Total => new(Quantity * UnitPrice.Amount, UnitPrice.Currency);
}

public sealed record Order(
    string Id,
    string CustomerReference,
    IReadOnlyList<OrderItem> Items,
    DateTimeOffset CreatedAtUtc)
{
    public Money TotalAmount
    {
        get
        {
            var sum = Items.Sum(i => i.Total.Amount);
            var currency = Items.Count > 0 ? Items[0].UnitPrice.Currency : "EUR";
            return new Money(sum, currency);
        }
    }
}
