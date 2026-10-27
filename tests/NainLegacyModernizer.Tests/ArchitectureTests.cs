using NetArchTest.Rules;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly CoreAssembly = typeof(Core.Order).Assembly;
    private static readonly System.Reflection.Assembly ShadowRunnerAssembly = typeof(ShadowRunner.ShadowAuditor).Assembly;

    [Fact]
    public void CoreShouldNotHaveDependencyOnOuterLayers()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NainLegacyModernizer.Gateway",
                "NainLegacyModernizer.ShadowRunner",
                "NainLegacyModernizer.ModernApi",
                "NainLegacyModernizer.LegacyMonolith",
                "NainLegacyModernizer.ParityCli")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Core layer has illegal dependencies on outer layers: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void CoreShouldNotDependOnWebFrameworks()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Mvc")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Core layer must not depend on ASP.NET Core: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ShadowRunnerShouldNotDependOnGatewayOrMonolith()
    {
        var result = Types.InAssembly(ShadowRunnerAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "NainLegacyModernizer.Gateway",
                "NainLegacyModernizer.ModernApi",
                "NainLegacyModernizer.LegacyMonolith",
                "NainLegacyModernizer.ParityCli")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"ShadowRunner must not depend on Gateway or host implementations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
