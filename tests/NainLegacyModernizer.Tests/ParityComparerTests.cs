using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ParityComparerTests
{
    [Fact]
    public void Compare_IdenticalDocuments_ReturnsNoDiscrepancies()
    {
        var diffs = JsonParityComparer.Compare("""{"id":"A","total":10.5,"tags":["x","y"]}""", """{"id":"A","total":10.5,"tags":["x","y"]}""");

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_DifferentString_ReportsFieldWithBothValues()
    {
        var diffs = JsonParityComparer.Compare("""{"status":"Open"}""", """{"status":"Closed"}""");

        var diff = Assert.Single(diffs);
        Assert.Equal(new DiscrepancyDetail("status", "Open", "Closed"), diff);
    }

    [Theory]
    [InlineData("""{"total":450.5}""", """{"total":450.50}""")]
    [InlineData("""{"total":450.50}""", """{"total":4.505e2}""")]
    [InlineData("""{"total":100}""", """{"total":100.00}""")]
    public void Compare_NumbersEqualAsDecimals_AreNotDiscrepancies(string legacy, string modern)
    {
        Assert.Empty(JsonParityComparer.Compare(legacy, modern));
    }

    [Fact]
    public void Compare_NumbersDifferByOneCent_AreDiscrepancies()
    {
        var diffs = JsonParityComparer.Compare("""{"total":99.99}""", """{"total":100.00}""");

        var diff = Assert.Single(diffs);
        Assert.Equal("total", diff.Field);
        Assert.Equal("99.99", diff.LegacyValue);
        Assert.Equal("100.00", diff.ModernValue);
    }

    [Fact]
    public void Compare_NumberAgainstNumericString_IsADiscrepancy()
    {
        Assert.Single(JsonParityComparer.Compare("""{"total":10}""", """{"total":"10"}"""));
    }

    [Theory]
    [InlineData("""{"OrderId":"A"}""", """{"orderId":"A"}""")]
    [InlineData("""{"order_id":"A"}""", """{"orderId":"A"}""")]
    [InlineData("""{"order-id":"A"}""", """{"ORDERID":"A"}""")]
    public void Compare_FieldNamesDifferingOnlyInCaseAndSeparators_AreTheSameField(string legacy, string modern)
    {
        Assert.Empty(JsonParityComparer.Compare(legacy, modern));
    }

    [Fact]
    public void Compare_MappedNames_AreTheSameField()
    {
        var options = new JsonParityOptions(Map: new Dictionary<string, string> { ["OrderID"] = "id" });

        Assert.Empty(JsonParityComparer.Compare("""{"OrderID":"A"}""", """{"id":"A"}""", options));
    }

    [Fact]
    public void Compare_WithoutTheMap_RenamedFieldIsMissingOnBothSides()
    {
        var diffs = JsonParityComparer.Compare("""{"OrderID":"A"}""", """{"id":"A"}""");

        Assert.Equal(2, diffs.Count);
        Assert.Contains(diffs, d => d.Field == "OrderID" && d.ModernValue == "<missing>");
        Assert.Contains(diffs, d => d.Field == "id" && d.LegacyValue == "<missing>");
    }

    [Fact]
    public void Compare_IgnoredFields_AreSkippedOnBothSides()
    {
        var options = new JsonParityOptions(Ignore: new HashSet<string> { "Source", "generated_at" });

        var diffs = JsonParityComparer.Compare(
            """{"id":"A","source":"Legacy","generatedAt":"1"}""",
            """{"id":"A","SOURCE":"Modern","generatedAt":"2"}""",
            options);

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_IgnoringALegacyName_AlsoIgnoresItsMappedModernName()
    {
        var options = new JsonParityOptions(
            Ignore: new HashSet<string> { "CustomerRef" },
            Map: new Dictionary<string, string> { ["CustomerRef"] = "customerReference" });

        var diffs = JsonParityComparer.Compare(
            """{"CustomerRef":"LEGACY-1","id":"A"}""",
            """{"customerReference":"MODERN-1","id":"A"}""",
            options);

        Assert.Empty(diffs);
    }

    [Fact]
    public void Compare_FieldOnOneSideOnly_IsReportedAsMissing()
    {
        var diffs = JsonParityComparer.Compare("""{"id":"A"}""", """{"id":"A","itemsCount":2}""");

        var diff = Assert.Single(diffs);
        Assert.Equal(new DiscrepancyDetail("itemsCount", "<missing>", "2"), diff);
    }

    [Fact]
    public void Compare_NullAgainstMissing_AreDifferent()
    {
        var diffs = JsonParityComparer.Compare("""{"id":"A","note":null}""", """{"id":"A"}""");

        Assert.Equal(new DiscrepancyDetail("note", "null", "<missing>"), Assert.Single(diffs));
    }

    [Fact]
    public void Compare_NestedObjectsAndArrays_UseFieldPaths()
    {
        var diffs = JsonParityComparer.Compare(
            """{"customer":{"name":"Ann"},"items":[{"sku":"S1"},{"sku":"S2"}]}""",
            """{"customer":{"name":"Ann"},"items":[{"sku":"S1"},{"sku":"S9"}]}""");

        var diff = Assert.Single(diffs);
        Assert.Equal(new DiscrepancyDetail("items[1].sku", "S2", "S9"), diff);
    }

    [Fact]
    public void Compare_ArraysOfDifferentLength_ReportExtraElementsAsMissing()
    {
        var diffs = JsonParityComparer.Compare("""{"items":["a"]}""", """{"items":["a","b"]}""");

        Assert.Equal(new DiscrepancyDetail("items[1]", "<missing>", "b"), Assert.Single(diffs));
    }

    [Fact]
    public void Compare_NestedNamesAreNormalisedPerSegment()
    {
        Assert.Empty(JsonParityComparer.Compare("""{"Customer_Info":{"Full-Name":"Ann"}}""", """{"customerInfo":{"fullName":"Ann"}}"""));
    }

    [Fact]
    public void Compare_InvalidJson_Throws()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => JsonParityComparer.Compare("<html/>", "{}"));
    }

    [Fact]
    public void Compare_TopLevelScalars_AreComparedAsTheRootField()
    {
        var diffs = JsonParityComparer.Compare("1", "2");

        Assert.Equal(new DiscrepancyDetail("$", "1", "2"), Assert.Single(diffs));
    }
}
