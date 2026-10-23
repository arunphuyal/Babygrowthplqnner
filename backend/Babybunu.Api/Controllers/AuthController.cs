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
    IOptions<JwtOptions> jwt, IConfiguration config) : ApiBase
{
    internal static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role, u.EffectivePlan, u.PlanExpiresAt, u.IsActive, u.CreatedAt, u.LastLoginAt, u.Language);

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

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var emailAddr = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == emailAddr)) return Conflict(new { error = "Email already registered" });
        var user = new User { Email = emailAddr, DisplayName = req.DisplayName.Trim(), PasswordHash = passwords.Hash(req.Password), LastLoginAt = DateTime.UtcNow, Language = req.Language ?? "en" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
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
        await db.PasswordResetTokens.Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, DateTime.UtcNow));
        await db.SaveChangesAsync();
        await RevokeAll(user.Id); // sign out every device
        return NoContent();
    }

    [Authorize, HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await db.Users.FindAsync(UserId);
        return user is null ? NotFound() : ToDto(user);
    }
}
