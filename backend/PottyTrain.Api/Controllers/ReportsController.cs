using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

/// <summary>Reports for daycare, a paediatrician or your own records: JSON, a print-friendly HTML page, or a CSV of entries, plus expiring share links.</summary>
public class ReportsController(AppDb db) : ApiBase
{
    private async Task<(Child? Child, bool IsOwner)> Find(Guid id)
    {
        var c = await db.Children.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return (null, false);
        if (c.OwnerId == UserId) return (c, true);
        return await db.Caregivers.AnyAsync(g => g.ChildId == id && g.UserId == UserId) ? (c, false) : (null, false);
    }

    private async Task<(ReportDto Report, List<LogEntry> Logs, Dictionary<Guid, string> Names)> Load(Child child, DateOnly from, DateOnly to, bool includeLogs)
    {
        var tz = InsightsCalculator.ResolveZone(child.TimeZoneId);
        var (start, end) = ReportBuilder.UtcRange(from, to, tz);
        var logs = await db.Logs.Where(l => l.ChildId == child.Id && l.OccurredAt >= start && l.OccurredAt < end).ToListAsync();
        var ids = logs.Where(l => l.LoggedByUserId != null).Select(l => l.LoggedByUserId!.Value).Distinct().ToList();
        var names = await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName);
        return (ReportBuilder.Build(child, logs, from, to, DateTime.UtcNow, includeLogs, names), logs, names);
    }

    [Authorize, HttpGet("api/children/{id:guid}/report")]
    public async Task<IActionResult> Get(Guid id, DateOnly? from, DateOnly? to, string format = "json", bool includeLogs = false)
    {
        var (child, _) = await Find(id);
        if (child is null) return NotFound();
        var tz = InsightsCalculator.ResolveZone(child.TimeZoneId);
        var today = DateOnly.FromDateTime(InsightsCalculator.Local(DateTime.UtcNow, tz));
        var (f, t) = (from ?? today.AddDays(-6), to ?? today);
        if (ReportBuilder.ValidateRange(f, t) is { } error) return BadRequest(new { error });

        var (report, logs, names) = await Load(child, f, t, includeLogs || format == "csv");
        return format.ToLowerInvariant() switch
        {
            "json" => Ok(report),
            "html" => Content(ReportRenderer.Html(report), "text/html; charset=utf-8"),
            "csv" => File(System.Text.Encoding.UTF8.GetBytes(ReportRenderer.Csv(logs, tz, names)), "text/csv; charset=utf-8", $"pottytrain-{f:yyyyMMdd}-{t:yyyyMMdd}.csv"),
            _ => BadRequest(new { error = "format must be json, html or csv." })
        };
    }

    // ---- Share links (owner only) ----
    [Authorize, HttpPost("api/children/{id:guid}/report-links")]
    public async Task<ActionResult<ReportLinkDto>> CreateLink(Guid id, ReportLinkRequest req)
    {
        if (await Find(id) is not { Child: not null, IsOwner: true }) return NotFound();
        if (ReportBuilder.ValidateRange(req.From, req.To) is { } error) return BadRequest(new { error });
        if (req.ExpiresInDays is < 1 or > 30) return BadRequest(new { error = "ExpiresInDays must be between 1 and 30." });

        var token = Secrets.NewToken();
        var share = new ReportShare
        {
            ChildId = id, TokenHash = Secrets.Hash(token), From = req.From, To = req.To, IncludeLogs = req.IncludeLogs,
            ExpiresAt = DateTime.UtcNow.AddDays(req.ExpiresInDays)
        };
        db.ReportShares.Add(share);
        await db.SaveChangesAsync();
        return new ReportLinkDto(share.Id, token, $"/r/{token}", share.From, share.To, share.IncludeLogs, share.ExpiresAt);
    }

    [Authorize, HttpGet("api/children/{id:guid}/report-links")]
    public async Task<ActionResult<List<ReportLinkSummary>>> Links(Guid id)
    {
        if (await Find(id) is not { Child: not null, IsOwner: true }) return NotFound();
        return await db.ReportShares.Where(r => r.ChildId == id && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt).Select(r => new ReportLinkSummary(r.Id, r.From, r.To, r.IncludeLogs, r.ExpiresAt, r.CreatedAt)).ToListAsync();
    }

    [Authorize, HttpDelete("api/children/{id:guid}/report-links/{linkId:guid}")]
    public async Task<IActionResult> Revoke(Guid id, Guid linkId)
    {
        if (await Find(id) is not { Child: not null, IsOwner: true }) return NotFound();
        var n = await db.ReportShares.Where(r => r.Id == linkId && r.ChildId == id && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, DateTime.UtcNow));
        return n == 0 ? NotFound() : NoContent();
    }

    /// <summary>Public, unguessable, expiring and revocable. Never indexed, cached or leaked through the Referer header.</summary>
    [AllowAnonymous, EnableRateLimiting("public"), HttpGet("r/{token}")]
    public async Task<IActionResult> Shared(string token)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'";

        var hash = Secrets.Hash(token);
        var share = await db.ReportShares.Include(r => r.Child).FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (share is null || share.RevokedAt is not null || share.ExpiresAt < DateTime.UtcNow)
            return NotFound(); // same answer for unknown, revoked and expired
        var (report, _, _) = await Load(share.Child!, share.From, share.To, share.IncludeLogs);
        return Content(ReportRenderer.Html(report), "text/html; charset=utf-8");
    }
}
