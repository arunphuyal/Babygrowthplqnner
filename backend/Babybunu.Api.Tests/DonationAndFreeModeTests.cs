using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Babybunu.Api.Tests;

public class FreeModeTests(TestFactory f) : IClassFixture<TestFactory>
{
    [Fact]
    public async Task When_all_free_everyone_gets_premium_features()
    {
        using var free = f.WithWebHostBuilder(b => b.UseSetting("Features:AllFree", "true"));
        var owner = await free.Register();

        // More than one child on the "Free" plan
        var first = await owner.Client.PostAsJsonAsync("/api/children", new { name = "A", birthDate = "2024-01-01" });
        Assert.Equal(HttpStatusCode.Created, (await owner.Client.PostAsJsonAsync("/api/children", new { name = "B", birthDate = "2024-01-01" })).StatusCode);
        var id = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        // Long history window
        Assert.Equal(30, (await owner.Client.GetFromJsonAsync<JsonElement>($"/api/children/{id}/insights?days=30")).GetProperty("days").GetInt32());

        // Caregiver sharing without a paid plan
        Assert.Equal(HttpStatusCode.OK, (await owner.Client.PostAsJsonAsync($"/api/children/{id}/invites", new { canEdit = true })).StatusCode);
    }

    [Fact]
    public async Task Free_mode_still_caps_children_to_stop_abuse()
    {
        using var free = f.WithWebHostBuilder(b =>
        {
            b.UseSetting("Features:AllFree", "true");
            b.UseSetting("Features:MaxChildrenWhenAllFree", "2");
        });
        var s = await free.Register();
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Created, (await s.Client.PostAsJsonAsync("/api/children", new { name = "K" + i, birthDate = "2024-01-01" })).StatusCode);
        Assert.Equal((HttpStatusCode)402, (await s.Client.PostAsJsonAsync("/api/children", new { name = "Z", birthDate = "2024-01-01" })).StatusCode);
    }

    [Fact]
    public async Task Seeded_guides_are_public_and_unlocked()
    {
        var guides = await f.CreateClient().GetFromJsonAsync<JsonElement>("/api/articles");
        Assert.True(guides.GetArrayLength() >= 4);
        var one = await f.CreateClient().GetFromJsonAsync<JsonElement>("/api/articles/potty-training-readiness");
        Assert.False(one.GetProperty("locked").GetBoolean());
        Assert.Contains("ready", one.GetProperty("body").GetString());
    }
}

public class DonationTests(TestFactory f) : IClassFixture<TestFactory>
{
    private const string Secret = "whsec_test";

    private static string Event(string id, string status = "paid", long amount = 500, string purpose = "donation") => JsonSerializer.Serialize(new
    {
        id = "evt_" + id, @object = "event", api_version = "2024-06-20", type = "checkout.session.completed",
        data = new { @object = new { id = "cs_test_" + id, @object = "checkout.session", payment_status = status, amount_total = amount, currency = "usd", metadata = new { purpose } } }
    });

    private static HttpRequestMessage Signed(string payload, string secret = Secret)
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Convert.ToHexString(new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes($"{t}.{payload}"))).ToLowerInvariant();
        return new(HttpMethod.Post, "/api/donations/webhook/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            Headers = { { "Stripe-Signature", $"t={t},v1={sig}" } }
        };
    }

    private async Task<JsonElement> AdminDonations()
    {
        var c = f.CreateClient();
        var login = await (await c.PostAsJsonAsync("/api/auth/login", new { email = "admin@babybunu.local", password = "Admin123!dev" })).Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        return await c.GetFromJsonAsync<JsonElement>("/api/admin/donations");
    }

    [Fact]
    public async Task Config_lists_presets_and_never_exposes_keys()
    {
        var text = await f.CreateClient().GetStringAsync("/api/donations/config");
        var j = JsonDocument.Parse(text).RootElement;
        Assert.True(j.GetProperty("enabled").GetBoolean());
        Assert.Equal(4, j.GetProperty("presetsCents").GetArrayLength());
        Assert.DoesNotContain("sk_test", text);
        Assert.DoesNotContain("whsec", text);
    }

    [Fact]
    public async Task Checkout_returns_the_stripe_url_and_validates_the_amount()
    {
        var c = f.CreateClient();
        var ok = await c.PostAsJsonAsync("/api/donations/checkout", new { amountCents = 1000 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("https://checkout.stripe.test/c/pay_1000", (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/donations/checkout", new { amountCents = 50 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/donations/checkout", new { amountCents = 99_999_999 })).StatusCode);
        Assert.DoesNotContain(50, f.Checkout.Requested);
    }

    [Fact]
    public async Task Checkout_is_unavailable_when_stripe_is_not_configured()
    {
        using var off = f.WithWebHostBuilder(b => b.UseSetting("Donations:StripeSecretKey", ""));
        Assert.False((await off.CreateClient().GetFromJsonAsync<JsonElement>("/api/donations/config")).GetProperty("enabled").GetBoolean());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await off.CreateClient().PostAsJsonAsync("/api/donations/checkout", new { amountCents = 500 })).StatusCode);
    }

    [Fact]
    public async Task Webhook_rejects_bad_signatures()
    {
        var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.SendAsync(Signed(Event("bad"), secret: "whsec_wrong"))).StatusCode);
        var unsigned = new HttpRequestMessage(HttpMethod.Post, "/api/donations/webhook/stripe") { Content = new StringContent(Event("bad2"), Encoding.UTF8, "application/json") };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.SendAsync(unsigned)).StatusCode);
    }

    [Fact]
    public async Task Webhook_records_paid_donations_once()
    {
        var before = (await AdminDonations()).GetProperty("count").GetInt32();
        var payload = Event("once", amount: 2500);
        var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(Signed(payload))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(Signed(payload))).StatusCode); // Stripe retries deliveries

        var after = await AdminDonations();
        Assert.Equal(before + 1, after.GetProperty("count").GetInt32());
        Assert.Contains(after.GetProperty("items").EnumerateArray(), i => i.GetProperty("amountCents").GetInt64() == 2500);
    }

    [Fact]
    public async Task Webhook_ignores_unpaid_and_unrelated_sessions()
    {
        var before = (await AdminDonations()).GetProperty("count").GetInt32();
        var c = f.CreateClient();
        await c.SendAsync(Signed(Event("unpaid", status: "unpaid")));
        await c.SendAsync(Signed(Event("other", purpose: "something-else")));
        Assert.Equal(before, (await AdminDonations()).GetProperty("count").GetInt32());
    }
}
