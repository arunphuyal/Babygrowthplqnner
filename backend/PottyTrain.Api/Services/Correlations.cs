using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;

namespace PottyTrain.Api.Services;

/// <summary>
/// Looks for patterns between what a child consumed in the 24h before a stool and how hard that stool was (Bristol 1-2 hard ... 7 liquid).
/// These are observations from the parent's own logs, never claims of cause, and are only reported with enough data on both sides.
/// </summary>
public static class Correlations
{
    public const int MinStools = 10;
    public const int MinPerGroup = 5;
    public const double MinDifference = 1.0;
    private static readonly TimeSpan Lookback = TimeSpan.FromHours(24);

    public static List<CorrelationDto> Analyze(IReadOnlyList<LogEntry> logs)
    {
        var stools = logs.Where(l => l.Type == LogType.Poop && l.StoolType is not null).OrderBy(l => l.OccurredAt).ToList();
        var result = new List<CorrelationDto>();
        if (stools.Count < MinStools) return result;

        var intake = logs.Where(l => l.Type is LogType.Fluid or LogType.Food).ToList();
        List<LogEntry> Before(LogEntry stool) => intake.Where(i => i.OccurredAt <= stool.OccurredAt && stool.OccurredAt - i.OccurredAt <= Lookback).ToList();
        var windows = stools.Select(Before).ToList();

        // Tags eaten or drunk in the previous 24h.
        var tagSets = windows.Select(w => w.SelectMany(i => (i.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)).ToHashSet()).ToList();
        foreach (var tag in tagSets.SelectMany(t => t).Distinct().OrderBy(t => t))
        {
            var exposed = stools.Where((_, i) => tagSets[i].Contains(tag)).Select(s => (double)s.StoolType!.Value).ToList();
            var other = stools.Where((_, i) => !tagSets[i].Contains(tag)).Select(s => (double)s.StoolType!.Value).ToList();
            if (Compare(exposed, other) is { } d)
                result.Add(new(tag, $"Stools within 24h of \"{tag}\" were {(d.Diff < 0 ? "harder" : "softer")} on average (type {exposed.Average():0.0} vs {other.Average():0.0}). This is an observation from your logs, not a proven cause.",
                    Math.Round(exposed.Average(), 1), Math.Round(other.Average(), 1), exposed.Count, other.Count));
        }

        // Low vs high fluid intake in the previous 24h, split at the median.
        var fluid = windows.Select(w => w.Sum(i => LogRules.IntakeMl(i) ?? 0)).ToList();
        if (fluid.Any(f => f > 0))
        {
            var median = Stats.Median(fluid.Select(f => (double)f).ToList());
            var low = stools.Where((_, i) => fluid[i] < median).Select(s => (double)s.StoolType!.Value).ToList();
            var high = stools.Where((_, i) => fluid[i] >= median).Select(s => (double)s.StoolType!.Value).ToList();
            if (Compare(low, high) is { Diff: < 0 })
                result.Add(new("low-fluid", $"Stools after lower-fluid days (under about {median:0} ml) were harder on average (type {low.Average():0.0} vs {high.Average():0.0}). More fluids may help; this is an observation, not a proven cause.",
                    Math.Round(low.Average(), 1), Math.Round(high.Average(), 1), low.Count, high.Count));
        }
        return result;
    }

    private static (double Diff, double A, double B)? Compare(List<double> a, List<double> b)
    {
        if (a.Count < MinPerGroup || b.Count < MinPerGroup) return null;
        var diff = a.Average() - b.Average();
        return Math.Abs(diff) >= MinDifference ? (diff, a.Average(), b.Average()) : null;
    }
}
