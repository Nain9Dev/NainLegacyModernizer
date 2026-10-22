using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NainLegacyModernizer.Gateway.ControlPanel;
using NainLegacyModernizer.ShadowRunner;

namespace NainLegacyModernizer.Gateway;

public static class Program
{
    public static void Main(string[] args) => GatewayHost.Create(args).Run();
}

/// <summary>Composes the gateway so it can run standalone or inside another process (see the Demo project).</summary>
public static class GatewayHost
{
    public static WebApplication Create(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOptions<ModernizerOptions>()
            .Bind(builder.Configuration.GetSection(ModernizerOptions.SectionName))
            .Validate(o => Uri.IsWellFormedUriString(o.LegacyBaseAddress, UriKind.Absolute), "Modernizer:LegacyBaseAddress must be an absolute URL.")
            .Validate(o => Uri.IsWellFormedUriString(o.ModernBaseAddress, UriKind.Absolute), "Modernizer:ModernBaseAddress must be an absolute URL.")
            .Validate(o => o.MaxCapturedBodyBytes > 0, "Modernizer:MaxCapturedBodyBytes must be positive.")
            .ValidateOnStart();

        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddHttpForwarder();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IUpstreamHttpClient, SocketsUpstreamHttpClient>();
        builder.Services.AddSingleton<MigrationRouteRegistry>(sp =>
            new MigrationRouteRegistry(sp.GetRequiredService<IOptions<ModernizerOptions>>().Value.BuildRoutes()));
        builder.Services.AddSingleton<IAuditStore>(sp =>
            new InMemoryAuditStore(sp.GetRequiredService<IOptions<ModernizerOptions>>().Value.StoreCapacity));
        builder.Services.AddSingleton<IShadowAuditor>(sp => new ShadowAuditor(sp.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton<StranglerProxy>();

        configure?.Invoke(builder);

        var app = builder.Build();

        app.MapControlPanel();
        app.Map("/{**path}", (HttpContext context, StranglerProxy proxy) => proxy.HandleAsync(context));

        return app;
    }

    private static void TryAddSingleton<T>(this IServiceCollection services, T instance)
        where T : class
    {
        if (services.All(d => d.ServiceType != typeof(T)))
        {
            services.AddSingleton(instance);
        }
    }
}

public partial class GatewayProgram;
