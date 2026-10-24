using Babybunu.Api.Domain;
using Babybunu.Api.Dtos;

namespace Babybunu.Api.Services;

public static class InsightsCalculator
{
    public static DateTime Local(DateTime utc, TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);

    public static TimeZoneInfo ResolveZone(string? id)
    {
        try { return string.IsNullOrEmpty(id) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    /// <param name="tz">The child's time zone; days and hours are bucketed in local time. Defaults to UTC.</param>
    /// <param name="includeCorrelations">Premium feature: tag/fluid-to-stool observations.</param>
    public static InsightsDto Compute(IReadOnlyList<LogEntry> logs, int days, DateTime now, Prediction? prediction = null,
        TimeZoneInfo? tz = null, bool includeCorrelations = false)
    {
        tz ??= TimeZoneInfo.Utc;
        var successes = logs.Where(LogRules.IsSuccess).OrderBy(l => l.OccurredAt).ToList();
        var accidents = logs.Where(LogRules.IsAccident).OrderBy(l => l.OccurredAt).ToList();
        var attempts = successes.Count + accidents.Count;
        var rate = attempts == 0 ? 0 : Math.Round(100.0 * successes.Count / attempts, 1);

        var lastAccident = accidents.LastOrDefault()?.OccurredAt;
        var dryHours = lastAccident is null ? 0 : (int)Math.Max(0, (now - lastAccident.Value).TotalHours);

        var gaps = Gaps(successes, 5, 600);
        var avgGap = gaps.Count == 0 ? 0 : Math.Round(gaps.Average(), 0);

        var byHour = new int[24];
        foreach (var s in successes) byHour[Local(s.OccurredAt, tz).Hour]++;

        var daily = logs.GroupBy(l => DateOnly.FromDateTime(Local(l.OccurredAt, tz))).OrderBy(g => g.Key)
            .Select(g => new DayInsight(g.Key, g.Count(LogRules.IsSuccess), g.Count(LogRules.IsAccident),
                g.Count(l => l.Type == LogType.Sit), g.Count(l => l.Type == LogType.Fluid)))
            .ToList();

        var balance = logs.GroupBy(l => DateOnly.FromDateTime(Local(l.OccurredAt, tz))).OrderBy(g => g.Key)
            .Select(g => new DailyBalance(g.Key, g.Sum(l => LogRules.IntakeMl(l) ?? 0), g.Count(l => l.Type == LogType.Fluid),
                g.Count(l => l.Type == LogType.Food), g.Count(l => l.Type == LogType.Pee), g.Count(l => l.Type == LogType.Poop)))
            .ToList();

        var lags = LogRules.DrinkToUrineLagsMinutes(logs);
        var urineGaps = Gaps(logs.Where(l => l.Type == LogType.Pee).OrderBy(l => l.OccurredAt).ToList(), 5, 720);
        var stoolGaps = Gaps(logs.Where(l => l.Type == LogType.Poop).OrderBy(l => l.OccurredAt).ToList(), 60, 60 * 24 * 14);
        var io = new IntakeOutputDto(
            lags.Count >= FluidAwarePredictor.MinSamples ? Math.Round(Stats.Median(lags), 0) : null, lags.Count,
            urineGaps.Count == 0 ? null : Math.Round(urineGaps.Average(), 0),
            stoolGaps.Count == 0 ? null : Math.Round(stoolGaps.Average() / 60, 1));

        return new InsightsDto(days, successes.Count, accidents.Count, rate, dryHours, avgGap,
            prediction?.NextLikelyAt, prediction?.Model, byHour, daily, balance, io, Notes(logs, now, tz),
            includeCorrelations ? Correlations.Analyze(logs) : []);
    }

    private static List<double> Gaps(IReadOnlyList<LogEntry> ordered, double minMin, double maxMin) =>
        ordered.Zip(ordered.Skip(1), (a, b) => (b.OccurredAt - a.OccurredAt).TotalMinutes).Where(m => m > minMin && m < maxMin).ToList();

    /// <summary>Observations to show parents. Deliberately non-diagnostic: they describe patterns and suggest asking a professional.</summary>
    private static List<InsightNote> Notes(IReadOnlyList<LogEntry> logs, DateTime now, TimeZoneInfo tz)
    {
        var notes = new List<InsightNote>();
        var stools = logs.Where(l => l.Type == LogType.Poop).OrderBy(l => l.OccurredAt).ToList();

        // Only reason about stool frequency if the parent actually logs stools (>= 2 in the window).
        if (stools.Count >= 2 && now - stools[^1].OccurredAt >= TimeSpan.FromDays(3))
            notes.Add(new("no-stool", $"No stool has been logged for {(int)(now - stools[^1].OccurredAt).TotalDays} days. Offering fluids and fiber-rich foods may help. If your child seems uncomfortable or this continues, mention it to your doctor."));

        if (stools.Count(l => l.OccurredAt >= now.AddDays(-3) && l.StoolType is 1 or 2) >= 2)
            notes.Add(new("hard-stools", "Several hard stools (type 1-2) in the last 3 days. More fluids and fiber can help; ask your doctor if it persists."));

        if (logs.Any(l => l.OccurredAt >= now.AddDays(-7) && l.Colour is OutputColour.Red or OutputColour.Black))
            notes.Add(new("unusual-colour", "An unusual color was logged in the last week. Some foods can cause this, but if it is unexplained or persists, please contact your doctor."));

        // Fewer pees than usual in the last 24h compared with the earlier average, needing >= 3 earlier days of data.
        var earlier = logs.Where(l => l.Type == LogType.Pee && l.OccurredAt < now.AddHours(-24)).ToList();
        var earlierDays = earlier.Select(l => DateOnly.FromDateTime(Local(l.OccurredAt, tz))).Distinct().Count();
        if (earlierDays >= 3)
        {
            var avgPerDay = earlier.Count / (double)earlierDays;
            var last24 = logs.Count(l => l.Type == LogType.Pee && l.OccurredAt >= now.AddHours(-24));
            if (avgPerDay >= 4 && last24 <= avgPerDay / 2)
                notes.Add(new("fewer-pees", "Fewer pees than usual were logged in the last 24 hours. Encourage fluids, and contact your doctor if there are signs of dehydration or no pee for 8+ hours."));
        }
        return notes;
    }
}
