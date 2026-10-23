using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;

namespace PottyTrain.Api.Services;

public static class InsightsCalculator
{
    public static bool IsSuccess(LogEntry l) => l.Type is LogType.Pee or LogType.Poop && l.InPotty == true;
    public static bool IsAccident(LogEntry l) => l.Type == LogType.Accident || (l.Type is LogType.Pee or LogType.Poop && l.InPotty == false);

    public static InsightsDto Compute(IReadOnlyList<LogEntry> logs, int days, DateTime now)
    {
        var successes = logs.Where(IsSuccess).OrderBy(l => l.OccurredAt).ToList();
        var accidents = logs.Where(IsAccident).OrderBy(l => l.OccurredAt).ToList();
        var attempts = successes.Count + accidents.Count;
        var rate = attempts == 0 ? 0 : Math.Round(100.0 * successes.Count / attempts, 1);

        var lastAccident = accidents.LastOrDefault()?.OccurredAt;
        var dryHours = lastAccident is null ? 0 : (int)Math.Max(0, (now - lastAccident.Value).TotalHours);

        var gaps = successes.Zip(successes.Skip(1), (a, b) => (b.OccurredAt - a.OccurredAt).TotalMinutes)
                            .Where(m => m is > 5 and < 600).ToList();
        var avgGap = gaps.Count == 0 ? 0 : Math.Round(gaps.Average(), 0);

        DateTime? predicted = null;
        if (gaps.Count >= 3 && successes.Count > 0)
        {
            var recent = gaps.TakeLast(10).OrderBy(x => x).ToList();
            var median = recent[recent.Count / 2];
            var next = successes[^1].OccurredAt.AddMinutes(median);
            predicted = next < now ? now.AddMinutes(10) : next;
        }

        var byHour = new int[24];
        foreach (var s in successes) byHour[s.OccurredAt.Hour]++;

        var daily = logs.GroupBy(l => DateOnly.FromDateTime(l.OccurredAt))
            .OrderBy(g => g.Key)
            .Select(g => new DayInsight(g.Key, g.Count(IsSuccess), g.Count(IsAccident),
                g.Count(l => l.Type == LogType.Sit), g.Count(l => l.Type == LogType.Fluid)))
            .ToList();

        return new InsightsDto(days, successes.Count, accidents.Count, rate, dryHours, avgGap, predicted, byHour, daily);
    }
}
