using Babybunu.Api.Domain;
using Babybunu.Api.Services;

namespace Babybunu.Api.Tests;

public class PredictionTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private static LogEntry Log(LogType t, DateTime at, bool? inPotty = null, int? ml = null, LogSize? size = null, int? stool = null) =>
        new() { Type = t, OccurredAt = at, InPotty = inPotty, Amount = ml, Size = size, StoolType = stool };

    /// <summary>n days of drink (200ml) then a wee <paramref name="lagMin"/> minutes later.</summary>
    private static List<LogEntry> DrinkWeePairs(int n, int lagMin)
    {
        var logs = new List<LogEntry>();
        for (var i = 0; i < n; i++)
        {
            var drink = Now.AddHours(-30).AddHours(i * 3);
            logs.Add(Log(LogType.Fluid, drink, ml: 200));
            logs.Add(Log(LogType.Pee, drink.AddMinutes(lagMin), inPotty: true));
        }
        return logs;
    }

    [Fact]
    public void MedianGap_needs_at_least_three_gaps()
    {
        var logs = new[] { 300, 200, 100 }.Select(m => Log(LogType.Pee, Now.AddMinutes(-m), true)).ToList();
        Assert.Null(new MedianGapPredictor().Predict(new(logs, Now)));
    }

    [Fact]
    public void MedianGap_adds_median_gap_to_last_success()
    {
        var logs = Enumerable.Range(0, 6).Select(i => Log(LogType.Pee, Now.AddMinutes(-60 - 90 * (5 - i)), true)).ToList();
        var p = new MedianGapPredictor().Predict(new(logs, Now.AddMinutes(-60 + 1)))!;
        Assert.Equal(logs[^1].OccurredAt.AddMinutes(90), p.NextLikelyAt);
        Assert.Equal(0.5, p.Confidence, 2); // 5 gaps of a possible 10, perfectly regular
    }

    [Fact]
    public void Lags_pair_each_wee_with_the_last_drink_only()
    {
        var logs = new List<LogEntry>
        {
            Log(LogType.Fluid, Now.AddMinutes(-100), ml: 200),
            Log(LogType.Fluid, Now.AddMinutes(-70), ml: 200),      // pairs with the wee, not the first drink
            Log(LogType.Pee, Now.AddMinutes(-30), true),
            Log(LogType.Pee, Now.AddMinutes(-10), true),           // no drink before it -> no pair
        };
        Assert.Equal([40d], LogRules.DrinkToUrineLagsMinutes(logs));
    }

    [Fact]
    public void Lags_ignore_sips_but_estimate_ml_from_size()
    {
        var sip = new List<LogEntry> { Log(LogType.Fluid, Now.AddMinutes(-60), size: LogSize.Tiny), Log(LogType.Pee, Now, true) };
        Assert.Empty(LogRules.DrinkToUrineLagsMinutes(sip));
        var cup = new List<LogEntry> { Log(LogType.Fluid, Now.AddMinutes(-60), size: LogSize.Medium), Log(LogType.Pee, Now, true) };
        Assert.Equal([60d], LogRules.DrinkToUrineLagsMinutes(cup));
    }

    [Fact]
    public void Lags_ignore_gaps_over_four_hours()
    {
        var logs = new List<LogEntry> { Log(LogType.Fluid, Now.AddHours(-5), ml: 200), Log(LogType.Pee, Now, true) };
        Assert.Empty(LogRules.DrinkToUrineLagsMinutes(logs));
    }

    [Fact]
    public void FluidAware_predicts_from_recent_drink()
    {
        var logs = DrinkWeePairs(6, 45);
        var drink = Now.AddMinutes(-20);
        logs.Add(Log(LogType.Fluid, drink, ml: 250));
        var p = new FluidAwarePredictor().Predict(new(logs, Now))!;
        Assert.Equal("fluid-aware", p.Model);
        Assert.Equal(drink.AddMinutes(45), p.NextLikelyAt);
    }

    [Fact]
    public void FluidAware_returns_null_without_enough_samples_or_after_a_wee()
    {
        var few = DrinkWeePairs(3, 45);
        few.Add(Log(LogType.Fluid, Now.AddMinutes(-10), ml: 250));
        Assert.Null(new FluidAwarePredictor().Predict(new(few, Now)));

        var voided = DrinkWeePairs(6, 45);
        voided.Add(Log(LogType.Fluid, Now.AddMinutes(-30), ml: 250));
        voided.Add(Log(LogType.Pee, Now.AddMinutes(-5), true));
        Assert.Null(new FluidAwarePredictor().Predict(new(voided, Now)));
    }

    [Fact]
    public void FluidAware_overdue_slightly_means_try_soon_but_stale_means_no_answer()
    {
        var slightly = DrinkWeePairs(6, 45);
        slightly.Add(Log(LogType.Fluid, Now.AddMinutes(-60), ml: 250)); // due 15 min ago
        Assert.Equal(Now.AddMinutes(5), new FluidAwarePredictor().Predict(new(slightly, Now))!.NextLikelyAt);

        var stale = DrinkWeePairs(6, 45);
        stale.Add(Log(LogType.Fluid, Now.AddMinutes(-150), ml: 250)); // due 105 min ago
        Assert.Null(new FluidAwarePredictor().Predict(new(stale, Now)));
    }

    [Fact]
    public void Service_picks_most_confident_model_and_lists_the_rest()
    {
        var svc = new PredictionService([new Fixed("low", 0.3), new Fixed("high", 0.9), new Fixed("none", null)]);
        var (best, alts) = svc.Predict(new([], Now));
        Assert.Equal("high", best!.Model);
        Assert.Equal(["low"], alts.Select(a => a.Model));
    }

    private class Fixed(string name, double? conf) : IPottyPredictor
    {
        public string Name => name;
        public Prediction? Predict(PredictionContext ctx) => conf is null ? null : new(ctx.Now.AddMinutes(10), conf.Value, name, null);
    }
}

public class InsightsTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private static LogEntry Log(LogType t, DateTime at, int? ml = null, int? stool = null, OutputColour? colour = null) =>
        new() { Type = t, OccurredAt = at, Amount = ml, StoolType = stool, Colour = colour };

    [Fact]
    public void Balance_sums_intake_and_counts_outputs_per_day()
    {
        var logs = new List<LogEntry>
        {
            Log(LogType.Fluid, Now.AddHours(-3), ml: 200), Log(LogType.Fluid, Now.AddHours(-2), ml: 100),
            new() { Type = LogType.Fluid, OccurredAt = Now.AddHours(-1), Size = LogSize.Small }, // 90 ml estimate
            Log(LogType.Food, Now.AddHours(-4)), Log(LogType.Pee, Now.AddHours(-1)), Log(LogType.Poop, Now.AddHours(-1)),
        };
        var day = Assert.Single(InsightsCalculator.Compute(logs, 1, Now).Balance);
        Assert.Equal((390, 3, 1, 1, 1), (day.IntakeMl, day.Drinks, day.Meals, day.Urine, day.Stools));
    }

    [Fact]
    public void Flags_no_stool_only_when_stools_are_being_logged()
    {
        var logging = new List<LogEntry> { Log(LogType.Poop, Now.AddDays(-6)), Log(LogType.Poop, Now.AddDays(-4)) };
        Assert.Contains(InsightsCalculator.Compute(logging, 7, Now).Notes, n => n.Code == "no-stool");

        var notLogging = new List<LogEntry> { Log(LogType.Poop, Now.AddDays(-6)) };
        Assert.DoesNotContain(InsightsCalculator.Compute(notLogging, 7, Now).Notes, n => n.Code == "no-stool");
    }

    [Fact]
    public void Flags_hard_stools_and_unusual_colour()
    {
        var logs = new List<LogEntry> { Log(LogType.Poop, Now.AddDays(-1), stool: 1), Log(LogType.Poop, Now.AddHours(-5), stool: 2, colour: OutputColour.Red) };
        var codes = InsightsCalculator.Compute(logs, 7, Now).Notes.Select(n => n.Code).ToList();
        Assert.Contains("hard-stools", codes);
        Assert.Contains("unusual-colour", codes);
    }

    [Fact]
    public void Flags_fewer_pees_than_usual()
    {
        var logs = new List<LogEntry>();
        for (var d = 2; d <= 5; d++) for (var i = 0; i < 6; i++) logs.Add(Log(LogType.Pee, Now.AddDays(-d).AddHours(i)));
        logs.Add(Log(LogType.Pee, Now.AddHours(-2)));
        Assert.Contains(InsightsCalculator.Compute(logs, 7, Now).Notes, n => n.Code == "fewer-pees");
    }

    [Fact]
    public void Intake_output_reports_lag_only_with_enough_samples()
    {
        var logs = new List<LogEntry>();
        for (var i = 0; i < 5; i++) { var t = Now.AddHours(-20 + i * 3); logs.Add(Log(LogType.Fluid, t, ml: 200)); logs.Add(Log(LogType.Pee, t.AddMinutes(50))); }
        var io = InsightsCalculator.Compute(logs, 2, Now).IntakeOutput;
        Assert.Equal(50, io.MedianDrinkToUrineMinutes);
        Assert.Null(InsightsCalculator.Compute(logs.Take(6).ToList(), 2, Now).IntakeOutput.MedianDrinkToUrineMinutes);
    }
}
