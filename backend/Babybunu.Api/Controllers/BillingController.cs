using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Babybunu.Api.Data;
using Babybunu.Api.Domain;

namespace Babybunu.Api.Controllers;

/// <summary>
/// Receives RevenueCat subscription webhooks (RevenueCat in turn handles App Store / Play Store). Configure:
/// Billing:RevenueCatWebhookSecret (the Authorization header value you set in RevenueCat; fails closed when empty)
/// and Billing:ProductPlans, mapping store product ids to a Plan, e.g. "premium_monthly": "Premium".
/// Set the RevenueCat App User ID to the Babybunu user id.
/// </summary>
[Route("api/billing")]
public class BillingController(AppDb db, IConfiguration config, ILogger<BillingController> log) : ApiBase
{
    [HttpPost("webhook/revenuecat")]
    public async Task<IActionResult> RevenueCat([FromBody] JsonElement body)
    {
        var secret = config["Billing:RevenueCatWebhookSecret"];
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(secret) || !FixedEquals(header, $"Bearer {secret}")) return Unauthorized();

        if (!body.TryGetProperty("event", out var ev)) return Ok();
        string? Str(string name) => ev.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var type = Str("type");
        if (!Guid.TryParse(Str("app_user_id"), out var userId)) return Ok(); // anonymous / unknown ids are ignored

        var user = await db.Users.FindAsync(userId);
        if (user is null) return Ok();

        switch (type)
        {
            case "INITIAL_PURCHASE" or "RENEWAL" or "PRODUCT_CHANGE" or "UNCANCELLATION" or "NON_RENEWING_PURCHASE":
                var product = Str("product_id");
                if (product is null || !Enum.TryParse<Plan>(config[$"Billing:ProductPlans:{product}"], out var plan))
                {
                    log.LogWarning("Billing webhook for unmapped product {Product}", product);
                    return Ok();
                }
                user.Plan = plan;
                user.PlanExpiresAt = ev.TryGetProperty("expiration_at_ms", out var ms) && ms.ValueKind == JsonValueKind.Number
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ms.GetInt64()).UtcDateTime : null;
                break;
            case "EXPIRATION":
                user.Plan = Plan.Free;
                user.PlanExpiresAt = null;
                break;
            // CANCELLATION keeps access until expiry; BILLING_ISSUE and the rest need no change.
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
