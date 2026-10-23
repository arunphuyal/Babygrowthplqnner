using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Babybunu.Api.Data;
using Babybunu.Api.Domain;
using Babybunu.Api.Dtos;
using Babybunu.Api.Services;

namespace Babybunu.Api.Controllers;

[Route("api/auth"), EnableRateLimiting("auth")]
public class AuthController(AppDb db, TokenService tokens, PasswordService passwords, IEmailSender email,
    IOptions<JwtOptions> jwt, IConfiguration config, ILogger<AuthController> log) : ApiBase
{
    internal static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role, u.EffectivePlan, u.PlanExpiresAt, u.IsActive, u.CreatedAt, u.LastLoginAt, u.Language, u.EmailVerifiedAt != null);

    private async Task<AuthResponse> Issue(User user)
    {
        var (access, exp) = tokens.Create(user);
        var refresh = Secrets.NewToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Secrets.Hash(refresh), ExpiresAt = DateTime.UtcNow.AddDays(jwt.Value.RefreshDays) });
        await db.SaveChangesAsync();
        return new AuthResponse(access, exp, refresh, ToDto(user));
    }

    private Task RevokeAll(Guid userId) =>
        db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));

    /// <summary>Emails a single-use link valid for 48 hours. A mail failure must never block sign-up, so it is logged and swallowed.</summary>
    private async Task SendVerification(User user)
    {
        try
        {
            await db.EmailVerificationTokens.Where(t => t.UserId == user.Id && t.UsedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTime.UtcNow));
            var token = Secrets.NewToken();
            db.EmailVerificationTokens.Add(new EmailVerificationToken { UserId = user.Id, TokenHash = Secrets.Hash(token), ExpiresAt = DateTime.UtcNow.AddHours(48) });
            await db.SaveChangesAsync();
            var link = $"{config["App:VerifyUrl"] ?? "https://app.example.com/verify-email"}?token={token}";
            await email.SendAsync(user.Email, $"Confirm your email for {config["App:Name"] ?? "your account"}",
                $"Hi {user.DisplayName},\n\nPlease confirm your email address using this link (valid for 48 hours):\n{link}\n\nIf you didn't create an account, you can ignore this email.");
        }
        catch (Exception e)
        {
            log.LogError(e, "Could not send the verification email for user {UserId}", user.Id);
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var emailAddr = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == emailAddr)) return Conflict(new { error = "Email already registered" });
        var user = new User { Email = emailAddr, DisplayName = req.DisplayName.Trim(), PasswordHash = passwords.Hash(req.Password), LastLoginAt = DateTime.UtcNow, Language = req.Language ?? "en" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await SendVerification(user);
        return Ok(await Issue(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var emailAddr = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailAddr);
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, req.Password))
            return Unauthorized(new { error = "Invalid credentials" });
        user.LastLoginAt = DateTime.UtcNow;
        return Ok(await Issue(user));
    }

    /// <summary>Rotates the refresh token. Presenting an already-used token revokes every session of that user (token theft signal).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req)
    {
        var hash = Secrets.Hash(req.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token is null) return Unauthorized(new { error = "Invalid refresh token" });
        if (token.RevokedAt is not null)
        {
            await RevokeAll(token.UserId);
            return Unauthorized(new { error = "Refresh token reuse detected; please sign in again" });
        }
        var user = await db.Users.FindAsync(token.UserId);
        if (token.ExpiresAt < DateTime.UtcNow || user is null || !user.IsActive) return Unauthorized(new { error = "Refresh token expired" });
        token.RevokedAt = DateTime.UtcNow;
        return Ok(await Issue(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req)
    {
        var hash = Secrets.Hash(req.RefreshToken);
        await db.RefreshTokens.Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        return NoContent();
    }

    /// <summary>Always returns 200 so the endpoint cannot be used to discover which emails are registered.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        var emailAddr = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailAddr && u.IsActive);
        if (user is not null)
        {
            var token = Secrets.NewToken();
            db.PasswordResetTokens.Add(new PasswordResetToken { UserId = user.Id, TokenHash = Secrets.Hash(token), ExpiresAt = DateTime.UtcNow.AddHours(1) });
            await db.SaveChangesAsync();
            var link = $"{config["App:ResetUrl"] ?? "https://babybunu.local/reset-password"}?token={token}";
            var app = config["App:Name"] ?? "your account";
            await email.SendAsync(user.Email, $"Reset your {app} password",
                $"Hi {user.DisplayName},\n\nUse this link within 1 hour to choose a new password:\n{link}\n\nIf you didn't ask for this, you can ignore this email.");
        }
        return Ok(new { sent = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
    {
        var hash = Secrets.Hash(req.Token);
        var reset = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (reset is null || reset.UsedAt is not null || reset.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "This reset link is invalid or has expired" });
        var user = await db.Users.FindAsync(reset.UserId);
        if (user is null) return BadRequest(new { error = "This reset link is invalid or has expired" });

        user.PasswordHash = passwords.Hash(req.NewPassword);
        user.EmailVerifiedAt ??= DateTime.UtcNow; // only the mailbox owner could have used the reset link
        await db.PasswordResetTokens.Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTime.UtcNow));
        await db.SaveChangesAsync();
        await RevokeAll(user.Id); // sign out every device
        return NoContent();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest req)
    {
        var hash = Secrets.Hash(req.Token);
        var t = await db.EmailVerificationTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (t is null || t.UsedAt is not null || t.ExpiresAt < DateTime.UtcNow) return BadRequest(new { error = "This confirmation link is invalid or has expired" });
        var user = await db.Users.FindAsync(t.UserId);
        if (user is null) return BadRequest(new { error = "This confirmation link is invalid or has expired" });
        user.EmailVerifiedAt ??= DateTime.UtcNow;
        t.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize, HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification()
    {
        var user = await db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        if (user.EmailVerifiedAt is null) await SendVerification(user);
        return NoContent(); // same answer whether or not an email was needed
    }

    /// <summary>Permanently deletes the account and everything the user owns (children, logs, invites, report links). Required by the app stores.</summary>
    [Authorize, HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(DeleteAccountRequest req)
    {
        var user = await db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        if (!passwords.Verify(user.PasswordHash, req.Password)) return Unauthorized(new { error = "Password is incorrect" });
        if (user.Role == UserRole.Admin && await db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive) <= 1)
            return BadRequest(new { error = "You are the only administrator. Create another administrator first." });

        await using var tx = await db.Database.BeginTransactionAsync();
        // Entries this person added to other families' children stay with those families, but are no longer attributed to them.
        await db.Logs.Where(l => l.LoggedByUserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.LoggedByUserId, (Guid?)null));
        await db.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await db.PasswordResetTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await db.EmailVerificationTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync(); // children, logs, invites, report links and caregiver rows cascade
        await tx.CommitAsync();
        return NoContent();
    }

    /// <summary>A machine-readable copy of the user's data (GDPR/CCPA access and portability).</summary>
    [Authorize, HttpGet("me/export")]
    public async Task<IActionResult> Export()
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UserId);
        if (user is null) return NotFound();
        var owned = await db.Children.AsNoTracking().Where(c => c.OwnerId == UserId).Include(c => c.Logs).OrderBy(c => c.CreatedAt).ToListAsync();
        var shared = await db.Caregivers.AsNoTracking().Where(g => g.UserId == UserId).Include(g => g.Child).ToListAsync();
        var mine = await db.Logs.AsNoTracking().Where(l => l.LoggedByUserId == UserId && l.Child!.OwnerId != UserId).ToListAsync();
        var links = await db.ReportShares.AsNoTracking().Where(r => r.Child!.OwnerId == UserId).ToListAsync();

        object Log(LogEntry l) => new { l.Id, type = l.Type.ToString(), l.OccurredAt, l.InPotty, l.Amount, l.Size, l.StoolType, l.Colour, l.Category, tags = string.IsNullOrEmpty(l.Tags) ? [] : l.Tags.Split(','), l.Notes };
        Response.Headers["Content-Disposition"] = "attachment; filename=\"my-data.json\"";
        Response.Headers["Cache-Control"] = "no-store";
        return Ok(new
        {
            exportedAt = DateTime.UtcNow,
            account = new { user.Email, user.DisplayName, user.Language, user.CreatedAt, emailVerified = user.EmailVerifiedAt != null },
            children = owned.Select(c => new { c.Id, c.Name, c.BirthDate, c.TrainingStartDate, c.Notes, c.TimeZoneId, logs = c.Logs.OrderBy(l => l.OccurredAt).Select(Log) }),
            sharedWithYou = shared.Select(g => new
            {
                childName = g.Child!.Name, g.CanEdit,
                entriesYouLogged = mine.Where(l => l.ChildId == g.ChildId).OrderBy(l => l.OccurredAt).Select(Log)
            }),
            reportLinks = links.Select(r => new { r.From, r.To, r.IncludeLogs, r.ExpiresAt, r.CreatedAt, revoked = r.RevokedAt != null })
        });
    }

    [Authorize, HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await db.Users.FindAsync(UserId);
        return user is null ? NotFound() : ToDto(user);
    }
}
