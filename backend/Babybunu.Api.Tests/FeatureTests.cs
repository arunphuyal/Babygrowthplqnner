using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Babybunu.Api.Domain;
using Babybunu.Api.Services;

namespace Babybunu.Api.Tests;

public class AuthFlowTests(TestFactory f) : IClassFixture<TestFactory>
{
    [Fact]
    public async Task Refresh_rotates_tokens_and_reuse_revokes_the_session()
    {
        var s = await f.Register();
        var r1 = await s.Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = s.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var next = (await r1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(s.RefreshToken, next);

        // Replaying the old token is treated as theft: it fails and kills the newer token too.
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = next })).StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var s = await f.Register();
        await s.Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = s.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, (await s.Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Password_reset_flow()
    {
        var s = await f.Register();
        var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/forgot-password", new { email = s.Email })).StatusCode);
        var mail = f.Emails.Sent.Last(m => m.To == s.Email);
        var token = mail.Body.Split("token=")[1].Split('\n')[0].Trim();

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "brand-new-pw1" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "another-pw-22" })).StatusCode); // single use
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/login", new { email = s.Email, password = "brand-new-pw1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new { email = s.Email, password = "password1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode); // old sessions signed out
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_unknown_emails()
    {
        var before = f.Emails.Sent.Count;
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@nowhere.com" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(before, f.Emails.Sent.Count);
    }

    [Fact]
    public async Task Auth_endpoints_are_rate_limited()
    {
        using var limited = f.WithWebHostBuilder(b => b.UseSetting("RateLimit:AuthPerMinute", "2"));
        var c = limited.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++) codes.Add((await c.PostAsJsonAsync("/api/auth/login", new { email = "x@y.com", password = "nope" })).StatusCode);
        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], codes);
    }
}

public class CaregiverTests(TestFactory f) : IClassFixture<TestFactory>
{
    private static async Task<string> NewChild(HttpClient c) =>
        (await (await c.PostAsJsonAsync("/api/children", new { name = "Leo", birthDate = "2024-03-01", timeZoneId = "Europe/London" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

    [Fact]
    public async Task Sharing_requires_the_family_plan()
    {
        var owner = await f.Register();
        var id = await NewChild(owner.Client);
        Assert.Equal((HttpStatusCode)402, (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).StatusCode);
    }

    [Fact]
    public async Task Caregiver_can_log_with_attribution_but_cannot_manage()
    {
        var owner = await f.Register();
        await f.SetPlan(owner.UserId, "Family");
        var id = await NewChild(owner.Client);
        var invite = await (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).Content.ReadFromJsonAsync<JsonElement>();
        var code = invite.GetProperty("code").GetString()!;

        var nanny = await f.Register();
        Assert.Equal(HttpStatusCode.OK, (await nanny.Client.PostAsJsonAsync("/api/invites/accept", new { code })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await (await f.Register()).Client.PostAsJsonAsync("/api/invites/accept", new { code })).StatusCode); // single use

        var list = await nanny.Client.GetFromJsonAsync<JsonElement>("/api/children");
        Assert.False(list[0].GetProperty("isOwner").GetBoolean());

        Assert.Equal(HttpStatusCode.Created, (await nanny.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Pee", inPotty = true })).StatusCode);
        var logs = await owner.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/logs");
        Assert.StartsWith("Parent", logs[0].GetProperty("loggedBy").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await nanny.Client.DeleteAsync($"/api/children/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await nanny.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await nanny.Client.GetAsync($"/api/children/{id}/caregivers")).StatusCode);

        // Owner removes the caregiver; access is gone.
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"/api/children/{id}/caregivers/{nanny.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await nanny.Client.GetAsync($"/api/children/{id}")).StatusCode);
    }

    [Fact]
    public async Task View_only_caregiver_cannot_add_logs()
    {
        var owner = await f.Register();
        await f.SetPlan(owner.UserId, "Family");
        var id = await NewChild(owner.Client);
        var code = (await (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = false })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;
        var viewer = await f.Register();
        await viewer.Client.PostAsJsonAsync("/api/invites/accept", new { code });
        Assert.Equal(HttpStatusCode.OK, (await viewer.Client.GetAsync($"/api/children/{id}/logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Pee" })).StatusCode);
    }

    [Fact]
    public async Task Strangers_cannot_see_a_child()
    {
        var owner = await f.Register();
        var id = await NewChild(owner.Client);
        Assert.Equal(HttpStatusCode.NotFound, (await (await f.Register()).Client.GetAsync($"/api/children/{id}/logs")).StatusCode);
    }
}

public class BillingAndGatingTests(TestFactory f) : IClassFixture<TestFactory>
{
    private static HttpRequestMessage Webhook(object ev, string? auth = "Bearer whsec") =>
        new(HttpMethod.Post, "/api/billing/webhook/revenuecat")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { @event = ev }), Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", auth ?? "" } }
        };

    private static async Task<string> PlanOf(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("plan").GetString()!;

    [Fact]
    public async Task Webhook_requires_the_shared_secret()
    {
        var s = await f.Register();
        var ev = new { type = "INITIAL_PURCHASE", app_user_id = s.UserId, product_id = "premium_monthly" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().SendAsync(Webhook(ev, "Bearer wrong"))).StatusCode);
        Assert.Equal("Free", await PlanOf(s.Client));
    }

    [Fact]
    public async Task Webhook_grants_and_revokes_plans()
    {
        var s = await f.Register();
        var future = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds();
        var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(Webhook(new { type = "INITIAL_PURCHASE", app_user_id = s.UserId, product_id = "family_yearly", expiration_at_ms = future }))).StatusCode);
        Assert.Equal("Family", await PlanOf(s.Client));

        await c.SendAsync(Webhook(new { type = "EXPIRATION", app_user_id = s.UserId }));
        Assert.Equal("Free", await PlanOf(s.Client));
    }

    [Fact]
    public async Task Lapsed_subscriptions_fall_back_to_free_and_unknown_ids_are_ignored()
    {
        var s = await f.Register();
        var past = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
        var c = f.CreateClient();
        await c.SendAsync(Webhook(new { type = "RENEWAL", app_user_id = s.UserId, product_id = "premium_monthly", expiration_at_ms = past }));
        Assert.Equal("Free", await PlanOf(s.Client));
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(Webhook(new { type = "RENEWAL", app_user_id = "$RCAnonymousID:abc", product_id = "premium_monthly" }))).StatusCode);
    }

    [Fact]
    public async Task Free_plan_gets_a_7_day_window_and_premium_gets_more()
    {
        var s = await f.Register();
        var id = (await (await s.Client.PostAsJsonAsync("/api/children", new { name = "A", birthDate = "2024-01-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        Assert.Equal(7, (await s.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/insights?days=30")).GetProperty("days").GetInt32());
        await f.SetPlan(s.UserId, "Premium");
        Assert.Equal(30, (await s.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/insights?days=30")).GetProperty("days").GetInt32());
    }

    [Fact]
    public async Task Rejects_unknown_time_zones()
    {
        var s = await f.Register();
        var r = await s.Client.PostAsJsonAsync("/api/children", new { name = "A", birthDate = "2024-01-01", timeZoneId = "Mars/Olympus" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}

public class AnalysisTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private static List<LogEntry> StoolsWithTag(string tag, int withTagType, int withoutTagType, int each = 6)
    {
        var logs = new List<LogEntry>();
        for (var i = 0; i < each * 2; i++)
        {
            var at = Now.AddDays(-20 + i);
            var tagged = i % 2 == 0;
            logs.Add(new() { Type = LogType.Food, OccurredAt = at.AddHours(-3), Tags = tagged ? tag : "plain", Category = "meal" });
            logs.Add(new() { Type = LogType.Poop, OccurredAt = at, StoolType = tagged ? withTagType : withoutTagType });
        }
        return logs;
    }

    [Fact]
    public void Reports_a_tag_when_stools_differ_clearly()
    {
        var c = Correlations.Analyze(StoolsWithTag("dairy", 2, 5));
        var dairy = Assert.Single(c, x => x.Factor == "dairy");
        Assert.Contains("harder", dairy.Message);
        Assert.Contains("not a proven cause", dairy.Message);
    }

    [Fact]
    public void Reports_nothing_without_enough_data_or_difference()
    {
        Assert.Empty(Correlations.Analyze(StoolsWithTag("dairy", 2, 5, each: 3)));       // 6 stools < 10
        Assert.DoesNotContain(Correlations.Analyze(StoolsWithTag("dairy", 4, 4)), x => x.Factor == "dairy");
    }

    [Fact]
    public void Reports_low_fluid_days_with_harder_stools()
    {
        var logs = new List<LogEntry>();
        for (var i = 0; i < 12; i++)
        {
            var at = Now.AddDays(-20 + i);
            var low = i % 2 == 0;
            logs.Add(new() { Type = LogType.Fluid, OccurredAt = at.AddHours(-2), Amount = low ? 100 : 600 });
            logs.Add(new() { Type = LogType.Poop, OccurredAt = at, StoolType = low ? 1 : 5 });
        }
        Assert.Contains(Correlations.Analyze(logs), x => x.Factor == "low-fluid");
    }

    [Fact]
    public void Days_and_hours_follow_the_childs_time_zone()
    {
        // 23:30 UTC on 22 Oct is 08:30 on 23 Oct in Tokyo.
        var logs = new List<LogEntry> { new() { Type = LogType.Pee, InPotty = true, OccurredAt = new(2026, 10, 22, 23, 30, 0, DateTimeKind.Utc) } };
        var utc = InsightsCalculator.Compute(logs, 7, Now);
        var tokyo = InsightsCalculator.Compute(logs, 7, Now, tz: InsightsCalculator.ResolveZone("Asia/Tokyo"));
        Assert.Equal(new DateOnly(2026, 10, 22), utc.Daily[0].Date);
        Assert.Equal(new DateOnly(2026, 10, 23), tokyo.Daily[0].Date);
        Assert.Equal(1, tokyo.SuccessesByHour[8]);
        Assert.Equal(1, utc.SuccessesByHour[23]);
    }

    [Fact]
    public void Unknown_zones_fall_back_to_utc()
    {
        Assert.Equal(TimeZoneInfo.Utc, InsightsCalculator.ResolveZone("Mars/Olympus"));
    }

    [Fact]
    public void Premium_only_predictors_are_skipped_for_free_users()
    {
        var svc = new PredictionService([new PremiumFixed(), new FreeFixed()]);
        Assert.Equal("premium", svc.Predict(new([], Now, IsPremium: true)).Best!.Model);
        Assert.Equal("free", svc.Predict(new([], Now, IsPremium: false)).Best!.Model);
    }

    [Fact]
    public void Plan_lapses_to_free_after_expiry()
    {
        Assert.Equal(Plan.Free, new User { Plan = Plan.Premium, PlanExpiresAt = DateTime.UtcNow.AddMinutes(-1) }.EffectivePlan);
        Assert.Equal(Plan.Premium, new User { Plan = Plan.Premium, PlanExpiresAt = DateTime.UtcNow.AddDays(1) }.EffectivePlan);
        Assert.Equal(Plan.Premium, new User { Plan = Plan.Premium }.EffectivePlan);
    }

    private class PremiumFixed : IPottyPredictor
    {
        public string Name => "premium";
        public bool RequiresPremium => true;
        public Prediction? Predict(PredictionContext c) => new(c.Now, 0.9, "premium", null);
    }

    private class FreeFixed : IPottyPredictor
    {
        public string Name => "free";
        public Prediction? Predict(PredictionContext c) => new(c.Now, 0.4, "free", null);
    }
}

public class LanguageTests(TestFactory f) : IClassFixture<TestFactory>
{
    private async Task<HttpClient> Admin()
    {
        var c = f.CreateClient();
        var login = await (await c.PostAsJsonAsync("/api/auth/login", new { email = "admin@babybunu.local", password = "Admin123!dev" })).Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        return c;
    }

    [Fact]
    public async Task Accounts_and_guides_default_to_english()
    {
        var s = await f.Register();
        Assert.Equal("en", (await s.Client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("language").GetString());
        var guides = await f.CreateClient().GetFromJsonAsync<JsonElement>("/api/articles");
        Assert.True(guides.GetArrayLength() >= 4);
    }

    [Fact]
    public async Task Register_accepts_a_language_and_rejects_nonsense()
    {
        var c = f.CreateClient();
        var ok = await c.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@t.com", password = "password1", displayName = "P", language = "pt-BR" });
        Assert.Equal("pt-BR", (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("language").GetString());
        var bad = await c.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@t.com", password = "password1", displayName = "P", language = "english please" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Same_slug_can_exist_once_per_language_and_the_public_api_filters_by_language()
    {
        var admin = await Admin();
        var slug = "tr-" + Guid.NewGuid().ToString("N")[..6];
        object Body(string lang, string title) => new { slug, title, summary = "s", body = "b", isPublished = true, language = lang };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/admin/articles", Body("en", "Hello"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/admin/articles", Body("fr", "Bonjour"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/admin/articles", Body("fr", "Again"))).StatusCode);

        var anon = f.CreateClient();
        Assert.Equal("Hello", (await anon.GetFromJsonAsync<JsonElement>($"/api/articles/{slug}")).GetProperty("title").GetString());
        Assert.Equal("Bonjour", (await anon.GetFromJsonAsync<JsonElement>($"/api/articles/{slug}?lang=fr")).GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/articles/{slug}?lang=de")).StatusCode);
        var en = await anon.GetFromJsonAsync<JsonElement>("/api/articles");
        Assert.DoesNotContain(en.EnumerateArray(), a => a.GetProperty("title").GetString() == "Bonjour");
    }
}
