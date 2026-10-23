using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Tests;

public class ReportUnitTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Child Leo = new() { Name = "Leo", BirthDate = new(2024, 7, 10), TimeZoneId = "Asia/Tokyo" };

    [Fact]
    public void Range_is_in_the_childs_local_days()
    {
        var tz = InsightsCalculator.ResolveZone("Asia/Tokyo");
        var (start, end) = ReportBuilder.UtcRange(new(2026, 10, 22), new(2026, 10, 22), tz);
        Assert.Equal(new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc), start); // 00:00 JST
        Assert.Equal(new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc), end);
    }

    [Theory]
    [InlineData(2026, 10, 5, 2026, 10, 1, false)]   // to before from
    [InlineData(2026, 1, 1, 2026, 4, 10, false)]    // 100 days
    [InlineData(2026, 1, 1, 2026, 3, 31, true)]     // 90 days
    public void Range_validation(int fy, int fm, int fd, int ty, int tm, int td, bool ok)
    {
        var from = new DateOnly(fy, fm, fd); var to = new DateOnly(ty, tm, td);
        Assert.Equal(ok, ReportBuilder.ValidateRange(from, to) is null);
    }

    [Fact]
    public void Report_summarises_counts_stool_types_and_age()
    {
        var logs = new List<LogEntry>
        {
            new() { Type = LogType.Pee, InPotty = true, OccurredAt = Now.AddHours(-5) },
            new() { Type = LogType.Accident, OccurredAt = Now.AddHours(-4) },
            new() { Type = LogType.Poop, InPotty = true, StoolType = 4, OccurredAt = Now.AddHours(-3) },
            new() { Type = LogType.Poop, InPotty = true, StoolType = 4, OccurredAt = Now.AddHours(-2) },
        };
        var r = ReportBuilder.Build(Leo, logs, new(2026, 10, 23), new(2026, 10, 23), Now, includeLogs: false, new Dictionary<Guid, string>());
        Assert.Equal(3, r.Insights.TotalSuccesses);
        Assert.Equal(1, r.Insights.TotalAccidents);
        Assert.Equal(2, r.StoolTypeCounts[3]);
        Assert.Equal(27, r.AgeMonths); // 10 Jul 2024 -> 23 Oct 2026
        Assert.Null(r.Logs);
    }

    [Fact]
    public void Html_escapes_everything_user_supplied()
    {
        var logs = new List<LogEntry> { new() { Type = LogType.Note, OccurredAt = Now.AddHours(-1), Notes = "<script>alert(1)</script>" } };
        var child = new Child { Name = "<img src=x onerror=alert(1)>", BirthDate = new(2024, 1, 1) };
        var html = ReportRenderer.Html(ReportBuilder.Build(child, logs, new(2026, 10, 23), new(2026, 10, 23), Now, true, new Dictionary<Guid, string>()));
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("noindex", html);
        Assert.Contains("not a medical assessment", html);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("plain", "plain")]
    [InlineData(null, "")]
    public void Csv_cells_are_safe(string? input, string expected) => Assert.Equal(expected, ReportRenderer.Cell(input));
}

public class ReportApiTests(TestFactory f) : IClassFixture<TestFactory>
{
    private static async Task<string> Child(HttpClient c) =>
        (await (await c.PostAsJsonAsync("/api/children", new { name = "Leo", birthDate = "2024-03-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

    [Fact]
    public async Task Owner_gets_json_html_and_csv_reports()
    {
        var s = await f.Register();
        var id = await Child(s.Client);
        await s.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Pee", inPotty = true, notes = "=cmd" });
        await s.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Fluid", amount = 200 });

        var json = await s.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/report");
        Assert.Equal("Leo", json.GetProperty("childName").GetString());
        Assert.Equal(1, json.GetProperty("insights").GetProperty("totalSuccesses").GetInt32());

        var html = await s.Client.GetStringAsync($"/api/children/{id}/report?format=html");
        Assert.Contains("Potty training report", html);

        var csv = await s.Client.GetStringAsync($"/api/children/{id}/report?format=csv");
        Assert.StartsWith("local_time,type", csv);
        Assert.Contains("'=cmd", csv);
    }

    [Fact]
    public async Task Rejects_long_ranges_and_bad_formats_and_strangers()
    {
        var s = await f.Register();
        var id = await Child(s.Client);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.GetAsync($"/api/children/{id}/report?from=2026-01-01&to=2026-06-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.Client.GetAsync($"/api/children/{id}/report?format=xml")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await f.Register()).Client.GetAsync($"/api/children/{id}/report")).StatusCode);
    }

    [Fact]
    public async Task Share_link_lifecycle()
    {
        var s = await f.Register();
        var id = await Child(s.Client);
        await s.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Note", notes = "<b>hi</b>" });
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var link = await (await s.Client.PostAsJsonAsync($"/api/children/{id}/report-links",
            new { from = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"), to = today, includeLogs = true, expiresInDays = 3 })).Content.ReadFromJsonAsync<JsonElement>();
        var path = link.GetProperty("path").GetString()!;

        // Anyone with the link can read it: no auth header.
        var anon = f.CreateClient();
        var ok = await anon.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadAsStringAsync();
        Assert.Contains("Leo", body);
        Assert.Contains("&lt;b&gt;hi&lt;/b&gt;", body);
        Assert.Equal("no-store", ok.Headers.CacheControl?.ToString());
        Assert.Contains("noindex", ok.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Equal("no-referrer", ok.Headers.GetValues("Referrer-Policy").Single());

        // Unknown tokens look exactly like revoked ones.
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/r/not-a-real-token")).StatusCode);

        // Listed, then revoked.
        Assert.Single((await s.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/report-links")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await s.Client.DeleteAsync($"/api/children/{id}/report-links/{link.GetProperty("id").GetString()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Expired_links_stop_working()
    {
        var s = await f.Register();
        var id = await Child(s.Client);
        var d = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var link = await (await s.Client.PostAsJsonAsync($"/api/children/{id}/report-links", new { from = d, to = d })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, (await f.CreateClient().GetAsync(link.GetProperty("path").GetString())).StatusCode);

        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            await db.ReportShares.ExecuteUpdateAsync(x => x.SetProperty(r => r.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        }
        Assert.Equal(HttpStatusCode.NotFound, (await f.CreateClient().GetAsync(link.GetProperty("path").GetString())).StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_can_manage_links()
    {
        var owner = await f.Register();
        await f.SetPlan(owner.UserId, "Family");
        var id = await Child(owner.Client);
        var code = (await (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        var nanny = await f.Register();
        await nanny.Client.PostAsJsonAsync("/api/invites/accept", new { code });

        Assert.Equal(HttpStatusCode.OK, (await nanny.Client.GetAsync($"/api/children/{id}/report")).StatusCode); // can view reports
        var d = DateTime.UtcNow.ToString("yyyy-MM-dd");
        Assert.Equal(HttpStatusCode.NotFound, (await nanny.Client.PostAsJsonAsync($"/api/children/{id}/report-links", new { from = d, to = d })).StatusCode);
    }

    [Fact]
    public async Task Admin_can_save_seo_fields_and_the_public_api_exposes_them()
    {
        var c = f.CreateClient();
        var login = await (await c.PostAsJsonAsync("/api/auth/login", new { email = "admin@pottytrain.local", password = "Admin123!dev" })).Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        var slug = "seo-" + Guid.NewGuid().ToString("N")[..6];
        var create = await c.PostAsJsonAsync("/api/admin/articles", new { slug, title = "T", summary = "S", body = "B", isPublished = true, metaTitle = "Meta T", metaDescription = "Meta D", focusKeyword = "potty" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var pub = await f.CreateClient().GetFromJsonAsync<JsonElement>($"/api/articles/{slug}");
        Assert.Equal("Meta T", pub.GetProperty("metaTitle").GetString());
        Assert.Equal("Meta D", pub.GetProperty("metaDescription").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/admin/articles", new { slug = "Bad Slug!", title = "T", summary = "S", body = "B" })).StatusCode);
    }
}
