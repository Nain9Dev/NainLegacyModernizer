using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;
using Yarp.ReverseProxy.Forwarder;

namespace NainLegacyModernizer.Gateway;

/// <summary>
/// Decides, per request, which backend answers and whether the request is mirrored to the modern side.
/// YARP's <see cref="IHttpForwarder"/> does the actual proxying.
/// </summary>
public sealed class StranglerProxy(
    IHttpForwarder forwarder,
    IUpstreamHttpClient upstream,
    MigrationRouteRegistry registry,
    IAuditStore store,
    IShadowAuditor auditor,
    IOptions<ModernizerOptions> options,
    TimeProvider time,
    ILogger<StranglerProxy> logger)
{
    public const string ModeHeader = "X-Modernizer-Mode";
    public const string TargetHeader = "X-Modernizer-Target";

    private static readonly ForwarderRequestConfig RequestConfig = new() { ActivityTimeout = TimeSpan.FromSeconds(100) };

    private readonly ModernizerOptions _options = options.Value;

    public async Task HandleAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var route = registry.Match(context.Request.Path);
        var routeId = route?.Id ?? MigrationRouteRegistry.FallbackRouteId;
        var mode = route?.Mode ?? MigrationMode.Legacy;
        var target = SelectTarget(route);

        if (_options.EmitDiagnosticHeaders)
        {
            context.Response.OnStarting(static state =>
            {
                var (ctx, m, t) = ((HttpContext, MigrationMode, BackendTarget))state;
                ctx.Response.Headers[ModeHeader] = m.ToString();
                ctx.Response.Headers[TargetHeader] = t.ToString();
                return Task.CompletedTask;
            }, (context, mode, target));
        }

        var isShadow = route is not null && mode == MigrationMode.Shadow;
        CapturingStream? capture = null;
        var originalBody = context.Response.Body;

        if (isShadow)
        {
            context.Request.EnableBuffering();
            context.Request.Headers.AcceptEncoding = "identity";
            capture = new CapturingStream(originalBody, _options.MaxCapturedBodyBytes);
            context.Response.Body = capture;
        }

        var destination = target == BackendTarget.Modern ? _options.ModernBaseAddress : _options.LegacyBaseAddress;
        var stopwatch = Stopwatch.StartNew();
        ForwarderError error;
        try
        {
            error = await forwarder.SendAsync(context, destination, upstream.Invoker, RequestConfig);
        }
        finally
        {
            stopwatch.Stop();
            if (capture is not null)
            {
                context.Response.Body = originalBody;
            }
        }

        if (error != ForwarderError.None)
        {
            logger.LogWarning("Forwarding {Method} {Path} to {Target} failed: {Error}", context.Request.Method, context.Request.Path, target, error);
        }

        var path = context.Request.Path + context.Request.QueryString;
        store.Record(new TrafficSample(
            routeId,
            context.Request.Method,
            path,
            mode,
            target,
            context.Response.StatusCode,
            stopwatch.Elapsed,
            time.GetUtcNow()));

        if (!isShadow || route is null || capture is null)
        {
            return;
        }

        var replica = await BuildReplicaAsync(context, path);
        var legacy = new BackendObservation(
            context.Response.StatusCode,
            stopwatch.Elapsed,
            capture.Overflowed ? null : capture.GetText(),
            Error: capture.Overflowed ? $"Legacy body exceeded the {_options.MaxCapturedBodyBytes} byte capture limit." : null);

        var replay = ReplayAsync(route, context.Request.Method, path, legacy, replica);
        if (_options.AwaitShadowReplay)
        {
            await replay;
        }
        else
        {
            _ = replay.ContinueWith(
                t => logger.LogError(t.Exception, "Shadow replay crashed for {RouteId}", route.Id),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }

    private static BackendTarget SelectTarget(MigrationRoute? route) => route?.Mode switch
    {
        MigrationMode.Modern => BackendTarget.Modern,
        MigrationMode.Canary => Random.Shared.Next(100) < route.CanaryPercent ? BackendTarget.Modern : BackendTarget.Legacy,
        _ => BackendTarget.Legacy,
    };

    private async Task ReplayAsync(MigrationRoute route, string method, string path, BackendObservation legacy, HttpRequestMessage replica)
    {
        using (replica)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ShadowTimeoutSeconds));
            var comparison = ParityComparisonOptions.ForRoute(route, _options.DecimalTolerance);

            var audit = await auditor.AuditAsync(
                new ShadowAuditRequest(route.Id, method, path),
                legacy,
                ct => ProbeModernAsync(replica, ct),
                comparison,
                timeout.Token);

            store.Record(audit);
        }
    }

    private async Task<ProbeResponse> ProbeModernAsync(HttpRequestMessage replica, CancellationToken cancellationToken)
    {
        using var response = await upstream.Invoker.SendAsync(replica, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > _options.MaxCapturedBodyBytes)
        {
            throw new InvalidOperationException($"Modern body exceeded the {_options.MaxCapturedBodyBytes} byte capture limit.");
        }

        return new ProbeResponse((int)response.StatusCode, Encoding.UTF8.GetString(bytes));
    }

    private async Task<HttpRequestMessage> BuildReplicaAsync(HttpContext context, string pathAndQuery)
    {
        var request = context.Request;
        var message = new HttpRequestMessage(new HttpMethod(request.Method), new Uri(new Uri(_options.ModernBaseAddress), pathAndQuery));

        byte[]? body = null;
        if (request.ContentLength is > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Body.Position = 0;
            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, context.RequestAborted);
            body = buffer.ToArray();
        }

        if (body is not null)
        {
            message.Content = new ByteArrayContent(body);
        }

        foreach (var header in request.Headers)
        {
            if (IsHopByHop(header.Key))
            {
                continue;
            }

            var values = header.Value.ToArray();
            if (!message.Headers.TryAddWithoutValidation(header.Key, values))
            {
                message.Content?.Headers.TryAddWithoutValidation(header.Key, values);
            }
        }

        message.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");
        return message;
    }

    private static bool IsHopByHop(string header) =>
        header.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) ||
        header.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Write-through stream that keeps a bounded copy of what was written to the client.</summary>
internal sealed class CapturingStream(Stream inner, int limit) : Stream
{
    private readonly MemoryStream _buffer = new();

    public bool Overflowed { get; private set; }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => throw new NotSupportedException();
    }

    public string GetText() => Encoding.UTF8.GetString(_buffer.GetBuffer(), 0, (int)_buffer.Length);

    public override void Flush() => inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        Capture(buffer.AsSpan(offset, count));
        inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Capture(buffer);
        inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Capture(buffer.AsSpan(offset, count));
        return inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Capture(buffer.Span);
        return inner.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Capture(ReadOnlySpan<byte> data)
    {
        if (Overflowed)
        {
            return;
        }

        if (_buffer.Length + data.Length > limit)
        {
            Overflowed = true;
            _buffer.SetLength(0);
            return;
        }

        _buffer.Write(data);
    }
}
