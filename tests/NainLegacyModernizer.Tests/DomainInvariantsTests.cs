using NainLegacyModernizer.Core;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class DomainInvariantsTests
{
    [Fact]
    public void Order_CalculatesTotalAmount_WithDeterministicDecimalPrecision()
    {
        var items = new[]
        {
            new OrderItem("ITEM-A", 3, Money.Eur(10.33m)), // 30.99
            new OrderItem("ITEM-B", 2, Money.Eur(5.50m)),  // 11.00
        };

        var order = new Order("ORD-100", "CUST-REF", items, DateTimeOffset.UtcNow);

        Assert.Equal(41.99m, order.TotalAmount.Amount);
        Assert.Equal("EUR", order.TotalAmount.Currency);
    }
}
