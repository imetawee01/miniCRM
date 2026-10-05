using System.Linq.Expressions;
using Crm.Domain.Entities;

namespace Crm.Application.Common;

/// <summary>
/// Builds EF-translatable predicates for pipe/comma-separated gate role codes.
/// Avoids <c>roles.Any(r => assigned.Contains(r))</c>, which InMemory cannot translate.
/// </summary>
public static class GateRoleFilter
{
    public static IQueryable<GateInstance> WhereAssignedTo(
        this IQueryable<GateInstance> source,
        Guid? userId,
        IReadOnlyList<string> roles)
    {
        var parameter = Expression.Parameter(typeof(GateInstance), "g");
        Expression? body = null;

        if (userId is Guid uid)
        {
            var assignedUser = Expression.Property(parameter, nameof(GateInstance.AssignedUserId));
            body = Expression.Equal(assignedUser, Expression.Constant(uid, typeof(Guid?)));
        }

        var roleProp = Expression.Property(parameter, nameof(GateInstance.AssignedRoleCode));
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            // Constant role string → translates on both SQL Server and InMemory
            var call = Expression.Call(roleProp, contains, Expression.Constant(role));
            body = body is null ? call : Expression.OrElse(body, call);
        }

        if (body is null)
            return source.Where(_ => false);

        var lambda = Expression.Lambda<Func<GateInstance, bool>>(body, parameter);
        return source.Where(lambda);
    }

    public static bool Matches(string assignedRoleCode, IReadOnlyList<string> userRoles)
    {
        if (string.IsNullOrWhiteSpace(assignedRoleCode) || userRoles.Count == 0)
            return false;
        var needed = assignedRoleCode.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return needed.Any(n => userRoles.Contains(n, StringComparer.OrdinalIgnoreCase));
    }
}
