using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

[Authorize, Route("api/children")]
public class ChildrenController(AppDb db) : ApiBase
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
    private static LogDto ToDto(LogEntry l) => new(l.Id, l.ChildId, l.Type, l.OccurredAt, l.InPotty, l.Amount, l.Notes);

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
        var log = new LogEntry
        {
            ChildId = id, Type = req.Type, OccurredAt = (req.OccurredAt ?? DateTime.UtcNow).ToUniversalTime(),
            InPotty = req.InPotty, Amount = req.Amount, Notes = req.Notes
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
        return InsightsCalculator.Compute(logs, days, now);
    }
}
