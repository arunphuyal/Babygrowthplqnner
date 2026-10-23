using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;
using Stripe;
using Stripe.Checkout;

namespace PottyTrain.Api.Controllers;

/// <summary>
/// One-off Stripe donations. The browser asks for a Checkout URL, pays on Stripe's hosted page, and Stripe's
/// <c>checkout.session.completed</c> webhook records the amount (idempotently, keyed by the session id).
/// Setup: set Donations:StripeSecretKey and Donations:StripeWebhookSecret, and point a Stripe webhook for
/// checkout.session.completed at /api/donations/webhook/stripe.
/// </summary>
[Route("api/donations")]
public class DonationsController(AppDb db, IOptions<DonationOptions> options, IDonationCheckout checkout, ILogger<DonationsController> log) : ApiBase
{
    private DonationOptions O => options.Value;

    [HttpGet("config")]
    public DonationConfigDto Config() => new(O.Enabled, O.Currency, O.Presets, O.MinCents, O.MaxCents);

    [HttpPost("checkout"), EnableRateLimiting("public")]
    public async Task<IActionResult> Checkout(DonationCheckoutRequest req, CancellationToken ct)
    {
        if (!O.Enabled) return StatusCode(503, new { error = "Donations are not enabled." });
        if (req.AmountCents < O.MinCents || req.AmountCents > O.MaxCents)
            return BadRequest(new { error = $"Amount must be between {O.MinCents} and {O.MaxCents} (in the smallest currency unit)." });
        try
        {
            return Ok(new { url = await checkout.CreateAsync(req.AmountCents, ct) });
        }
        catch (StripeException e)
        {
            log.LogError(e, "Stripe checkout creation failed");
            return StatusCode(502, new { error = "Could not start checkout. Please try again later." });
        }
    }

    [HttpPost("webhook/stripe")]
    public async Task<IActionResult> Webhook()
    {
        if (string.IsNullOrWhiteSpace(O.StripeWebhookSecret)) return StatusCode(503);
        if (string.IsNullOrEmpty(Request.Headers["Stripe-Signature"])) return BadRequest(new { error = "Missing signature" });
        var json = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
        Event ev;
        try
        {
            ev = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], O.StripeWebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return BadRequest(new { error = "Invalid signature" });
        }

        if (ev.Type == "checkout.session.completed" && ev.Data.Object is Session s
            && s.PaymentStatus == "paid" && s.Metadata?.GetValueOrDefault("purpose") == StripeDonationCheckout.Purpose
            && !await db.Donations.AnyAsync(d => d.StripeSessionId == s.Id))
        {
            db.Donations.Add(new Donation { StripeSessionId = s.Id, AmountCents = s.AmountTotal ?? 0, Currency = s.Currency ?? O.Currency });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { /* a concurrent delivery of the same event inserted it first */ }
        }
        return Ok();
    }
}
