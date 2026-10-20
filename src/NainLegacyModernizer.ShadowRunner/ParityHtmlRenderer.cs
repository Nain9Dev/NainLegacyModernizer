using System.Globalization;
using System.Net;
using System.Text;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>Renders a <see cref="ParityReport"/> as one self-contained HTML file: no script, no external resource.</summary>
public static class ParityHtmlRenderer
{
    // Token values from the NainDev brandbook (tokens.css v1.5).
    private const string Style = """
        :root{--blue-deep:#002B66;--blue-mid:#3C5F94;--paper:#EDF1F6;--surface:#F7F9FC;--line:#C6D2E3;--green-ink:#2E6B00;--red-ink:#C4001A;--black:#1D1D1B}
        *{box-sizing:border-box}
        body{margin:0;background:var(--paper);color:var(--blue-deep);font:17px/1.5 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif}
        main{max-width:960px;margin:0 auto;padding:24px 16px 48px}
        .banner{background:var(--surface);border:3px solid var(--green-ink);border-radius:4px;padding:16px 24px;box-shadow:8px 8px 0 var(--black)}
        .banner.bad{border-color:var(--red-ink);border-style:dashed}
        h1{margin:0;font-size:1.75rem}
        .ok{color:var(--green-ink)}.bad-text{color:var(--red-ink)}
        .counts{margin:8px 0 0;font-weight:600}
        .bar{display:block;width:100%;height:28px;margin:16px 0;border:2px solid var(--blue-deep);border-radius:4px;background:var(--surface)}
        .meta{color:var(--blue-mid);font-size:.9375rem;margin:16px 0}
        code{font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;font-size:.9375em}
        .route{background:var(--surface);border:2px solid var(--blue-mid);border-radius:4px;padding:12px 16px;margin:12px 0}
        .route h2{margin:0;font-size:1.0625rem;display:flex;gap:12px;flex-wrap:wrap;align-items:baseline}
        .state{font-weight:700;border:2px solid;border-radius:4px;padding:0 8px}
        .state-matched{color:var(--green-ink);border-color:var(--green-ink)}
        .state-different{color:var(--red-ink);border-color:var(--red-ink);border-style:dashed}
        .state-failed{color:var(--black);border-color:var(--black);border-style:dotted;border-width:3px}
        table{width:100%;border-collapse:collapse;margin-top:12px;font-size:.9375rem}
        th,td{text-align:left;padding:6px 8px;border-bottom:1px solid var(--blue-mid);vertical-align:top;overflow-wrap:anywhere}
        td.diff{font-weight:700;color:var(--red-ink)}
        .err{margin:8px 0 0}
        """;

    public static string Render(ParityReport report)
    {
        var ready = report.IsReadyToCut;
        var html = new StringBuilder();
        html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        html.Append("<title>Parity report</title>\n<style>\n").Append(Style).Append("\n</style>\n</head>\n<body>\n<main>\n");

        html.Append("<header class=\"banner").Append(ready ? string.Empty : " bad").Append("\">\n");
        html.Append("<h1 class=\"").Append(ready ? "ok" : "bad-text").Append("\">")
            .Append(ready ? "&#10003; Ready to cut over" : "&#10005; Not ready to cut over").Append("</h1>\n");
        html.Append("<p class=\"counts\">")
            .Append(report.Matched.ToString(CultureInfo.InvariantCulture)).Append(" matched &middot; ")
            .Append(report.Discrepant.ToString(CultureInfo.InvariantCulture)).Append(" different &middot; ")
            .Append(report.Failed.ToString(CultureInfo.InvariantCulture)).Append(" failed")
            .Append("</p>\n</header>\n");

        AppendBar(html, report);

        html.Append("<p class=\"meta\">Legacy <code>").Append(Encode(report.LegacyBaseUrl))
            .Append("</code> against modern <code>").Append(Encode(report.ModernBaseUrl))
            .Append("</code> &middot; ").Append(Encode(report.GeneratedAtUtc.ToString("u", CultureInfo.InvariantCulture)))
            .Append("</p>\n");

        if (report.IgnoredFields is { Count: > 0 })
        {
            html.Append("<p class=\"meta\">Ignored fields: ")
                .Append(string.Join(", ", report.IgnoredFields.Select(f => "<code>" + Encode(f) + "</code>")))
                .Append("</p>\n");
        }

        foreach (var route in report.Routes)
        {
            AppendRoute(html, route);
        }

        html.Append("</main>\n</body>\n</html>\n");
        return html.ToString();
    }

    private static void AppendBar(StringBuilder html, ParityReport report)
    {
        var label = string.Create(
            CultureInfo.InvariantCulture,
            $"{report.Matched} matched, {report.Discrepant} different, {report.Failed} failed");
        html.Append("<svg class=\"bar\" viewBox=\"0 0 100 6\" preserveAspectRatio=\"none\" role=\"img\" aria-label=\"")
            .Append(Encode(label)).Append("\">\n");

        if (report.Total > 0)
        {
            double x = 0;
            foreach (var (count, color) in new[] { (report.Matched, "#2E6B00"), (report.Discrepant, "#C4001A"), (report.Failed, "#1D1D1B") })
            {
                if (count == 0)
                {
                    continue;
                }

                var width = count * 100d / report.Total;
                html.Append("<rect x=\"").Append(Number(x)).Append("\" y=\"0\" width=\"").Append(Number(width))
                    .Append("\" height=\"6\" fill=\"").Append(color).Append("\"/>\n");
                x += width;
            }
        }

        html.Append("</svg>\n");
    }

    private static void AppendRoute(StringBuilder html, RouteParityResult route)
    {
        var (css, text) = route.Verdict switch
        {
            ParityVerdict.Matched => ("matched", "&#10003; Matched"),
            ParityVerdict.Discrepancy => ("different", "&ne; Different"),
            _ => ("failed", "! Failed"),
        };

        html.Append("<section class=\"route\">\n<h2><code>").Append(Encode(route.Path)).Append("</code> <span class=\"state state-")
            .Append(css).Append("\">").Append(text).Append("</span></h2>\n");

        if (route.Error is not null)
        {
            html.Append("<p class=\"err\">").Append(Encode(route.Error)).Append("</p>\n");
        }

        if (route.Discrepancies.Count > 0)
        {
            html.Append("<table>\n<thead><tr><th scope=\"col\">Field</th><th scope=\"col\">Legacy</th><th scope=\"col\">Modern</th></tr></thead>\n<tbody>\n");
            foreach (var d in route.Discrepancies)
            {
                html.Append("<tr><th scope=\"row\"><code>").Append(Encode(d.Field)).Append("</code></th><td class=\"diff\">")
                    .Append(Encode(d.LegacyValue)).Append("</td><td class=\"diff\">&ne; ")
                    .Append(Encode(d.ModernValue)).Append("</td></tr>\n");
            }

            html.Append("</tbody>\n</table>\n");
        }

        html.Append("</section>\n");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
