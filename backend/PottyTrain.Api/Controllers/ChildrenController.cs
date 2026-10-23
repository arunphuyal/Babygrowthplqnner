using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

[Authorize, Route("api/children")]
public class ChildrenController(AppDb db, PredictionService predictions, Entitlements entitlements) : ApiBase
{
    private const int FreeInsightDays = 7;

    private static ChildDto ToDto(Child c, bool isOwner, bool canEdit) =>
        new(c.Id, c.Name, c.BirthDate, c.TrainingStartDate, c.Notes, c.TimeZoneId, isOwner, canEdit);

    private record Access(Child Child, bool IsOwner, bool CanEdit);

    /// <summary>The child if the current user owns it or is a caregiver for it; otherwise null (reported as 404).</summary>
    private async Task<Access?> Find(Guid id)
    {
        var c = await db.Children.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return null;
        if (c.OwnerId == UserId) return new(c, true, true);
        var cg = await db.Caregivers.FirstOrDefaultAsync(x => x.ChildId == id && x.UserId == UserId);
        return cg is null ? null : new(c, false, cg.CanEdit);
    }

    private async Task<bool> OwnerIsPremium(Child c) => entitlements.IsPremium((await db.Users.FindAsync(c.OwnerId))!);

    private static string? ValidateZone(string? id)
    {
        if (id is null) return null;
        try { TimeZoneInfo.FindSystemTimeZoneById(id); return null; }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { return "Unknown TimeZoneId. Use an IANA name such as \"Europe/London\"."; }
    }

    [HttpGet]
    public async Task<List<ChildDto>> List()
    {
        var owned = await db.Children.Where(c => c.OwnerId == UserId).OrderBy(c => c.CreatedAt).ToListAsync();
        var shared = await db.Caregivers.Where(g => g.UserId == UserId).Include(g => g.Child).OrderBy(g => g.CreatedAt).ToListAsync();
        return owned.Select(c => ToDto(c, true, true)).Concat(shared.Select(g => ToDto(g.Child!, false, g.CanEdit))).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<ChildDto>> Create(ChildRequest req)
    {
        if (ValidateZone(req.TimeZoneId) is { } zoneError) return BadRequest(new { error = zoneError });
        var user = await db.Users.FindAsync(UserId);
        var count = await db.Children.CountAsync(c => c.OwnerId == UserId);
        if (count >= entitlements.MaxChildren(user!))
            return StatusCode(402, new { error = "You have reached the maximum number of children for your plan." });
        var child = new Child
        {
            OwnerId = UserId, Name = req.Name.Trim(), BirthDate = req.BirthDate, TrainingStartDate = req.TrainingStartDate,
            Notes = req.Notes, TimeZoneId = req.TimeZoneId ?? "UTC"
        };
        db.Children.Add(child);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = child.Id }, ToDto(child, true, true));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildDto>> Get(Guid id) => await Find(id) is { } a ? ToDto(a.Child, a.IsOwner, a.CanEdit) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChildDto>> Update(Guid id, ChildRequest req)
    {
        if (await Find(id) is not { IsOwner: true } a) return NotFound();
        if (ValidateZone(req.TimeZoneId) is { } zoneError) return BadRequest(new { error = zoneError });
        var c = a.Child;
        c.Name = req.Name.Trim(); c.BirthDate = req.BirthDate; c.TrainingStartDate = req.TrainingStartDate; c.Notes = req.Notes;
        if (req.TimeZoneId is not null) c.TimeZoneId = req.TimeZoneId;
        await db.SaveChangesAsync();
        return ToDto(c, true, true);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await Find(id) is not { IsOwner: true } a) return NotFound();
        db.Children.Remove(a.Child);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Logs ----
    private static LogDto ToDto(LogEntry l, Dictionary<Guid, string> names) => new(l.Id, l.ChildId, l.Type, l.OccurredAt, l.InPotty, l.Amount, l.Notes,
        l.Size, l.StoolType, l.Colour, l.Category, string.IsNullOrEmpty(l.Tags) ? [] : l.Tags.Split(','),
        l.LoggedByUserId is { } u && names.TryGetValue(u, out var n) ? n : null);

    private static readonly System.Text.RegularExpressions.Regex Slug = new("^[a-z0-9-]{1,20}$");

    /// <summary>Returns an error message, or null if the type-specific fields are consistent.</summary>
    private static string? ValidateLog(LogRequest r, out string? tags)
    {
        tags = null;
        var output = r.Type is LogType.Pee or LogType.Poop;
        var intake = r.Type is LogType.Fluid or LogType.Food;
        if (r.Amount is { } a && (a < 1 || a > 2000)) return "Amount must be between 1 and 2000 ml.";
        if (r.Amount is not null && r.Type is not (LogType.Fluid or LogType.Pee)) return "Amount (ml) only applies to Fluid and Pee logs.";
        if (r.StoolType is { } st && (st < 1 || st > 7)) return "StoolType must be 1-7 (Bristol scale).";
        if (r.StoolType is not null && r.Type != LogType.Poop) return "StoolType only applies to Poop logs.";
        if (r.Colour is not null && !output) return "Colour only applies to Pee and Poop logs.";
        if (r.Size is not null && !(output || intake)) return "Size only applies to Pee, Poop, Fluid and Food logs.";
        if (r.Category is { } c && (c.Length > 30 || !intake)) return "Category must be at most 30 characters and only applies to Fluid and Food logs.";
        if (r.Tags is { Length: > 0 } t)
        {
            if (!intake) return "Tags only apply to Fluid and Food logs.";
            var clean = t.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0).Distinct().ToList();
            if (clean.Count > 8 || clean.Any(x => !Slug.IsMatch(x))) return "Up to 8 tags of letters, digits or '-' (max 20 chars each).";
            tags = string.Join(',', clean);
        }
        return null;
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<List<LogDto>>> Logs(Guid id, DateTime? from, DateTime? to, LogType? type, int limit = 200)
    {
        if (await Find(id) is null) return NotFound();
        var q = db.Logs.Where(l => l.ChildId == id);
        if (from is not null) q = q.Where(l => l.OccurredAt >= from);
        if (to is not null) q = q.Where(l => l.OccurredAt <= to);
        if (type is not null) q = q.Where(l => l.Type == type);
        var rows = await q.OrderByDescending(l => l.OccurredAt).Take(Math.Clamp(limit, 1, 1000)).ToListAsync();
        var ids = rows.Where(r => r.LoggedByUserId != null).Select(r => r.LoggedByUserId!.Value).Distinct().ToList();
        var names = await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName);
        return rows.Select(r => ToDto(r, names)).ToList();
    }

    [HttpPost("{id:guid}/logs")]
    public async Task<ActionResult<LogDto>> AddLog(Guid id, LogRequest req)
    {
        if (await Find(id) is not { } a) return NotFound();
        if (!a.CanEdit) return StatusCode(403, new { error = "You have view-only access to this child." });
        if (ValidateLog(req, out var tags) is { } error) return BadRequest(new { error });
        var log = new LogEntry
        {
            ChildId = id, Type = req.Type, OccurredAt = (req.OccurredAt ?? DateTime.UtcNow).ToUniversalTime(),
            InPotty = req.InPotty, Amount = req.Amount, Notes = req.Notes, LoggedByUserId = UserId,
            Size = req.Size, StoolType = req.StoolType, Colour = req.Colour, Category = req.Category?.Trim(), Tags = tags
        };
        db.Logs.Add(log);
        await db.SaveChangesAsync();
        var name = (await db.Users.FindAsync(UserId))!.DisplayName;
        return Created($"api/children/{id}/logs/{log.Id}", ToDto(log, new() { [UserId] = name }));
    }

    [HttpDelete("{id:guid}/logs/{logId:guid}")]
    public async Task<IActionResult> DeleteLog(Guid id, Guid logId)
    {
        if (await Find(id) is not { } a) return NotFound();
        if (!a.CanEdit) return StatusCode(403, new { error = "You have view-only access to this child." });
        var log = await db.Logs.FirstOrDefaultAsync(l => l.Id == logId && l.ChildId == id);
        if (log is null) return NotFound();
        db.Logs.Remove(log);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Insights & predictions (paid features follow the child's owner, so a Family plan benefits every caregiver) ----
    [HttpGet("{id:guid}/insights")]
    public async Task<ActionResult<InsightsDto>> Insights(Guid id, int days = 7)
    {
        if (await Find(id) is not { } a) return NotFound();
        var premium = await OwnerIsPremium(a.Child);
        days = Math.Clamp(days, 1, premium ? 90 : FreeInsightDays);
        var now = DateTime.UtcNow;
        var logs = await db.Logs.Where(l => l.ChildId == id && l.OccurredAt >= now.AddDays(-days)).ToListAsync();
        var (best, _) = predictions.Predict(new PredictionContext(logs, now, premium));
        return InsightsCalculator.Compute(logs, days, now, best, InsightsCalculator.ResolveZone(a.Child.TimeZoneId), includeCorrelations: premium);
    }

    [HttpGet("{id:guid}/prediction")]
    public async Task<ActionResult<PredictionResponse>> Prediction(Guid id)
    {
        if (await Find(id) is not { } a) return NotFound();
        var now = DateTime.UtcNow;
        var logs = await db.Logs.Where(l => l.ChildId == id && l.OccurredAt >= now.AddDays(-14)).ToListAsync();
        var (best, alts) = predictions.Predict(new PredictionContext(logs, now, await OwnerIsPremium(a.Child)));
        static PredictionDto Dto(Prediction p) => new(p.NextLikelyAt, p.Confidence, p.Model, p.Reason);
        return new PredictionResponse(best is null ? null : Dto(best), alts.Select(Dto).ToList());
    }
}
