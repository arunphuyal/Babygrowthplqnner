using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Tests;

public class CapturingEmailSender : IEmailSender
{
    public ConcurrentQueue<(string To, string Subject, string Body)> Sent { get; } = new();
    public Task SendAsync(string to, string subject, string body) { Sent.Enqueue((to, subject, body)); return Task.CompletedTask; }
}

public class TestFactory : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"pt-test-{Guid.NewGuid():N}.db");
    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Development");
        b.UseSetting("ConnectionStrings:Default", $"Data Source={_db}");
        b.UseSetting("RateLimit:AuthPerMinute", "10000");
        b.UseSetting("Billing:RevenueCatWebhookSecret", "whsec");
        b.ConfigureServices(s => s.AddSingleton<IEmailSender>(Emails));
    }

    public record Session(HttpClient Client, string UserId, string Email, string Token, string RefreshToken);

    public async Task<Session> Register(string? email = null, string password = "password1")
    {
        email ??= $"{Guid.NewGuid():N}@t.com";
        var c = CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/register", new { email, password, displayName = "Parent " + email[..4] });
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        var token = j.GetProperty("token").GetString()!;
        c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return new(c, j.GetProperty("user").GetProperty("id").GetString()!, email, token, j.GetProperty("refreshToken").GetString()!);
    }

    /// <summary>Uses the seeded development admin to set a user's plan.</summary>
    public async Task SetPlan(string userId, string plan)
    {
        var c = CreateClient();
        var login = await (await c.PostAsJsonAsync("/api/auth/login", new { email = "admin@pottytrain.local", password = "Admin123!dev" })).Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("token").GetString());
        var r = await c.PatchAsJsonAsync($"/api/admin/users/{userId}", new { plan });
        r.EnsureSuccessStatusCode();
    }
}
