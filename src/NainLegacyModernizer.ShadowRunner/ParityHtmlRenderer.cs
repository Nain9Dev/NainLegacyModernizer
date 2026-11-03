using System.Globalization;
using System.Net;
using System.Text;
using NainLegacyModernizer.Core;

namespace NainLegacyModernizer.ShadowRunner;

/// <summary>Renders a <see cref="ParityReport"/> as one self-contained HTML file: no script, no external resource.</summary>
public static class ParityHtmlRenderer
{
    // Token values from the NainDev brandbook (tokens.css v1.6); light and dark, print-safe.
    private const string Style = """
        :root{--blue:#0047AB;--blue-deep:#002B66;--blue-mid:#3C5F94;--paper:#EDF1F6;--surface:#F7F9FC;--line:#C6D2E3;--line-strong:#5F7DA6;--green-ink:#2E6B00;--red-ink:#C4001A;--black:#1D1D1B;--bg:var(--paper);--card:var(--surface);--ink:var(--blue-deep);--ink-soft:var(--blue-mid);--heading:var(--blue);--ok:var(--green-ink);--bad:var(--red-ink);--failed:var(--black);--shadow:var(--black);--grid:rgba(0,43,102,.07);--bad-soft:rgba(196,0,26,.08);--ok-soft:rgba(46,107,0,.1);color-scheme:light dark}
        @media (prefers-color-scheme:dark){:root{--bg:#060D1A;--card:#0B1930;--line:#1E3A5F;--line-strong:#4A6FA0;--ink:#EDF1F6;--ink-soft:#94A3B8;--heading:#EDF1F6;--ok:#80FF00;--bad:#FF5C6C;--failed:#F5BA63;--shadow:#000;--grid:rgba(0,255,224,.05);--bad-soft:rgba(255,92,108,.12);--ok-soft:rgba(128,255,0,.1)}}
        *{box-sizing:border-box}
        body{margin:0;background-color:var(--bg);background-image:linear-gradient(to right,var(--grid) 1px,transparent 1px),linear-gradient(to bottom,var(--grid) 1px,transparent 1px);background-size:40px 40px;color:var(--ink);font:17px/1.5 "Urbanist",system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;-webkit-font-smoothing:antialiased}
        main{max-width:960px;margin:0 auto;padding:32px 16px 64px}
        .banner{background:var(--card);border:1px solid var(--line-strong);border-left:8px solid var(--ok);border-radius:4px;padding:20px 24px;box-shadow:12px 12px 0 var(--shadow)}
        .banner.bad{border-left-color:var(--bad)}
        h1{margin:0;font-size:1.75rem;font-weight:800;letter-spacing:-.01em;line-height:1.15}
        .ok{color:var(--ok)}.bad-text{color:var(--bad)}
        .counts{margin:8px 0 0;font-weight:700;color:var(--ink-soft);font-variant-numeric:tabular-nums}
        .bar{display:block;width:100%;height:20px;margin:24px 0 8px;border:1px solid var(--line-strong);border-radius:4px;background:var(--card)}
        .meta{color:var(--ink-soft);font-size:.9375rem;margin:8px 0}
        code{font-family:ui-monospace,"Cascadia Code",SFMono-Regular,Menlo,Consolas,monospace;font-size:.9em}
        .route{background:var(--card);border:1px solid var(--line-strong);border-top:4px solid var(--line-strong);border-radius:4px;padding:16px;margin:16px 0}
        .route:has(.state-matched){border-top-color:var(--ok)}
        .route:has(.state-different){border-top-color:var(--bad)}
        .route:has(.state-failed){border-top-color:var(--failed)}
        .route h2{margin:0;font-size:1.0625rem;display:flex;gap:12px;flex-wrap:wrap;align-items:center;color:var(--heading)}
        .state{font-weight:700;font-size:.9375rem;border:2px solid;border-radius:4px;padding:1px 10px;background:var(--bg)}
        .state-matched{color:var(--ok);border-color:var(--ok)}
        .state-different{color:var(--bad);border-color:var(--bad);border-style:dashed}
        .state-failed{color:var(--failed);border-color:var(--failed);border-style:dotted}
        table{width:100%;border-collapse:collapse;margin-top:12px;font-size:.9375rem}
        th{font-size:.75rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase;color:var(--ink-soft)}
        th,td{text-align:left;padding:8px;border-bottom:1px solid var(--line);vertical-align:top;overflow-wrap:anywhere}
        tbody th{font-size:inherit;letter-spacing:0;text-transform:none;color:inherit;font-weight:400}
        td.diff{font-weight:700;color:var(--bad);background:var(--bad-soft)}
        .err{margin:8px 0 0;color:var(--failed)}
        @media (max-width:560px){main{padding-top:16px}.banner{box-shadow:6px 6px 0 var(--shadow)}}
        @media print{body{background:#fff;color:#000}.banner,.route{box-shadow:none;border-color:#000}}
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
                    .Append(Encode(d.LegacyValue ?? JsonParityComparer.Missing)).Append("</td><td class=\"diff\">&ne; ")
                    .Append(Encode(d.ModernValue ?? JsonParityComparer.Missing)).Append("</td></tr>\n");
            }

            html.Append("</tbody>\n</table>\n");
        }

        html.Append("</section>\n");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
