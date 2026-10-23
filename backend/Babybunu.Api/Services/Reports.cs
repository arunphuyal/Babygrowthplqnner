using System.Globalization;
using System.Net;
using System.Text;
using Babybunu.Api.Domain;
using Babybunu.Api.Dtos;

namespace Babybunu.Api.Services;

public static class ReportBuilder
{
    public const int MaxDays = 90;
    public const string Disclaimer = "This report is generated from information entered by the child's parents or caregivers. It is a tracking summary, not a medical assessment or diagnosis.";

    /// <summary>Converts a local calendar date in <paramref name="tz"/> to the UTC instant at its local midnight.</summary>
    public static DateTime LocalMidnightUtc(DateOnly date, TimeZoneInfo tz)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local)) local = local.AddHours(1); // some zones skip midnight on DST change days
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    /// <summary>Half-open UTC range [start, end) covering the local dates From..To inclusive.</summary>
    public static (DateTime Start, DateTime End) UtcRange(DateOnly from, DateOnly to, TimeZoneInfo tz) =>
        (LocalMidnightUtc(from, tz), LocalMidnightUtc(to.AddDays(1), tz));

    public static string? ValidateRange(DateOnly from, DateOnly to) =>
        to < from ? "'to' must not be before 'from'." :
        to.DayNumber - from.DayNumber + 1 > MaxDays ? $"A report can cover at most {MaxDays} days." : null;

    /// <param name="logs">Logs already restricted to the report range.</param>
    public static ReportDto Build(Child child, IReadOnlyList<LogEntry> logs, DateOnly from, DateOnly to, DateTime now,
        bool includeLogs, IReadOnlyDictionary<Guid, string> names)
    {
        var tz = InsightsCalculator.ResolveZone(child.TimeZoneId);
        var days = to.DayNumber - from.DayNumber + 1;
        var (_, end) = UtcRange(from, to, tz);
        var insights = InsightsCalculator.Compute(logs, days, now < end ? now : end, tz: tz);

        var stool = new int[7];
        foreach (var l in logs.Where(l => l.Type == LogType.Poop && l.StoolType is >= 1 and <= 7)) stool[l.StoolType!.Value - 1]++;

        List<ReportLogRow>? rows = includeLogs
            ? logs.OrderBy(l => l.OccurredAt).Select(l => new ReportLogRow(
                InsightsCalculator.Local(l.OccurredAt, tz).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                l.Type.ToString(), Details(l), l.LoggedByUserId is { } u && names.TryGetValue(u, out var n) ? n : null)).ToList()
            : null;

        var age = (now.Year - child.BirthDate.Year) * 12 + now.Month - child.BirthDate.Month - (now.Day < child.BirthDate.Day ? 1 : 0);
        return new ReportDto(child.Name, age >= 0 ? age : null, tz.Id, from, to, now, insights, stool, rows, Disclaimer);
    }

    public static string Details(LogEntry l)
    {
        var parts = new List<string>();
        if (l.InPotty is { } p) parts.Add(p ? "in potty" : "not in potty");
        if (LogRules.IntakeMl(l) is { } ml && l.Type == LogType.Fluid) parts.Add(l.Amount is null ? $"~{ml} ml" : $"{ml} ml");
        else if (l.Amount is { } a) parts.Add($"{a} ml");
        if (l.Size is { } sz) parts.Add(sz.ToString().ToLowerInvariant());
        if (l.StoolType is { } st) parts.Add($"Bristol {st}");
        if (l.Colour is { } c && c != OutputColour.Normal) parts.Add(c.ToString().ToLowerInvariant());
        if (!string.IsNullOrEmpty(l.Category)) parts.Add(l.Category);
        if (!string.IsNullOrEmpty(l.Tags)) parts.Add(l.Tags.Replace(",", ", "));
        if (!string.IsNullOrWhiteSpace(l.Notes)) parts.Add(l.Notes.Trim());
        return string.Join("; ", parts);
    }
}

public static class ReportRenderer
{
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>A self-contained, print-friendly page (no scripts, no external resources). Every dynamic value is HTML-encoded.</summary>
    public static string Html(ReportDto r)
    {
        var i = r.Insights;
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
          .Append("<meta name=\"robots\" content=\"noindex,nofollow\"><title>Potty training report – ").Append(E(r.ChildName)).Append("</title><style>")
          .Append("body{font:15px/1.5 system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:#1d2b2a;max-width:820px;margin:24px auto;padding:0 16px}")
          .Append("h1{margin:0 0 4px}h2{margin:28px 0 8px;font-size:1.1rem;border-bottom:2px solid #2f9e77;padding-bottom:4px}")
          .Append("table{border-collapse:collapse;width:100%}th,td{border:1px solid #dbe8e3;padding:6px 8px;text-align:left;font-size:.9rem}th{background:#eef7f3}")
          .Append(".tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px}.tile{border:1px solid #dbe8e3;border-radius:10px;padding:10px}")
          .Append(".tile b{display:block;font-size:1.4rem}.muted{color:#5b6b69;font-size:.85rem}@media print{body{margin:0}}</style></head><body>");
        sb.Append("<h1>Potty training report</h1><p><b>").Append(E(r.ChildName)).Append("</b>");
        if (r.AgeMonths is { } m) sb.Append(" · ").Append(m / 12).Append(" y ").Append(m % 12).Append(" m");
        sb.Append("<br><span class=\"muted\">").Append(r.From.ToString("d MMM yyyy", CultureInfo.InvariantCulture)).Append(" – ")
          .Append(r.To.ToString("d MMM yyyy", CultureInfo.InvariantCulture)).Append(" (").Append(E(r.TimeZone)).Append(") · generated ")
          .Append(r.GeneratedAt.ToString("d MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture)).Append("</span></p>");

        sb.Append("<div class=\"tiles\">");
        void Tile(string label, string value) => sb.Append("<div class=\"tile\"><b>").Append(E(value)).Append("</b>").Append(E(label)).Append("</div>");
        Tile("Potty successes", i.TotalSuccesses.ToString());
        Tile("Accidents", i.TotalAccidents.ToString());
        Tile("Success rate", $"{i.SuccessRatePercent:0.#}%");
        Tile("Avg minutes between successes", i.AvgMinutesBetweenSuccesses > 0 ? i.AvgMinutesBetweenSuccesses.ToString("0") : "n/a");
        Tile("Median drink-to-wee (min)", i.IntakeOutput.MedianDrinkToUrineMinutes?.ToString("0") ?? "n/a");
        sb.Append("</div>");

        sb.Append("<h2>Day by day</h2><table><tr><th>Date</th><th>Successes</th><th>Accidents</th><th>Drinks (ml)</th><th>Wees</th><th>Poos</th></tr>");
        var acc = i.Daily.ToDictionary(d => d.Date);
        foreach (var b in i.Balance)
        {
            var a = acc.TryGetValue(b.Date, out var d) ? d : null;
            sb.Append("<tr><td>").Append(b.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)).Append("</td><td>").Append(a?.Successes ?? 0)
              .Append("</td><td>").Append(a?.Accidents ?? 0).Append("</td><td>").Append(b.IntakeMl).Append("</td><td>").Append(b.Urine)
              .Append("</td><td>").Append(b.Stools).Append("</td></tr>");
        }
        sb.Append("</table>");

        if (r.StoolTypeCounts.Any(c => c > 0))
        {
            sb.Append("<h2>Stool types (Bristol scale)</h2><table><tr>");
            for (var t = 1; t <= 7; t++) sb.Append("<th>Type ").Append(t).Append("</th>");
            sb.Append("</tr><tr>");
            foreach (var c in r.StoolTypeCounts) sb.Append("<td>").Append(c).Append("</td>");
            sb.Append("</tr></table>");
        }

        if (i.Notes.Count > 0)
        {
            sb.Append("<h2>Observations</h2><ul>");
            foreach (var n in i.Notes) sb.Append("<li>").Append(E(n.Message)).Append("</li>");
            sb.Append("</ul>");
        }

        if (r.Logs is { Count: > 0 })
        {
            sb.Append("<h2>All entries</h2><table><tr><th>Time</th><th>Type</th><th>Details</th><th>Logged by</th></tr>");
            foreach (var l in r.Logs)
                sb.Append("<tr><td>").Append(E(l.LocalTime)).Append("</td><td>").Append(E(l.Type)).Append("</td><td>").Append(E(l.Details)).Append("</td><td>").Append(E(l.LoggedBy)).Append("</td></tr>");
            sb.Append("</table>");
        }

        sb.Append("<p class=\"muted\">").Append(E(r.Disclaimer)).Append("</p></body></html>");
        return sb.ToString();
    }

    /// <summary>CSV of the individual entries. Cells that start with a formula character are prefixed with an apostrophe so spreadsheets do not execute them.</summary>
    public static string Csv(IEnumerable<LogEntry> logs, TimeZoneInfo tz, IReadOnlyDictionary<Guid, string> names)
    {
        var sb = new StringBuilder("local_time,type,in_potty,size,stool_type,colour,amount_ml,category,tags,notes,logged_by\r\n");
        foreach (var l in logs.OrderBy(l => l.OccurredAt))
            sb.AppendJoin(',', new[]
            {
                InsightsCalculator.Local(l.OccurredAt, tz).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), l.Type.ToString(),
                l.InPotty?.ToString().ToLowerInvariant(), l.Size?.ToString(), l.StoolType?.ToString(), l.Colour?.ToString(),
                l.Amount?.ToString(), l.Category, l.Tags, l.Notes, l.LoggedByUserId is { } u && names.TryGetValue(u, out var n) ? n : null
            }.Select(Cell)).Append("\r\n");
        return sb.ToString();
    }

    public static string Cell(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v[0] is '=' or '+' or '-' or '@' or '\t' or '\r') v = "'" + v;
        return v.Any(c => c is ',' or '"' or '\n' or '\r') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
