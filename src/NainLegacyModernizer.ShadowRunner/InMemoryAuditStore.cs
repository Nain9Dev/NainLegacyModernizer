using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

public enum StoreEventKind
{
    Traffic,
    Audit,
    Cleared,
}

public sealed record StoreEvent(StoreEventKind Kind, TrafficSample? Traffic = null, ShadowAuditResult? Audit = null);

/// <summary>Keeps what the gateway observed and feeds the control panel.</summary>
public interface IAuditStore
{
    void Record(TrafficSample sample);

    void Record(ShadowAuditResult audit);

    IReadOnlyList<TrafficSample> GetRecentTraffic(int take = 100, string? routeId = null);

    IReadOnlyList<ShadowAuditResult> GetAudits(string? routeId = null, ParityVerdict? verdict = null, int take = 50);

    ShadowAuditResult? GetAudit(string id);

    RouteSummary GetSummary(string routeId, MigrationMode mode);

    void Clear();

    /// <summary>Live feed of everything recorded after the subscription starts.</summary>
    IAsyncEnumerable<StoreEvent> SubscribeAsync(CancellationToken cancellationToken);
}

/// <summary>Bounded ring buffers protected by a single lock. Intended for verification runs, not for long-term logging.</summary>
public sealed class InMemoryAuditStore(int capacity = 2_000) : IAuditStore
{
    private readonly int _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    private readonly Lock _gate = new();
    private readonly LinkedList<TrafficSample> _traffic = new();
    private readonly LinkedList<ShadowAuditResult> _audits = new();
    private readonly Dictionary<string, ShadowAuditResult> _auditsById = new(StringComparer.Ordinal);
    private readonly List<Channel<StoreEvent>> _subscribers = [];

    public void Record(TrafficSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        lock (_gate)
        {
            _traffic.AddFirst(sample);
            if (_traffic.Count > _capacity)
            {
                _traffic.RemoveLast();
            }
        }

        Publish(new StoreEvent(StoreEventKind.Traffic, Traffic: sample));
    }

    public void Record(ShadowAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        lock (_gate)
        {
            _audits.AddFirst(audit);
            _auditsById[audit.Id] = audit;
            if (_audits.Count > _capacity)
            {
                var evicted = _audits.Last!.Value;
                _audits.RemoveLast();
                _auditsById.Remove(evicted.Id);
            }
        }

        Publish(new StoreEvent(StoreEventKind.Audit, Audit: audit));
    }

    public IReadOnlyList<TrafficSample> GetRecentTraffic(int take = 100, string? routeId = null)
    {
        lock (_gate)
        {
            IEnumerable<TrafficSample> query = _traffic;
            if (routeId is not null)
            {
                query = query.Where(t => t.RouteId == routeId);
            }

            return query.Take(Math.Max(0, take)).ToArray();
        }
    }

    public IReadOnlyList<ShadowAuditResult> GetAudits(string? routeId = null, ParityVerdict? verdict = null, int take = 50)
    {
        lock (_gate)
        {
            IEnumerable<ShadowAuditResult> query = _audits;
            if (routeId is not null)
            {
                query = query.Where(a => a.RouteId == routeId);
            }

            if (verdict is not null)
            {
                query = query.Where(a => a.Verdict == verdict);
            }

            return query.Take(Math.Max(0, take)).ToArray();
        }
    }

    public ShadowAuditResult? GetAudit(string id)
    {
        lock (_gate)
        {
            return _auditsById.GetValueOrDefault(id);
        }
    }

    public RouteSummary GetSummary(string routeId, MigrationMode mode)
    {
        lock (_gate)
        {
            var legacyMs = new List<double>();
            var modernMs = new List<double>();
            int total = 0, byLegacy = 0, byModern = 0;
            DateTimeOffset? lastSeen = null;

            foreach (var sample in _traffic)
            {
                if (sample.RouteId != routeId)
                {
                    continue;
                }

                total++;
                lastSeen ??= sample.ObservedAtUtc;
                if (sample.Target == BackendTarget.Legacy)
                {
                    byLegacy++;
                    legacyMs.Add(sample.Latency.TotalMilliseconds);
                }
                else
                {
                    byModern++;
                    modernMs.Add(sample.Latency.TotalMilliseconds);
                }
            }

            int matched = 0, discrepancies = 0, errors = 0;
            foreach (var audit in _audits)
            {
                if (audit.RouteId != routeId)
                {
                    continue;
                }

                switch (audit.Verdict)
                {
                    case ParityVerdict.Matched:
                        matched++;
                        break;
                    case ParityVerdict.Discrepancy:
                        discrepancies++;
                        break;
                    default:
                        errors++;
                        break;
                }

                if (audit.Modern.Succeeded)
                {
                    modernMs.Add(audit.Modern.Latency.TotalMilliseconds);
                }
            }

            legacyMs.Sort();
            modernMs.Sort();

            return new RouteSummary(
                routeId,
                mode,
                total,
                byLegacy,
                byModern,
                matched,
                discrepancies,
                errors,
                LatencyStats.From(legacyMs),
                LatencyStats.From(modernMs),
                lastSeen);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _traffic.Clear();
            _audits.Clear();
            _auditsById.Clear();
        }

        Publish(new StoreEvent(StoreEventKind.Cleared));
    }

    public async IAsyncEnumerable<StoreEvent> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<StoreEvent>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

        lock (_gate)
        {
            _subscribers.Add(channel);
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            lock (_gate)
            {
                _subscribers.Remove(channel);
            }
        }
    }

    private void Publish(StoreEvent storeEvent)
    {
        Channel<StoreEvent>[] targets;
        lock (_gate)
        {
            if (_subscribers.Count == 0)
            {
                return;
            }

            targets = _subscribers.ToArray();
        }

        foreach (var channel in targets)
        {
            channel.Writer.TryWrite(storeEvent);
        }
    }
}
