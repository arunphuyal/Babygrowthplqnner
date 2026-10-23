using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

[Authorize, Route("api/children")]
public class ChildrenController(AppDb db, PredictionService predictions) : ApiBase
{
    private const int FreeChildLimit = 1;
    private static ChildDto ToDto(Child c) => new(c.Id, c.Name, c.BirthDate, c.TrainingStartDate, c.Notes);
    private Task<Child?> Owned(Guid id) => db.Children.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == UserId);

    [HttpGet]
    public async Task<List<ChildDto>> List() =>
        (await db.Children.Where(c => c.OwnerId == UserId).OrderBy(c => c.CreatedAt).ToListAsync()).Select(ToDto).ToList();

    [HttpPost]
    public async Task<ActionResult<ChildDto>> Create(ChildRequest req)
    {
        var user = await db.Users.FindAsync(UserId);
        var count = await db.Children.CountAsync(c => c.OwnerId == UserId);
        if (user!.Plan == Plan.Free && count >= FreeChildLimit)
            return StatusCode(402, new { error = "Free plan supports one child. Upgrade to add more." });
        var child = new Child { OwnerId = UserId, Name = req.Name.Trim(), BirthDate = req.BirthDate, TrainingStartDate = req.TrainingStartDate, Notes = req.Notes };
        db.Children.Add(child);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = child.Id }, ToDto(child));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildDto>> Get(Guid id) => await Owned(id) is { } c ? ToDto(c) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChildDto>> Update(Guid id, ChildRequest req)
    {
        var c = await Owned(id);
        if (c is null) return NotFound();
        c.Name = req.Name.Trim(); c.BirthDate = req.BirthDate; c.TrainingStartDate = req.TrainingStartDate; c.Notes = req.Notes;
        await db.SaveChangesAsync();
        return ToDto(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await Owned(id);
        if (c is null) return NotFound();
        db.Children.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Logs ----
    private static LogDto ToDto(LogEntry l) => new(l.Id, l.ChildId, l.Type, l.OccurredAt, l.InPotty, l.Amount, l.Notes,
        l.Size, l.StoolType, l.Colour, l.Category, string.IsNullOrEmpty(l.Tags) ? [] : l.Tags.Split(','));

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
        if (await Owned(id) is null) return NotFound();
        var q = db.Logs.Where(l => l.ChildId == id);
        if (from is not null) q = q.Where(l => l.OccurredAt >= from);
        if (to is not null) q = q.Where(l => l.OccurredAt <= to);
        if (type is not null) q = q.Where(l => l.Type == type);
        var rows = await q.OrderByDescending(l => l.OccurredAt).Take(Math.Clamp(limit, 1, 1000)).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    [HttpPost("{id:guid}/logs")]
    public async Task<ActionResult<LogDto>> AddLog(Guid id, LogRequest req)
    {
        if (await Owned(id) is null) return NotFound();
        if (ValidateLog(req, out var tags) is { } error) return BadRequest(new { error });
        var log = new LogEntry
        {
            ChildId = id, Type = req.Type, OccurredAt = (req.OccurredAt ?? DateTime.UtcNow).ToUniversalTime(),
            InPotty = req.InPotty, Amount = req.Amount, Notes = req.Notes,
            Size = req.Size, StoolType = req.StoolType, Colour = req.Colour, Category = req.Category?.Trim(), Tags = tags
        };
        db.Logs.Add(log);
        await db.SaveChangesAsync();
        return Created($"api/children/{id}/logs/{log.Id}", ToDto(log));
    }

    [HttpDelete("{id:guid}/logs/{logId:guid}")]
    public async Task<IActionResult> DeleteLog(Guid id, Guid logId)
    {
        if (await Owned(id) is null) return NotFound();
        var log = await db.Logs.FirstOrDefaultAsync(l => l.Id == logId && l.ChildId == id);
        if (log is null) return NotFound();
        db.Logs.Remove(log);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/insights")]
    public async Task<ActionResult<InsightsDto>> Insights(Guid id, int days = 7)
    {
        if (await Owned(id) is null) return NotFound();
        days = Math.Clamp(days, 1, 90);
        var now = DateTime.UtcNow;
        var logs = await db.Logs.Where(l => l.ChildId == id && l.OccurredAt >= now.AddDays(-days)).ToListAsync();
        var (best, _) = predictions.Predict(new PredictionContext(logs, now));
        return InsightsCalculator.Compute(logs, days, now, best);
    }

    [HttpGet("{id:guid}/prediction")]
    public async Task<ActionResult<PredictionResponse>> Prediction(Guid id)
    {
        if (await Owned(id) is null) return NotFound();
        var now = DateTime.UtcNow;
        var logs = await db.Logs.Where(l => l.ChildId == id && l.OccurredAt >= now.AddDays(-14)).ToListAsync();
        var (best, alts) = predictions.Predict(new PredictionContext(logs, now));
        static PredictionDto Dto(Prediction p) => new(p.NextLikelyAt, p.Confidence, p.Model, p.Reason);
        return new PredictionResponse(best is null ? null : Dto(best), alts.Select(Dto).ToList());
    }
}
