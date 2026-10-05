using System.Collections.Concurrent;
using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Infrastructure.Security;

public sealed class PermissionStore : IPermissionStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private ConcurrentDictionary<string, string[]>? _cache;

    public PermissionStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public IReadOnlyList<string> GetAllowedRoles(string policyName)
    {
        EnsureLoaded();
        if (_cache!.TryGetValue(policyName, out var roles))
            return roles;
        return AuthorizationPolicies.PolicyRoles.TryGetValue(policyName, out var fallback)
            ? fallback
            : Array.Empty<string>();
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetMatrix()
    {
        EnsureLoaded();
        return _cache!.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task ReplaceMatrixAsync(IReadOnlyList<(string PolicyName, string RoleCode, bool IsAllowed)> rows, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var existing = await db.RolePermissions.ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);
        foreach (var (policy, role, allowed) in rows)
        {
            db.RolePermissions.Add(new RolePermission
            {
                PolicyName = policy.Trim(),
                RoleCode = role.Trim(),
                IsAllowed = allowed
            });
        }
        await db.SaveChangesAsync(ct);
        Invalidate();
    }

    public void Invalidate() => _cache = null;

    private void EnsureLoaded()
    {
        if (_cache is not null) return;
        var map = new ConcurrentDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        // Defaults
        foreach (var kv in AuthorizationPolicies.PolicyRoles)
            map[kv.Key] = kv.Value.ToArray();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        List<RolePermission> rows;
        try
        {
            rows = db.RolePermissions.AsNoTracking().ToList();
        }
        catch
        {
            _cache = map;
            return;
        }

        if (rows.Count > 0)
        {
            map.Clear();
            foreach (var group in rows.Where(r => r.IsAllowed).GroupBy(r => r.PolicyName, StringComparer.OrdinalIgnoreCase))
                map[group.Key] = group.Select(r => r.RoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        _cache = map;
    }
}
