using NainLegacyModernizer.Core;
using NainLegacyModernizer.Gateway;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class MigrationRouteRegistryTests
{
    [Theory]
    [InlineData("/api/orders/{**rest}", "/api/orders/ORD-1", true)]
    [InlineData("/api/orders/{**rest}", "/api/orders", true)]
    [InlineData("/api/orders/{**rest}", "/API/Orders/a/b/c", true)]
    [InlineData("/api/orders/{**rest}", "/api/ordersx", false)]
    [InlineData("/api/orders/{id}", "/api/orders/ORD-1", true)]
    [InlineData("/api/orders/{id}", "/api/orders/ORD-1/lines", false)]
    [InlineData("/api/orders/{id}/lines", "/api/orders/ORD-1/lines", true)]
    [InlineData("/health", "/health", true)]
    [InlineData("/health", "/health/live", false)]
    public void PathPatternMatcher_MatchesExpectedPaths(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, PathPatternMatcher.IsMatch(pattern, path));
    }

    [Fact]
    public void Match_PrefersMostSpecificPattern()
    {
        var registry = new MigrationRouteRegistry(
        [
            new MigrationRoute { Id = "all-api", PathPattern = "/api/{**rest}", Mode = MigrationMode.Legacy },
            new MigrationRoute { Id = "orders", PathPattern = "/api/orders/{**rest}", Mode = MigrationMode.Modern },
        ]);

        Assert.Equal("orders", registry.Match("/api/orders/1")?.Id);
        Assert.Equal("all-api", registry.Match("/api/invoices/1")?.Id);
        Assert.Null(registry.Match("/health"));
    }

    [Fact]
    public void SetMode_UpdatesRouteForNextMatch_AndKeepsDisplayOrder()
    {
        var registry = new MigrationRouteRegistry(
        [
            new MigrationRoute { Id = "b", PathPattern = "/b/{**rest}" },
            new MigrationRoute { Id = "a", PathPattern = "/a/{x}/{y}" },
        ]);

        var updated = registry.SetMode("b", MigrationMode.Canary, canaryPercent: 55);

        Assert.Equal(MigrationMode.Canary, updated.Mode);
        Assert.Equal(55, updated.CanaryPercent);
        Assert.Equal(MigrationMode.Canary, registry.Match("/b/1")?.Mode);
        Assert.Equal(["b", "a"], registry.Routes.Select(r => r.Id));
    }

    [Fact]
    public void SetMode_UnknownRoute_Throws()
    {
        var registry = new MigrationRouteRegistry([new MigrationRoute { Id = "a", PathPattern = "/a" }]);

        Assert.Throws<KeyNotFoundException>(() => registry.SetMode("nope", MigrationMode.Modern));
    }

    [Fact]
    public void Constructor_RejectsDuplicateIds_AndInvalidPatterns()
    {
        Assert.Throws<InvalidOperationException>(() => new MigrationRouteRegistry(
        [
            new MigrationRoute { Id = "a", PathPattern = "/a" },
            new MigrationRoute { Id = "A", PathPattern = "/b" },
        ]));

        Assert.Throws<InvalidOperationException>(() => new MigrationRouteRegistry(
            [new MigrationRoute { Id = "a", PathPattern = "no-leading-slash" }]));
    }
}
