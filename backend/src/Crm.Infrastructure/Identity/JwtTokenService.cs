using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Crm.Application.Abstractions;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Crm.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly IDateTime _clock;

    public JwtTokenService(IConfiguration config, IDateTime clock)
    {
        _config = config;
        _clock = clock;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var jwt = _config.GetSection("Authentication:Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var expires = _clock.UtcNow.AddMinutes(int.Parse(jwt["AccessTokenMinutes"] ?? "60"));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}

public sealed class CurrentUser : ICurrentUser
{
    public Guid? UserId { get; }
    public string? Email { get; }
    public string? DisplayName { get; }
    public IReadOnlyList<string> Roles { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdmin => HasRole(RoleCodes.Admin);

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        var principal = http?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(sub, out var id)) UserId = id;
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);
            DisplayName = principal.FindFirstValue(ClaimTypes.Name);
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }
        else
        {
            Roles = [];
        }
        IpAddress = http?.Connection.RemoteIpAddress?.ToString();
        UserAgent = http?.Request.Headers.UserAgent.ToString();
    }

    public bool HasRole(string roleCode) => Roles.Contains(roleCode, StringComparer.OrdinalIgnoreCase);
}

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
