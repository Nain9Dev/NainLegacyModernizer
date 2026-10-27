using NainLegacyModernizer.Core;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class CutoverPolicyTests
{
    private static readonly CutoverPolicy Policy = new(MinimumSamples: 10, MinimumParityRatio: 0.9m);

    [Fact]
    public void Evaluate_WithTooFewSamples_IsCollecting()
    {
        var summary = Summary(MigrationMode.Shadow, matched: 4, discrepancies: 0, errors: 0);

        var readiness = Policy.Evaluate(summary);

        Assert.Equal(ReadinessState.Collecting, readiness.State);
        Assert.Contains("6 more", readiness.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_WithExecutionErrors_IsBlocked()
    {
        var summary = Summary(MigrationMode.Shadow, matched: 20, discrepancies: 0, errors: 1);

        Assert.Equal(ReadinessState.Blocked, Policy.Evaluate(summary).State);
    }

    [Fact]
    public void Evaluate_BelowParityThreshold_IsBlocked()
    {
        var summary = Summary(MigrationMode.Shadow, matched: 8, discrepancies: 2, errors: 0);

        var readiness = Policy.Evaluate(summary);

        Assert.Equal(ReadinessState.Blocked, readiness.State);
        Assert.Equal(0.8m, readiness.ParityRatio);
    }

    [Fact]
    public void Evaluate_MeetingThreshold_IsReady()
    {
        var summary = Summary(MigrationMode.Shadow, matched: 19, discrepancies: 1, errors: 0);

        Assert.Equal(ReadinessState.Ready, Policy.Evaluate(summary).State);
    }

    [Fact]
    public void Evaluate_RouteAlreadyModern_IsCompleted()
    {
        var summary = Summary(MigrationMode.Modern, matched: 0, discrepancies: 0, errors: 0);

        Assert.Equal(ReadinessState.Completed, Policy.Evaluate(summary).State);
    }

    private static RouteSummary Summary(MigrationMode mode, int matched, int discrepancies, int errors) =>
        new("r", mode, matched + discrepancies + errors, 0, 0, matched, discrepancies, errors, LatencyStats.Empty, LatencyStats.Empty, null);
}
