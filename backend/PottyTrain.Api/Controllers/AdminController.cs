using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;

namespace PottyTrain.Api.Controllers;

[Authorize(Roles = nameof(UserRole.Admin)), Route("api/admin")]
public class AdminController(AppDb db) : ApiBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.UtcNow;
        var users = db.Users.Where(u => u.Role == UserRole.Parent);
        var signups = await users.Where(u => u.CreatedAt >= now.AddDays(-30))
            .GroupBy(u => u.CreatedAt.Date).Select(g => new { date = g.Key, count = g.Count() }).ToListAsync();
        var paying = await users.CountAsync(u => u.Plan != Plan.Free);
        var offers = await db.PlanOffers.ToListAsync();
        var mrr = 0m;
        foreach (var g in await users.GroupBy(u => u.Plan).Select(g => new { plan = g.Key, n = g.Count() }).ToListAsync())
            mrr += g.n * (offers.FirstOrDefault(o => o.Plan == g.plan)?.MonthlyPrice ?? 0);
        return Ok(new
        {
            totalUsers = await users.CountAsync(),
            activeLast7Days = await users.CountAsync(u => u.LastLoginAt >= now.AddDays(-7)),
            payingUsers = paying,
            estimatedMrr = mrr,
            totalChildren = await db.Children.CountAsync(),
            logsLast7Days = await db.Logs.CountAsync(l => l.OccurredAt >= now.AddDays(-7)),
            activeChildren7d = await db.Logs.Where(l => l.OccurredAt >= now.AddDays(-7)).Select(l => l.ChildId).Distinct().CountAsync(),
            sharedChildren = await db.Caregivers.Select(c => c.ChildId).Distinct().CountAsync(),
            logsByType30d = (await db.Logs.Where(l => l.OccurredAt >= now.AddDays(-30)).GroupBy(l => l.Type)
                .Select(g => new { type = g.Key, count = g.Count() }).ToListAsync()).Select(x => new { type = x.type.ToString(), x.count }),
            waitlist = await db.Waitlist.CountAsync(),
            donationsCount = await db.Donations.CountAsync(),
            donationsTotalCents = await db.Donations.SumAsync(d => (long?)d.AmountCents) ?? 0,
            signupsLast30Days = signups.OrderBy(s => s.date)
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? q, int page = 1, int pageSize = 25)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.Email.Contains(q) || u.DisplayName.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(Math.Clamp(pageSize, 1, 100)).ToListAsync();
        return Ok(new { total, items = items.Select(AuthController.ToDto) });
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, UpdateUserRequest req)
    {
        var u = await db.Users.FindAsync(id);
        if (u is null) return NotFound();
        if (id == UserId && (req.IsActive == false || req.Role is { } r && r != UserRole.Admin))
            return BadRequest(new { error = "You cannot demote or deactivate yourself" });
        if (req.Plan is { } p) { u.Plan = p; u.PlanExpiresAt = null; } // admin grants do not expire
        if (req.IsActive is { } a) u.IsActive = a;
        if (req.Role is { } role) u.Role = role;
        await db.SaveChangesAsync();
        return AuthController.ToDto(u);
    }

    [HttpGet("donations")]
    public async Task<IActionResult> Donations()
    {
        var items = await db.Donations.OrderByDescending(d => d.CreatedAt).Take(200).ToListAsync();
        return Ok(new
        {
            count = await db.Donations.CountAsync(),
            totalCents = await db.Donations.SumAsync(d => (long?)d.AmountCents) ?? 0,
            items = items.Select(d => new { d.Id, d.AmountCents, d.Currency, d.CreatedAt })
        });
    }

    [HttpGet("waitlist")]
    public async Task<List<WaitlistEntry>> Waitlist() => await db.Waitlist.OrderByDescending(w => w.CreatedAt).ToListAsync();

    // ---- Articles ----
    [HttpGet("articles")]
    public async Task<List<Article>> Articles() => await db.Articles.OrderByDescending(a => a.UpdatedAt).ToListAsync();

    [HttpPost("articles")]
    public async Task<ActionResult<Article>> CreateArticle(ArticleRequest r)
    {
        var lang = r.Language ?? "en";
        if (await db.Articles.AnyAsync(a => a.Slug == r.Slug && a.Language == lang)) return Conflict(new { error = "Slug already exists for this language" });
        var a = new Article { Slug = r.Slug, Language = lang, Title = r.Title, Summary = r.Summary, Body = r.Body, IsPremium = r.IsPremium, IsPublished = r.IsPublished,
            MetaTitle = r.MetaTitle, MetaDescription = r.MetaDescription, FocusKeyword = r.FocusKeyword };
        db.Articles.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    [HttpPut("articles/{id:guid}")]
    public async Task<ActionResult<Article>> UpdateArticle(Guid id, ArticleRequest r)
    {
        var a = await db.Articles.FindAsync(id);
        if (a is null) return NotFound();
        var lang = r.Language ?? a.Language;
        if (await db.Articles.AnyAsync(x => x.Slug == r.Slug && x.Language == lang && x.Id != id)) return Conflict(new { error = "Slug already exists for this language" });
        a.Slug = r.Slug; a.Language = lang; a.Title = r.Title; a.Summary = r.Summary; a.Body = r.Body;
        a.IsPremium = r.IsPremium; a.IsPublished = r.IsPublished; a.UpdatedAt = DateTime.UtcNow;
        a.MetaTitle = r.MetaTitle; a.MetaDescription = r.MetaDescription; a.FocusKeyword = r.FocusKeyword;
        await db.SaveChangesAsync();
        return a;
    }

    [HttpDelete("articles/{id:guid}")]
    public async Task<IActionResult> DeleteArticle(Guid id)
    {
        var a = await db.Articles.FindAsync(id);
        if (a is null) return NotFound();
        db.Articles.Remove(a);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Plans ----
    [HttpGet("plans")]
    public async Task<List<PlanOffer>> Plans() => await db.PlanOffers.OrderBy(p => p.Plan).ToListAsync();

    [HttpPut("plans/{id:guid}")]
    public async Task<ActionResult<PlanOffer>> UpdatePlan(Guid id, PlanOfferRequest r)
    {
        var p = await db.PlanOffers.FindAsync(id);
        if (p is null) return NotFound();
        p.Name = r.Name; p.MonthlyPrice = r.MonthlyPrice; p.YearlyPrice = r.YearlyPrice; p.Features = r.Features; p.IsActive = r.IsActive;
        await db.SaveChangesAsync();
        return p;
    }
}
