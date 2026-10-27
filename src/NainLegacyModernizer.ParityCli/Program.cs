using System.Text.Json;
using System.Text.Json.Serialization;
using NainLegacyModernizer.ShadowRunner;

namespace NainLegacyModernizer.ParityCli;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            await Console.Out.WriteLineAsync(CliOptions.Usage);
            return args.Length == 0 ? 2 : 0;
        }

        if (!CliOptions.TryParse(args, out var options, out var error))
        {
            await Console.Error.WriteLineAsync(error);
            await Console.Error.WriteLineAsync(CliOptions.Usage);
            return 2;
        }

        using var legacy = new HttpClient { BaseAddress = options.Legacy, Timeout = TimeSpan.FromSeconds(10) };
        using var modern = new HttpClient { BaseAddress = options.Modern, Timeout = TimeSpan.FromSeconds(10) };
        var runner = new ParityRunner(legacy, modern, new JsonParityOptions(options.Ignore, options.Map));

        var report = await runner.RunAllAsync(
            options.Paths,
            options.Legacy.ToString(),
            options.Modern.ToString(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await File.WriteAllTextAsync(options.OutputPath, ParityHtmlRenderer.Render(report));
        if (options.JsonPath is not null)
        {
            await File.WriteAllTextAsync(options.JsonPath, JsonSerializer.Serialize(report, JsonOptions));
        }

        await Console.Out.WriteLineAsync(
            $"{(report.IsReadyToCut ? "READY" : "NOT READY")}: {report.Matched} matched, {report.Discrepant} different, {report.Failed} failed. Report: {options.OutputPath}");
        return CliOptions.ExitCodeFor(report.IsReadyToCut);
    }
}
