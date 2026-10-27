using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NainLegacyModernizer.Gateway;
using NainLegacyModernizer.LegacyMonolith;
using NainLegacyModernizer.ModernApi;

namespace NainLegacyModernizer.Tests;

/// <summary>
/// Boots legacy, modern and gateway as in-memory test servers and wires the gateway's upstream HTTP pipeline to the
/// other two by host name. No sockets are opened, so the suite runs anywhere.
/// </summary>
public sealed class GatewayFixture : IAsyncDisposable
{
    public const string LegacyHost = "http://legacy.test";
    public const string ModernHost = "http://modern.test";

    private readonly WebApplicationFactory<LegacyMonolithProgram> _legacy = new();
    private readonly WebApplicationFactory<ModernApiProgram> _modern = new();
    private readonly WebApplicationFactory<GatewayProgram> _gateway;

    public GatewayFixture(Action<IDictionary<string, string?>>? settings = null, bool modernReachable = true)
    {
        var config = new Dictionary<string, string?>
        {
            ["Modernizer:LegacyBaseAddress"] = LegacyHost,
            ["Modernizer:ModernBaseAddress"] = ModernHost,
            ["Modernizer:AwaitShadowReplay"] = "true",
            ["Modernizer:Cutover:MinimumSamples"] = "3",
        };
        settings?.Invoke(config);

        var upstream = new TestUpstreamHttpClient(_legacy.Server.CreateHandler(), modernReachable ? _modern.Server.CreateHandler() : null);

        _gateway = new WebApplicationFactory<GatewayProgram>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in config)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureTestServices(services => services.AddSingleton<IUpstreamHttpClient>(upstream));
        });
    }

    public HttpClient Client => _gateway.CreateClient();

    public IServiceProvider Services => _gateway.Services;

    public async ValueTask DisposeAsync()
    {
        await _gateway.DisposeAsync();
        await _modern.DisposeAsync();
        await _legacy.DisposeAsync();
    }

    private sealed class TestUpstreamHttpClient(HttpMessageHandler legacy, HttpMessageHandler? modern) : IUpstreamHttpClient
    {
        public HttpMessageInvoker Invoker { get; } = new(new HostRoutingHandler(legacy, modern));
    }

    private sealed class HostRoutingHandler(HttpMessageHandler legacy, HttpMessageHandler? modern) : DelegatingHandler
    {
        private readonly HttpMessageInvoker _legacy = new(legacy);
        private readonly HttpMessageInvoker? _modern = modern is null ? null : new HttpMessageInvoker(modern);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // TestServer ignores HTTP/2 hints that YARP adds; normalise to 1.1.
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            if (request.RequestUri!.Host.Equals("legacy.test", StringComparison.OrdinalIgnoreCase))
            {
                return _legacy.SendAsync(request, cancellationToken);
            }

            if (_modern is null)
            {
                throw new HttpRequestException("modern backend unreachable");
            }

            return _modern.SendAsync(request, cancellationToken);
        }
    }
}
