using NainLegacyModernizer.Gateway;
using NainLegacyModernizer.LegacyMonolith;
using NainLegacyModernizer.ModernApi;

namespace NainLegacyModernizer.Demo;

/// <summary>
/// One command, whole harness: legacy monolith, modern API and the gateway with its control panel run in a single
/// process. Intended for demos and local evaluation; production deploys each host separately.
/// </summary>
public static class Program
{
    private const string LegacyUrl = "http://127.0.0.1:5000";
    private const string ModernUrl = "http://127.0.0.1:5001";
    private const string GatewayUrl = "http://127.0.0.1:8080";

    public static async Task Main(string[] args)
    {
        var legacy = LegacyMonolithHost.Create(["--urls", LegacyUrl, .. args]);
        var modern = ModernApiHost.Create(["--urls", ModernUrl, .. args]);
        // The gateway's appsettings.json (routes, aliases, samples) is copied next to the demo binary.
        var gateway = GatewayHost.Create(["--urls", GatewayUrl, "--contentRoot", AppContext.BaseDirectory, .. args], builder =>
        {
            builder.Configuration["Modernizer:LegacyBaseAddress"] = LegacyUrl;
            builder.Configuration["Modernizer:ModernBaseAddress"] = ModernUrl;
            builder.Configuration["Logging:LogLevel:Default"] = "Warning";
        });

        await legacy.StartAsync();
        await modern.StartAsync();
        await gateway.StartAsync();

        Console.WriteLine();
        Console.WriteLine("  NainLegacyModernizer demo is running");
        Console.WriteLine($"  Control panel : {GatewayUrl}/_modernizer/");
        Console.WriteLine($"  Gateway       : {GatewayUrl}/api/...");
        Console.WriteLine($"  Legacy        : {LegacyUrl}");
        Console.WriteLine($"  Modern        : {ModernUrl}");
        Console.WriteLine("  Press Ctrl+C to stop.");
        Console.WriteLine();

        await gateway.WaitForShutdownAsync();
        await Task.WhenAll(legacy.StopAsync(), modern.StopAsync());
    }
}
