using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NainLegacyModernizer.Core;
using NainLegacyModernizer.ShadowRunner;

namespace NainLegacyModernizer.Gateway.ControlPanel;

/// <summary>HTTP surface of the control panel under <c>/_modernizer</c>.</summary>
public static class ControlPanelEndpoints
{
    public const string BasePath = "/_modernizer";
    public const string ApiPath = BasePath + "/api";

    private const string PanelResourceNamespace = "NainLegacyModernizer.Gateway.Panel";

    public static IEndpointRouteBuilder MapControlPanel(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var files = new EmbeddedFileProvider(typeof(ControlPanelEndpoints).Assembly, PanelResourceNamespace);

        // Matches both "/_modernizer" and "/_modernizer/"; the page references its assets with absolute paths.
        endpoints.MapGet(BasePath, () => ServeFile(files, "index.html", "text/html; charset=utf-8"));
        endpoints.MapGet(BasePath + "/{file}", (string file) =>
        {
            var contentType = Path.GetExtension(file) switch
            {
                ".css" => "text/css; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".svg" => "image/svg+xml",
                ".html" => "text/html; charset=utf-8",
                _ => "application/octet-stream",
            };
            return ServeFile(files, file, contentType);
        });

        var api = endpoints.MapGroup(ApiPath);

        api.MapGet("/overview", (MigrationRouteRegistry registry, IAuditStore store, IOptions<ModernizerOptions> options, TimeProvider time) =>
            Results.Ok(BuildOverview(registry, store, options.Value, time)));

        api.MapGet("/routes", (MigrationRouteRegistry registry, IAuditStore store, IOptions<ModernizerOptions> options) =>
            Results.Ok(BuildRouteViews(registry, store, options.Value.BuildCutoverPolicy())));

        api.MapPut("/routes/{id}/mode", Results<Ok<RouteView>, NotFound, BadRequest<string>> (
            string id,
            ChangeModeRequest request,
            MigrationRouteRegistry registry,
            IAuditStore store,
            IOptions<ModernizerOptions> options) =>
        {
            if (!Enum.IsDefined(request.Mode))
            {
                return TypedResults.BadRequest("Unknown mode.");
            }

            try
            {
                var route = registry.SetMode(id, request.Mode, request.CanaryPercent);
                var summary = store.GetSummary(route.Id, route.Mode);
                return TypedResults.Ok(route.ToView(summary, options.Value.BuildCutoverPolicy()));
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        });

        api.MapGet("/traffic", (IAuditStore store, int take = 100, string? routeId = null) =>
            Results.Ok(store.GetRecentTraffic(Math.Clamp(take, 1, 1000), routeId).Select(t => t.ToView())));

        api.MapGet("/audits", (IAuditStore store, string? routeId = null, ParityVerdict? verdict = null, int take = 50) =>
            Results.Ok(store.GetAudits(routeId, verdict, Math.Clamp(take, 1, 1000)).Select(a => a.ToView())));

        api.MapGet("/audits/{id}", Results<Ok<AuditView>, NotFound> (string id, IAuditStore store) =>
            store.GetAudit(id) is { } audit ? TypedResults.Ok(audit.ToView()) : TypedResults.NotFound());

        api.MapDelete("/audits", (IAuditStore store) =>
        {
            store.Clear();
            return Results.NoContent();
        });

        api.MapGet("/stream", (IAuditStore store, IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> json, CancellationToken cancellationToken) =>
            TypedResults.ServerSentEvents(Stream(store, json.Value.SerializerOptions, cancellationToken)));

        return endpoints;
    }

    private static IResult ServeFile(EmbeddedFileProvider files, string name, string contentType)
    {
        var file = files.GetFileInfo(name);
        return file.Exists
            ? Results.Stream(file.CreateReadStream(), contentType)
            : Results.NotFound();
    }

    private static OverviewView BuildOverview(MigrationRouteRegistry registry, IAuditStore store, ModernizerOptions options, TimeProvider time)
    {
        var policy = options.BuildCutoverPolicy();
        var routes = registry.Routes;
        var summaries = routes.Select(r => store.GetSummary(r.Id, r.Mode)).ToArray();
        var views = routes.Select((r, i) => r.ToView(summaries[i], policy)).ToArray();

        return new OverviewView(
            time.GetUtcNow(),
            options.LegacyBaseAddress,
            options.ModernBaseAddress,
            policy,
            views,
            PanelViews.Totals(views, summaries));
    }

    private static RouteView[] BuildRouteViews(MigrationRouteRegistry registry, IAuditStore store, CutoverPolicy policy) =>
        registry.Routes.Select(r => r.ToView(store.GetSummary(r.Id, r.Mode), policy)).ToArray();

    private static async IAsyncEnumerable<SseItem<string>> Stream(
        IAuditStore store,
        JsonSerializerOptions json,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new SseItem<string>("{}", "connected");

        await foreach (var storeEvent in store.SubscribeAsync(cancellationToken))
        {
            switch (storeEvent.Kind)
            {
                case StoreEventKind.Traffic when storeEvent.Traffic is not null:
                    yield return new SseItem<string>(JsonSerializer.Serialize(storeEvent.Traffic.ToView(), json), "traffic");
                    break;
                case StoreEventKind.Audit when storeEvent.Audit is not null:
                    yield return new SseItem<string>(JsonSerializer.Serialize(storeEvent.Audit.ToView(), json), "audit");
                    break;
                case StoreEventKind.Cleared:
                    yield return new SseItem<string>("{}", "cleared");
                    break;
                default:
                    break;
            }
        }
    }
}
