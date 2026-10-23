using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "PottyTrain";
    public string Audience { get; set; } = "PottyTrain";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 30;
    public int RefreshDays { get; set; } = 30;
}

public class TokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> opts)
{
    private readonly JwtOptions _o = opts.Value;

    public (string Token, DateTime ExpiresAt) Create(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_o.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("plan", user.EffectivePlan.ToString())
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}

public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    public string Hash(string password) => _hasher.HashPassword(null!, password);
    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}

/// <summary>Opaque random secrets (refresh, reset and invite tokens). Only the SHA-256 hash is ever stored.</summary>
public static class Secrets
{
    public static string NewToken(int bytes = 32) =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(bytes)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

public class EmailOptions
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@pottytrain.local";
}

/// <summary>Sends via SMTP when Email:SmtpHost is configured; otherwise logs the message (development).</summary>
public class SmtpEmailSender(Microsoft.Extensions.Options.IOptions<EmailOptions> opts, ILogger<SmtpEmailSender> log) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var o = opts.Value;
        if (string.IsNullOrEmpty(o.SmtpHost))
        {
            log.LogWarning("Email not configured; would send to {To}: {Subject}\n{Body}", to, subject, body);
            return;
        }
        using var client = new System.Net.Mail.SmtpClient(o.SmtpHost, o.SmtpPort) { EnableSsl = true };
        if (!string.IsNullOrEmpty(o.User)) client.Credentials = new System.Net.NetworkCredential(o.User, o.Password);
        await client.SendMailAsync(new System.Net.Mail.MailMessage(o.From, to, subject, body));
    }
}
