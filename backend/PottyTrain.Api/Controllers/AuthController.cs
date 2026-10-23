using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Data;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Dtos;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Controllers;

[Route("api/auth")]
public class AuthController(AppDb db, TokenService tokens, PasswordService passwords) : ApiBase
{
    internal static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.Role, u.Plan, u.IsActive, u.CreatedAt, u.LastLoginAt);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(new { error = "Email already registered" });
        var user = new User { Email = email, DisplayName = req.DisplayName.Trim(), PasswordHash = passwords.Hash(req.Password), LastLoginAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var (token, exp) = tokens.Create(user);
        return Ok(new AuthResponse(token, exp, ToDto(user)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, req.Password))
            return Unauthorized(new { error = "Invalid credentials" });
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var (token, exp) = tokens.Create(user);
        return Ok(new AuthResponse(token, exp, ToDto(user)));
    }

    [Authorize, HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await db.Users.FindAsync(UserId);
        return user is null ? NotFound() : ToDto(user);
    }
}
