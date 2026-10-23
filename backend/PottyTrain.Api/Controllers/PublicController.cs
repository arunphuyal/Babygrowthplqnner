using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;

namespace PottyTrain.Api.Controllers;

[Route("api")]
public class PublicController(AppDb db) : ApiBase
{
    [HttpGet("plans")]
    public async Task<List<PlanOffer>> Plans() =>
        await db.PlanOffers.Where(p => p.IsActive).OrderBy(p => p.Plan).ToListAsync();

    [HttpPost("waitlist")]
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
    public async Task<IActionResult> Articles() =>
        Ok(await db.Articles.Where(a => a.IsPublished).OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Slug, a.Title, a.Summary, a.IsPremium }).ToListAsync());

    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> Article(string slug)
    {
        var a = await db.Articles.FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);
        if (a is null) return NotFound();
        var unlocked = !a.IsPremium;
        if (!unlocked && User.Identity?.IsAuthenticated == true)
            unlocked = (await db.Users.FindAsync(UserId))?.EffectivePlan is Plan.Premium or Plan.Family;
        return Ok(new { a.Slug, a.Title, a.Summary, a.IsPremium, Body = unlocked ? a.Body : null, Locked = !unlocked });
    }
}
