using System.Diagnostics.CodeAnalysis;

namespace NainLegacyModernizer.ParityCli;

public sealed record CliOptions(
    Uri Legacy,
    Uri Modern,
    IReadOnlyList<string> Paths,
    IReadOnlySet<string> Ignore,
    IReadOnlyDictionary<string, string> Map,
    string OutputPath,
    string? JsonPath)
{
    public const string DefaultOutputPath = "parity-report.html";

    public const string Usage = """
        nain-parity --legacy <url> --modern <url> --paths <file|/a,/b> [options]

          --legacy <url>     Base URL of the legacy system (http or https)
          --modern <url>     Base URL of the new implementation
          --paths <value>    File with one path per line (# comments allowed), or a comma separated list
          --ignore <a,b>     Field names to skip on both sides
          --map <old=new>    Treat a legacy field name as a modern one (repeatable)
          --out <file>       HTML report (default: parity-report.html)
          --json <file>      Also write a machine-readable summary

        Only GET requests are sent. Exit code: 0 ready to cut over, 1 not ready, 2 invalid arguments.
        """;

    public static int ExitCodeFor(bool readyToCut) => readyToCut ? 0 : 1;

    public static bool TryParse(string[] args, [NotNullWhen(true)] out CliOptions? options, [NotNullWhen(false)] out string? error)
    {
        options = null;
        string? legacy = null;
        string? modern = null;
        string? paths = null;
        var ignore = new HashSet<string>(StringComparer.Ordinal);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var output = DefaultOutputPath;
        string? json = null;

        for (var i = 0; i < args.Length; i += 2)
        {
            var name = args[i];
            if (i + 1 >= args.Length)
            {
                error = $"Option {name} needs a value.";
                return false;
            }

            var value = args[i + 1];
            switch (name)
            {
                case "--legacy": legacy = value; break;
                case "--modern": modern = value; break;
                case "--paths": paths = value; break;
                case "--out": output = value; break;
                case "--json": json = value; break;
                case "--ignore":
                    foreach (var field in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        ignore.Add(field);
                    }

                    break;
                case "--map":
                    var parts = value.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    {
                        error = $"Option --map expects old=new, got '{value}'.";
                        return false;
                    }

                    map[parts[0]] = parts[1];
                    break;
                default:
                    error = $"Unknown option {name}.";
                    return false;
            }
        }

        if (legacy is null || modern is null || paths is null)
        {
            var missing = legacy is null ? "--legacy" : modern is null ? "--modern" : "--paths";
            error = $"Missing required option {missing}.";
            return false;
        }

        if (!TryBaseUri(legacy, out var legacyUri) || !TryBaseUri(modern, out var modernUri))
        {
            error = "--legacy and --modern must be absolute http or https URLs.";
            return false;
        }

        var list = ReadPaths(paths);
        if (list.Count == 0 || list.Any(p => !p.StartsWith('/')))
        {
            error = "--paths must list at least one path, and every path must start with '/'.";
            return false;
        }

        options = new CliOptions(legacyUri, modernUri, list, ignore, map, output, json);
        error = null;
        return true;
    }

    private static bool TryBaseUri(string value, [NotNullWhen(true)] out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https")
        {
            // A trailing slash keeps a base path (for example /app/) when paths are resolved.
            uri = value.EndsWith('/') ? parsed : new Uri(value + "/", UriKind.Absolute);
            return true;
        }

        uri = null;
        return false;
    }

    private static List<string> ReadPaths(string value)
    {
        var lines = File.Exists(value) ? File.ReadAllLines(value) : value.Split(',');
        return lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();
    }
}
