using Babybunu.Api.Domain;

namespace Babybunu.Api.Services;

/// <param name="IsPremium">Whether the child's owner has a paid plan; predictors that <see cref="IPottyPredictor.RequiresPremium"/> are skipped otherwise.</param>
public record PredictionContext(IReadOnlyList<LogEntry> Logs, DateTime Now, bool IsPremium = true);
public record Prediction(DateTime NextLikelyAt, double Confidence, string Model, string? Reason);

/// <summary>A strategy that estimates the next likely potty visit. Register implementations in DI; <see cref="PredictionService"/> picks the most confident one.</summary>
public interface IPottyPredictor
{
    string Name { get; }
    bool RequiresPremium => false;
    /// <summary>Returns null when there is not enough (or no relevant) data.</summary>
    Prediction? Predict(PredictionContext ctx);
}

public static class Stats
{
    public static double Median(IReadOnlyCollection<double> xs)
    {
        var s = xs.OrderBy(x => x).ToArray();
        var m = s.Length / 2;
        return s.Length % 2 == 1 ? s[m] : (s[m - 1] + s[m]) / 2;
    }

    /// <summary>Confidence from sample size and consistency (median absolute deviation relative to the median).</summary>
    public static double Confidence(IReadOnlyCollection<double> samples, int fullAt)
    {
        var med = Median(samples);
        if (med <= 0) return 0;
        var mad = Median(samples.Select(x => Math.Abs(x - med)).ToArray());
        var consistency = Math.Clamp(1 - mad / med, 0.1, 1);
        return Math.Round(Math.Min(1.0, samples.Count / (double)fullAt) * consistency, 2);
    }
}

public static class LogRules
{
    public const int MinQualifyingDrinkMl = 60;
    public static readonly TimeSpan MaxDrinkToUrineLag = TimeSpan.FromHours(4);

    public static bool IsSuccess(LogEntry l) => l.Type is LogType.Pee or LogType.Poop && l.InPotty == true;
    public static bool IsAccident(LogEntry l) => l.Type == LogType.Accident || (l.Type is LogType.Pee or LogType.Poop && l.InPotty == false);

    /// <summary>Volume in ml of a drink: the exact amount if logged, otherwise estimated from the rough size.</summary>
    public static int? IntakeMl(LogEntry l)
    {
        if (l.Type != LogType.Fluid) return null;
        if (l.Amount is { } a) return a;
        return l.Size switch { LogSize.Tiny => 30, LogSize.Small => 90, LogSize.Medium => 180, LogSize.Large => 300, _ => null };
    }

    public static bool IsQualifyingDrink(LogEntry l) => IntakeMl(l) >= MinQualifyingDrinkMl;

    /// <summary>
    /// Minutes between a drink and the next pee. Each pee is paired only with the last qualifying drink before it
    /// (so back-to-back drinks are not double counted) and only if it follows within 4 hours.
    /// </summary>
    public static List<double> DrinkToUrineLagsMinutes(IEnumerable<LogEntry> logs)
    {
        var ordered = logs.OrderBy(l => l.OccurredAt).ToList();
        var lags = new List<double>();
        LogEntry? lastDrink = null;
        foreach (var l in ordered)
        {
            if (IsQualifyingDrink(l)) lastDrink = l;
            else if (l.Type == LogType.Pee && lastDrink is not null)
            {
                var gap = l.OccurredAt - lastDrink.OccurredAt;
                if (gap.TotalMinutes >= 5 && gap <= MaxDrinkToUrineLag) lags.Add(gap.TotalMinutes);
                lastDrink = null;
            }
        }
        return lags;
    }
}

/// <summary>Baseline: median time between recent successful potty visits.</summary>
public class MedianGapPredictor : IPottyPredictor
{
    public string Name => "median-gap";

    public Prediction? Predict(PredictionContext ctx)
    {
        var successes = ctx.Logs.Where(LogRules.IsSuccess).OrderBy(l => l.OccurredAt).ToList();
        var gaps = successes.Zip(successes.Skip(1), (a, b) => (b.OccurredAt - a.OccurredAt).TotalMinutes)
                            .Where(m => m is > 5 and < 600).ToList();
        if (gaps.Count < 3) return null;

        var recent = gaps.TakeLast(10).ToList();
        var median = Stats.Median(recent);
        var next = successes[^1].OccurredAt.AddMinutes(median);
        if (next < ctx.Now) next = ctx.Now.AddMinutes(10);
        return new Prediction(next, Stats.Confidence(recent, 10), Name, $"Typical gap between potty successes is about {median:0} min.");
    }
}

/// <summary>Uses the child's own drink-to-pee delay: a recent qualifying drink with no pee since predicts when the next one is due.</summary>
public class FluidAwarePredictor : IPottyPredictor
{
    public const int MinSamples = 5;
    public static readonly TimeSpan MaxOverdue = TimeSpan.FromMinutes(30);
    public string Name => "fluid-aware";
    public bool RequiresPremium => true;

    public Prediction? Predict(PredictionContext ctx)
    {
        var lags = LogRules.DrinkToUrineLagsMinutes(ctx.Logs.Where(l => l.OccurredAt <= ctx.Now));
        if (lags.Count < MinSamples) return null;

        var ordered = ctx.Logs.Where(l => l.OccurredAt <= ctx.Now).OrderBy(l => l.OccurredAt).ToList();
        var drink = ordered.LastOrDefault(LogRules.IsQualifyingDrink);
        if (drink is null) return null;
        if (ordered.Any(l => l.Type == LogType.Pee && l.OccurredAt > drink.OccurredAt)) return null; // already voided since

        var median = Stats.Median(lags);
        var due = drink.OccurredAt.AddMinutes(median);
        if (due < ctx.Now)
        {
            if (ctx.Now - due > MaxOverdue) return null; // stale; let other models answer
            due = ctx.Now.AddMinutes(5);
        }
        return new Prediction(due, Stats.Confidence(lags, 15), Name,
            $"Last drink was {drink.OccurredAt:HH:mm} UTC; this child usually pees about {median:0} min after a drink ({lags.Count} samples).");
    }
}

public class PredictionService(IEnumerable<IPottyPredictor> predictors)
{
    public (Prediction? Best, List<Prediction> Alternatives) Predict(PredictionContext ctx)
    {
        var all = predictors.Where(p => ctx.IsPremium || !p.RequiresPremium).Select(p => p.Predict(ctx)).OfType<Prediction>().OrderByDescending(p => p.Confidence).ToList();
        return (all.FirstOrDefault(), all.Skip(1).ToList());
    }
}
