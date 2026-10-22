using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class JsonParityComparerTests
{
    [Fact]
    public void Compare_IgnoresPropertyCasing()
    {
        var diffs = JsonParityComparer.Compare("""{"OrderId":"A","Total":10}""", """{"orderId":"A","total":10}""");

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_AppliesFieldAliases()
    {
        var options = new ParityComparisonOptions
        {
            FieldAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["OrderID"] = "id" },
        };

        var diffs = JsonParityComparer.Compare("""{"OrderID":"A"}""", """{"id":"A"}""", options);

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_SkipsIgnoredFields_OnBothSides()
    {
        var options = new ParityComparisonOptions { IgnoredFields = ["Source", "meta.servedAt"] };

        var diffs = JsonParityComparer.Compare(
            """{"id":"A","Source":"legacy","meta":{"servedAt":"1"}}""",
            """{"id":"A","source":"modern","meta":{"servedAt":"2"}}""",
            options);

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_TreatsNumericStringsAsNumbers()
    {
        var diffs = JsonParityComparer.Compare("""{"total":"450.50"}""", """{"total":450.5}""");

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_HonoursDecimalTolerance()
    {
        var exact = JsonParityComparer.Compare("""{"tax":262.61}""", """{"tax":262.60}""");
        var tolerant = JsonParityComparer.Compare("""{"tax":262.61}""", """{"tax":262.60}""", new ParityComparisonOptions { DecimalTolerance = 0.01m });

        var diff = Assert.Single(exact);
        Assert.Equal("tax", diff.Field);
        Assert.Equal("262.61", diff.LegacyValue);
        Assert.Equal("262.60", diff.ModernValue);
        Assert.Equal(DiscrepancyKind.ValueMismatch, diff.Kind);
        Assert.Empty(tolerant);
    }

    [Fact]
    public void Compare_ReportsMissingFields_WithDirection()
    {
        var diffs = JsonParityComparer.Compare("""{"a":1,"onlyLegacy":true}""", """{"a":1,"onlyModern":true}""");

        Assert.Equal(2, diffs.Count);
        Assert.Contains(diffs, d => d.Field == "onlyLegacy" && d.Kind == DiscrepancyKind.MissingInModern);
        Assert.Contains(diffs, d => d.Field == "onlyModern" && d.Kind == DiscrepancyKind.MissingInLegacy);
    }

    [Fact]
    public void Compare_WalksArraysAndNestedObjects()
    {
        var diffs = JsonParityComparer.Compare(
            """{"lines":[{"sku":"S1","qty":2},{"sku":"S2","qty":1}]}""",
            """{"lines":[{"sku":"S1","qty":2},{"sku":"S2","qty":3}]}""");

        var diff = Assert.Single(diffs);
        Assert.Equal("lines[1].qty", diff.Field);
    }

    [Fact]
    public void Compare_ArrayLengthMismatch_IsReportedOnce()
    {
        var diffs = JsonParityComparer.Compare("""{"lines":[1,2,3]}""", """{"lines":[1]}""");

        var diff = Assert.Single(diffs);
        Assert.Equal("lines.length", diff.Field);
    }

    [Fact]
    public void Compare_TypeMismatch_IsFlagged()
    {
        var diffs = JsonParityComparer.Compare("""{"active":"yes"}""", """{"active":true}""");

        Assert.Equal(DiscrepancyKind.TypeMismatch, Assert.Single(diffs).Kind);
    }

    [Fact]
    public void Compare_NonJsonBodies_FallBackToTextComparison()
    {
        Assert.Empty(JsonParityComparer.Compare("OK\n", "OK"));

        var diff = Assert.Single(JsonParityComparer.Compare("OK", "KO"));
        Assert.Equal("$body", diff.Field);
    }
}
