using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class InMemoryAuditStoreTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_BeyondCapacity_EvictsOldest()
    {
        var store = new InMemoryAuditStore(capacity: 2);

        store.Record(Audit("a1", ParityVerdict.Matched));
        store.Record(Audit("a2", ParityVerdict.Matched));
        store.Record(Audit("a3", ParityVerdict.Discrepancy));

        Assert.Null(store.GetAudit("a1"));
        Assert.NotNull(store.GetAudit("a3"));
        Assert.Equal(["a3", "a2"], store.GetAudits().Select(a => a.Id));
    }

    [Fact]
    public void GetSummary_AggregatesTrafficAndAudits_PerRoute()
    {
        var store = new InMemoryAuditStore();

        store.Record(Traffic("orders", BackendTarget.Legacy, 40));
        store.Record(Traffic("orders", BackendTarget.Legacy, 60));
        store.Record(Traffic("orders", BackendTarget.Modern, 5));
        store.Record(Traffic("other", BackendTarget.Legacy, 999));
        store.Record(Audit("a1", ParityVerdict.Matched, modernMs: 7));
        store.Record(Audit("a2", ParityVerdict.Discrepancy, modernMs: 9));
        store.Record(Audit("a3", ParityVerdict.ExecutionError, modernMs: 0, modernFailed: true));

        var summary = store.GetSummary("orders", MigrationMode.Shadow);

        Assert.Equal(3, summary.TotalRequests);
        Assert.Equal(2, summary.ServedByLegacy);
        Assert.Equal(1, summary.ServedByModern);
        Assert.Equal(1, summary.Matched);
        Assert.Equal(1, summary.Discrepancies);
        Assert.Equal(1, summary.ExecutionErrors);
        Assert.Equal(3, summary.AuditedSamples);
        Assert.Equal(2, summary.LegacyLatency.Count);
        Assert.Equal(40, summary.LegacyLatency.P50Ms);
        Assert.Equal(3, summary.ModernLatency.Count); // 1 served + 2 successful replays
        Assert.Equal(7, summary.ModernLatency.P50Ms);
    }

    [Fact]
    public async Task SubscribeAsync_ReceivesEventsRecordedAfterSubscription()
    {
        var store = new InMemoryAuditStore();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var received = new List<StoreEvent>();
        var reader = Task.Run(async () =>
        {
            await foreach (var e in store.SubscribeAsync(cts.Token))
            {
                received.Add(e);
                if (received.Count == 2)
                {
                    break;
                }
            }
        }, cts.Token);

        await Task.Delay(50, cts.Token);
        store.Record(Traffic("orders", BackendTarget.Legacy, 1));
        store.Record(Audit("a1", ParityVerdict.Matched));
        await reader;

        Assert.Equal([StoreEventKind.Traffic, StoreEventKind.Audit], received.Select(e => e.Kind));
    }

    private static TrafficSample Traffic(string routeId, BackendTarget target, double ms) =>
        new(routeId, "GET", "/x", MigrationMode.Shadow, target, 200, TimeSpan.FromMilliseconds(ms), T0);

    private static ShadowAuditResult Audit(string id, ParityVerdict verdict, double modernMs = 3, bool modernFailed = false) =>
        new(
            id,
            "orders",
            "GET",
            "/x",
            T0,
            verdict,
            new BackendObservation(200, TimeSpan.FromMilliseconds(50), "{}"),
            modernFailed
                ? new BackendObservation(null, TimeSpan.Zero, null, "boom")
                : new BackendObservation(200, TimeSpan.FromMilliseconds(modernMs), "{}"),
            []);
}
