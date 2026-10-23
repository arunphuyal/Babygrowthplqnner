using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace PottyTrain.Api.Services;

public class DonationOptions
{
    /// <summary>Stripe secret key (sk_live_... / sk_test_...). Empty disables donations. Supply via environment, never commit it.</summary>
    public string StripeSecretKey { get; set; } = "";
    /// <summary>Signing secret of the Stripe webhook endpoint (whsec_...). Empty disables the webhook.</summary>
    public string StripeWebhookSecret { get; set; } = "";
    public string Currency { get; set; } = "usd";
    /// <summary>No initializer on purpose: the configuration binder appends to an existing array instead of replacing it.</summary>
    public int[] PresetsCents { get; set; } = [];
    public int[] Presets => PresetsCents.Length > 0 ? PresetsCents : [300, 500, 1000, 2500];
    public int MinCents { get; set; } = 100;
    public int MaxCents { get; set; } = 50_000;
    public string SuccessUrl { get; set; } = "http://localhost:5173/?donation=thanks";
    public string CancelUrl { get; set; } = "http://localhost:5173/?donation=cancelled";

    public bool Enabled => !string.IsNullOrWhiteSpace(StripeSecretKey);
}

public interface IDonationCheckout
{
    /// <summary>Creates a hosted Stripe Checkout page for a one-off donation and returns its URL.</summary>
    Task<string> CreateAsync(long amountCents, CancellationToken ct = default);
}

public class StripeDonationCheckout(IOptions<DonationOptions> options) : IDonationCheckout
{
    public const string Purpose = "donation";

    public async Task<string> CreateAsync(long amountCents, CancellationToken ct = default)
    {
        var o = options.Value;
        var sessions = new SessionService(new StripeClient(o.StripeSecretKey));
        var session = await sessions.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            SubmitType = "donate",
            SuccessUrl = o.SuccessUrl,
            CancelUrl = o.CancelUrl,
            Metadata = new Dictionary<string, string> { ["purpose"] = Purpose },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = o.Currency, UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Support PottyTrain" }
                    }
                }
            ]
        }, cancellationToken: ct);
        return session.Url;
    }
}
