using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Babybunu.Api.Data;

namespace Babybunu.Api.Tests;

public class AccountTests(TestFactory f) : IClassFixture<TestFactory>
{
    private static string TokenIn(string body) => body.Split("token=")[1].Split('\n')[0].Trim();

    private static async Task<string> NewChild(HttpClient c) =>
        (await (await c.PostAsJsonAsync("/api/children", new { name = "Leo", birthDate = "2024-03-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

    [Fact]
    public async Task Email_verification_flow()
    {
        var s = await f.Register();
        Assert.False((await s.Client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("emailVerified").GetBoolean());

        var mail = f.Emails.Sent.Last(m => m.To == s.Email && m.Subject.StartsWith("Confirm your email"));
        var token = TokenIn(mail.Body);
        var anon = f.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await anon.PostAsJsonAsync("/api/auth/verify-email", new { token })).StatusCode);
        Assert.True((await s.Client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("emailVerified").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await anon.PostAsJsonAsync("/api/auth/verify-email", new { token })).StatusCode); // single use
        Assert.Equal(HttpStatusCode.BadRequest, (await anon.PostAsJsonAsync("/api/auth/verify-email", new { token = "nonsense" })).StatusCode);
    }

    [Fact]
    public async Task Resend_replaces_the_old_link_and_does_nothing_once_verified()
    {
        var s = await f.Register();
        var first = TokenIn(f.Emails.Sent.Last(m => m.To == s.Email).Body);
        Assert.Equal(HttpStatusCode.NoContent, (await s.Client.PostAsync("/api/auth/resend-verification", null)).StatusCode);
        var second = TokenIn(f.Emails.Sent.Last(m => m.To == s.Email).Body);
        Assert.NotEqual(first, second);

        var anon = f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await anon.PostAsJsonAsync("/api/auth/verify-email", new { token = first })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await anon.PostAsJsonAsync("/api/auth/verify-email", new { token = second })).StatusCode);

        var before = f.Emails.Sent.Count;
        await s.Client.PostAsync("/api/auth/resend-verification", null);
        Assert.Equal(before, f.Emails.Sent.Count);
    }

    [Fact]
    public async Task Completing_a_password_reset_also_verifies_the_email()
    {
        var s = await f.Register();
        await f.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email = s.Email });
        var token = TokenIn(f.Emails.Sent.Last(m => m.To == s.Email && m.Subject.StartsWith("Reset")).Body);
        await f.CreateClient().PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "another-pass-1" });
        var login = await (await f.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = s.Email, password = "another-pass-1" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(login.GetProperty("user").GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task Export_contains_my_data_and_only_my_entries_on_shared_children()
    {
        var owner = await f.Register();
        await f.SetPlan(owner.UserId, "Family");
        var id = await NewChild(owner.Client);
        await owner.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Pee", inPotty = true, notes = "owner entry" });
        var code = (await (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        var nanny = await f.Register();
        await nanny.Client.PostAsJsonAsync("/api/invites/accept", new { code });
        await nanny.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Poop", inPotty = true, stoolType = 4, notes = "nanny entry" });

        var ownerText = await owner.Client.GetStringAsync("/api/auth/me/export");
        Assert.Contains("owner entry", ownerText);
        Assert.Contains("nanny entry", ownerText); // it is the owner's child's data
        Assert.DoesNotContain("passwordHash", ownerText, StringComparison.OrdinalIgnoreCase);

        var nannyText = await nanny.Client.GetStringAsync("/api/auth/me/export");
        Assert.Contains("nanny entry", nannyText);
        Assert.DoesNotContain("owner entry", nannyText); // never another family's data
        Assert.DoesNotContain("passwordHash", nannyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deleting_the_account_removes_everything_it_owns_and_anonymises_entries_elsewhere()
    {
        var owner = await f.Register();
        await f.SetPlan(owner.UserId, "Family");
        var id = await NewChild(owner.Client);
        var code = (await (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
        var nanny = await f.Register();
        await nanny.Client.PostAsJsonAsync("/api/invites/accept", new { code });
        await nanny.Client.PostAsJsonAsync($"/api/children/{id}/logs", new { type = "Pee", inPotty = true });

        // The nanny also owns a child of her own.
        var hers = await NewChild(nanny.Client);
        await nanny.Client.PostAsJsonAsync($"/api/children/{hers}/logs", new { type = "Pee", inPotty = true });

        // Wrong password is rejected and nothing is deleted.
        Assert.Equal(HttpStatusCode.Unauthorized, (await DeleteWith(nanny.Client, "wrong-password")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await nanny.Client.GetAsync($"/api/children/{hers}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteWith(nanny.Client, "password1")).StatusCode);

        // Her sign-in, tokens and child are gone; the owner's entry stays but is no longer attributed to her.
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = nanny.Email, password = "password1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = nanny.RefreshToken })).StatusCode);
        var logs = await owner.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/logs");
        Assert.Equal(1, logs.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, logs[0].GetProperty("loggedBy").ValueKind);

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        Assert.False(await db.Children.AnyAsync(c => c.Id == Guid.Parse(hers)));
        Assert.False(await db.Logs.AnyAsync(l => l.ChildId == Guid.Parse(hers)));
        Assert.False(await db.Caregivers.AnyAsync(g => g.UserId == Guid.Parse(nanny.UserId)));
    }

    [Fact]
    public async Task The_only_administrator_cannot_delete_themselves()
    {
        var c = f.CreateClient();
        var login = await (await c.PostAsJsonAsync("/api/auth/login", new { email = "admin@babybunu.local", password = "Admin123!dev" })).Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await DeleteWith(c, "Admin123!dev")).StatusCode);
    }

    private static Task<HttpResponseMessage> DeleteWith(HttpClient c, string password) =>
        c.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/auth/me") { Content = JsonContent.Create(new { password }) });
}

public class OfflineSyncTests(TestFactory f) : IClassFixture<TestFactory>
{
    [Fact]
    public async Task Retrying_a_log_with_the_same_id_does_not_duplicate_it()
    {
        var s = await f.Register();
        var child = (await (await s.Client.PostAsJsonAsync("/api/children", new { name = "A", birthDate = "2024-01-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var id = Guid.NewGuid();
        var body = new { id, type = "Pee", inPotty = true };

        var first = await s.Client.PostAsJsonAsync($"/api/children/{child}/logs", body);
        var retry = await s.Client.PostAsJsonAsync($"/api/children/{child}/logs", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(id.ToString(), (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString());
        Assert.Equal(1, (await s.Client.GetFromJsonAsync<JsonElement>($"/api/children/{child}/logs")).GetArrayLength());
    }

    [Fact]
    public async Task An_id_belonging_to_someone_elses_entry_is_rejected_without_leaking_it()
    {
        var a = await f.Register();
        var b = await f.Register();
        var childA = (await (await a.Client.PostAsJsonAsync("/api/children", new { name = "A", birthDate = "2024-01-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var childB = (await (await b.Client.PostAsJsonAsync("/api/children", new { name = "B", birthDate = "2024-01-01" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var id = Guid.NewGuid();
        await a.Client.PostAsJsonAsync($"/api/children/{childA}/logs", new { id, type = "Pee" });

        var clash = await b.Client.PostAsJsonAsync($"/api/children/{childB}/logs", new { id, type = "Pee" });
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.DoesNotContain(childA!, await clash.Content.ReadAsStringAsync());
    }
}

public class SecurityHeaderTests(TestFactory f) : IClassFixture<TestFactory>
{
    [Fact]
    public async Task Responses_carry_baseline_security_headers_and_report_pages_keep_their_stricter_ones()
    {
        var r = await f.CreateClient().GetAsync("/api/plans");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", r.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Oversized_bodies_are_rejected()
    {
        var big = new StringContent(new string('x', 2_000_000), System.Text.Encoding.UTF8, "application/json");
        var r = await f.CreateClient().PostAsync("/api/waitlist", big);
        Assert.True(r.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest, r.StatusCode.ToString());
    }
}
