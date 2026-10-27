using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace NainLegacyModernizer.Gateway;

/// <summary>Single HTTP pipeline used both by the YARP forwarder and by shadow replays. Tests swap it for in-memory servers.</summary>
public interface IUpstreamHttpClient
{
    HttpMessageInvoker Invoker { get; }
}

public sealed class SocketsUpstreamHttpClient : IUpstreamHttpClient, IDisposable
{
    public SocketsUpstreamHttpClient()
    {
        Invoker = new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        });
    }

    public HttpMessageInvoker Invoker { get; }

    public void Dispose() => Invoker.Dispose();
}
