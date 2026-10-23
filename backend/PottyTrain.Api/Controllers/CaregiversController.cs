using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

/// <summary>Share a child with other caregivers (partner, grandparent, nanny). Free while Features:AllFree is on; otherwise needs the Family plan.</summary>
[Authorize, Route("api")]
public class CaregiversController(AppDb db, Entitlements entitlements) : ApiBase
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    private Task<Child?> OwnedChild(Guid id) => db.Children.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == UserId);

    [HttpPost("children/{id:guid}/invites")]
    public async Task<ActionResult<InviteResponse>> CreateInvite(Guid id, InviteRequest req)
    {
        if (await OwnedChild(id) is null) return NotFound();
        if (!entitlements.CanShare((await db.Users.FindAsync(UserId))!))
            return StatusCode(402, new { error = "Sharing with caregivers requires the Family plan." });
        var code = Secrets.NewToken(9); // 12 url-safe chars; single use, 7 days, only the hash is stored
        var invite = new ChildInvite { ChildId = id, CodeHash = Secrets.Hash(code), CanEdit = req.CanEdit, ExpiresAt = DateTime.UtcNow.Add(InviteLifetime) };
        db.Invites.Add(invite);
        await db.SaveChangesAsync();
        return new InviteResponse(code, invite.ExpiresAt, invite.CanEdit);
    }

    [HttpPost("invites/accept"), EnableRateLimiting("auth")]
    public async Task<ActionResult<ChildDto>> Accept(AcceptInviteRequest req)
    {
        var hash = Secrets.Hash(req.Code.Trim());
        var invite = await db.Invites.Include(i => i.Child).FirstOrDefaultAsync(i => i.CodeHash == hash);
        if (invite is null || invite.UsedAt is not null || invite.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "This invite is invalid or has expired." });
        var child = invite.Child!;
        if (child.OwnerId == UserId) return BadRequest(new { error = "You already own this child." });

        if (!await db.Caregivers.AnyAsync(g => g.ChildId == child.Id && g.UserId == UserId))
            db.Caregivers.Add(new ChildCaregiver { ChildId = child.Id, UserId = UserId, CanEdit = invite.CanEdit });
        invite.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new ChildDto(child.Id, child.Name, child.BirthDate, child.TrainingStartDate, child.Notes, child.TimeZoneId, false, invite.CanEdit);
    }

    [HttpGet("children/{id:guid}/caregivers")]
    public async Task<ActionResult<List<CaregiverDto>>> List(Guid id)
    {
        if (await OwnedChild(id) is null) return NotFound();
        return await db.Caregivers.Where(g => g.ChildId == id).Include(g => g.User)
            .Select(g => new CaregiverDto(g.UserId, g.User!.DisplayName, g.User.Email, g.CanEdit)).ToListAsync();
    }

    /// <summary>The owner can remove any caregiver; a caregiver can remove themselves (userId = their own id).</summary>
    [HttpDelete("children/{id:guid}/caregivers/{userId:guid}")]
    public async Task<IActionResult> Remove(Guid id, Guid userId)
    {
        var isOwner = await OwnedChild(id) is not null;
        if (!isOwner && userId != UserId) return NotFound();
        var removed = await db.Caregivers.Where(g => g.ChildId == id && g.UserId == userId).ExecuteDeleteAsync();
        return removed == 0 ? NotFound() : NoContent();
    }
}
