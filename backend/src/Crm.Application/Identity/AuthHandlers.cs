using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Identity;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
public record RefreshCommand(string RefreshToken) : IRequest<AuthResponse>;
public record LogoutCommand(string? RefreshToken) : IRequest<Unit>;
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Unit>;
public record GetMeQuery() : IRequest<UserProfileDto>;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc, UserProfileDto User);
public record UserProfileDto(Guid Id, string Email, string DisplayName, string? JobTitle, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class AuthHandlers :
    IRequestHandler<LoginCommand, AuthResponse>,
    IRequestHandler<RefreshCommand, AuthResponse>,
    IRequestHandler<LogoutCommand, Unit>,
    IRequestHandler<ChangePasswordCommand, Unit>,
    IRequestHandler<GetMeQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;
    private readonly IPermissionStore _permissions;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthHandlers(IApplicationDbContext db, IJwtTokenService jwt, ICurrentUser user, IDateTime clock, IAuditWriter audit, IPermissionStore permissions)
    {
        _db = db;
        _jwt = jwt;
        _user = user;
        _clock = clock;
        _audit = audit;
        _permissions = permissions;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var password = request.Password; // do not trim passwords — only normalize email
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        var roles = user.UserRoles.Select(r => r.Role.Code).ToList();
        var (access, exp) = _jwt.CreateAccessToken(user, roles);
        var refresh = _jwt.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwt.HashToken(refresh),
            ExpiresAtUtc = _clock.UtcNow.AddDays(14),
            CreatedAtUtc = _clock.UtcNow,
            CreatedByIp = _user.IpAddress
        });
        _audit.Add("User", user.Id, AuditAction.UserLoggedIn, "User logged in");
        await _db.SaveChangesAsync(ct);
        return new AuthResponse(access, refresh, exp, Profile(user, roles));
    }

    public async Task<AuthResponse> Handle(RefreshCommand request, CancellationToken ct)
    {
        var hash = _jwt.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!stored.IsActive)
            throw new UnauthorizedException("Refresh token is revoked or expired.");

        var roles = stored.User.UserRoles.Select(r => r.Role.Code).ToList();
        var (access, exp) = _jwt.CreateAccessToken(stored.User, roles);
        var next = _jwt.CreateRefreshToken();
        stored.RevokedAtUtc = _clock.UtcNow;
        stored.ReplacedByTokenHash = _jwt.HashToken(next);
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = stored.ReplacedByTokenHash,
            ExpiresAtUtc = _clock.UtcNow.AddDays(14),
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return new AuthResponse(access, next, exp, Profile(stored.User, roles));
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _jwt.HashToken(request.RefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is not null) stored.RevokedAtUtc = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == _user.UserId, ct);
        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new BusinessRuleException("Current password is incorrect.");
        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<UserProfileDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == _user.UserId, ct);
        return Profile(user, user.UserRoles.Select(r => r.Role.Code).ToList());
    }

    private UserProfileDto Profile(User user, IReadOnlyList<string> roles)
    {
        var matrix = _permissions.GetMatrix();
        var perms = AuthorizationPolicies.PolicyRoles.Keys
            .Where(policy =>
            {
                var allowed = matrix.TryGetValue(policy, out var list)
                    ? list
                    : AuthorizationPolicies.PolicyRoles[policy];
                return allowed.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
            })
            .ToList();
        return new UserProfileDto(user.Id, user.Email, user.DisplayName, user.JobTitle, roles, perms);
    }
}
