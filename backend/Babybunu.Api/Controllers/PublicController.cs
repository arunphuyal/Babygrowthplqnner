using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Babybunu.Api.Data;
using Babybunu.Api.Domain;
using Babybunu.Api.Dtos;
using Babybunu.Api.Services;

namespace Babybunu.Api.Controllers;

[Route("api")]
public class PublicController(AppDb db, Entitlements entitlements) : ApiBase
{
    [HttpGet("plans")]
    public async Task<List<PlanOffer>> Plans() =>
        await db.PlanOffers.Where(p => p.IsActive).OrderBy(p => p.Plan).ToListAsync();

    [HttpPost("waitlist"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
    public async Task<IActionResult> Join(WaitlistRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (!await db.Waitlist.AnyAsync(w => w.Email == email))
        {
            db.Waitlist.Add(new WaitlistEntry { Email = email, Source = req.Source });
            await db.SaveChangesAsync();
        }
        return Ok(new { joined = true }); // idempotent, does not reveal membership
    }

    [HttpGet("articles")]
    public async Task<IActionResult> Articles(string lang = "en") =>
        Ok(await db.Articles.Where(a => a.IsPublished && a.Language == lang).OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Slug, a.Title, a.Summary, a.IsPremium, a.UpdatedAt }).ToListAsync());

    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> Article(string slug, string lang = "en")
    {
        var a = await db.Articles.FirstOrDefaultAsync(x => x.Slug == slug && x.Language == lang && x.IsPublished);
        if (a is null) return NotFound();
        var unlocked = !a.IsPremium;
        if (!unlocked && User.Identity?.IsAuthenticated == true)
            unlocked = (await db.Users.FindAsync(UserId)) is { } u && entitlements.IsPremium(u);
        return Ok(new { a.Slug, a.Language, a.Title, a.Summary, a.IsPremium, a.MetaTitle, a.MetaDescription, a.CreatedAt, a.UpdatedAt, Body = unlocked ? a.Body : null, Locked = !unlocked });
    }
}
