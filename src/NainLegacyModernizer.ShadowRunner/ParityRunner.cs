using System.Globalization;
using System.Text.Json;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>
/// Sends the same <c>GET</c> request to a legacy and a modern API and compares the JSON answers.
/// It never sends anything but <c>GET</c>, so it cannot change data on either side.
/// </summary>
public sealed class ParityRunner(HttpClient legacy, HttpClient modern, JsonParityOptions? options = null)
{
    public async Task<RouteParityResult> RunAsync(string path, CancellationToken cancellationToken)
    {
        var relative = new Uri(path.TrimStart('/'), UriKind.Relative);

        var legacyResponse = await FetchAsync(legacy, "legacy", relative, cancellationToken).ConfigureAwait(false);
        var modernResponse = await FetchAsync(modern, "modern", relative, cancellationToken).ConfigureAwait(false);

        if (legacyResponse.Error is not null || modernResponse.Error is not null)
        {
            return Failure(path, legacyResponse.Error ?? modernResponse.Error!);
        }

        var diffs = new List<DiscrepancyDetail>();
        var statusDiffers = legacyResponse.Status != modernResponse.Status;
        if (statusDiffers)
        {
            diffs.Add(new DiscrepancyDetail(
                "$status",
                legacyResponse.Status.ToString(CultureInfo.InvariantCulture),
                modernResponse.Status.ToString(CultureInfo.InvariantCulture)));
        }

        var legacyValid = IsJson(legacyResponse.Body);
        var modernValid = IsJson(modernResponse.Body);
        if (legacyValid && modernValid)
        {
            diffs.AddRange(JsonParityComparer.Compare(legacyResponse.Body, modernResponse.Body, options));
        }
        else if (!statusDiffers)
        {
            var side = !legacyValid ? "legacy" : "modern";
            return Failure(path, $"The {side} response body is not valid JSON.");
        }

        return new RouteParityResult(path, diffs.Count == 0 ? ParityVerdict.Matched : ParityVerdict.Discrepancy, diffs);
    }

    public async Task<ParityReport> RunAllAsync(
        IEnumerable<string> paths,
        string legacyBaseUrl,
        string modernBaseUrl,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken)
    {
        var results = new List<RouteParityResult>();
        foreach (var path in paths)
        {
            results.Add(await RunAsync(path, cancellationToken).ConfigureAwait(false));
        }

        var ignored = (options?.Ignore ?? new HashSet<string>()).Order(StringComparer.Ordinal).ToList();
        return new ParityReport(generatedAtUtc, legacyBaseUrl, modernBaseUrl, results, ignored);
    }

    private static RouteParityResult Failure(string path, string error) =>
        new(path, ParityVerdict.ExecutionError, [], error);

    private readonly record struct Fetched(int Status, string Body, string? Error);

    private static async Task<Fetched> FetchAsync(HttpClient client, string side, Uri relative, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(relative, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new Fetched((int)response.StatusCode, body, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException && !cancellationToken.IsCancellationRequested)
        {
            return new Fetched(0, string.Empty, $"The {side} request failed: {ex.Message}");
        }
    }

    private static bool IsJson(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
