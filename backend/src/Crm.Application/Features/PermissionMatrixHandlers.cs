using Crm.Application.Abstractions;
using Crm.Application.Common;
using MediatR;

namespace Crm.Application.Features;

public record PermissionCellDto(string PolicyName, string RoleCode, bool IsAllowed);
public record PermissionMatrixDto(
    IReadOnlyList<string> Policies,
    IReadOnlyList<string> Roles,
    IReadOnlyList<PermissionCellDto> Cells);

public record GetPermissionMatrixQuery : IRequest<PermissionMatrixDto>;
public record SavePermissionMatrixCommand(IReadOnlyList<PermissionCellDto> Cells) : IRequest<Unit>;

public sealed class PermissionMatrixHandlers :
    IRequestHandler<GetPermissionMatrixQuery, PermissionMatrixDto>,
    IRequestHandler<SavePermissionMatrixCommand, Unit>
{
    private readonly IPermissionStore _store;

    public PermissionMatrixHandlers(IPermissionStore store) => _store = store;

    public Task<PermissionMatrixDto> Handle(GetPermissionMatrixQuery request, CancellationToken ct)
    {
        var policies = AuthorizationPolicies.PolicyRoles.Keys.OrderBy(k => k).ToList();
        var roles = AuthorizationPolicies.PolicyRoles.Values
            .SelectMany(v => v)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();
        var matrix = _store.GetMatrix();
        var cells = new List<PermissionCellDto>();
        foreach (var policy in policies)
        {
            var allowed = matrix.TryGetValue(policy, out var list)
                ? list
                : AuthorizationPolicies.PolicyRoles[policy];
            foreach (var role in roles)
                cells.Add(new PermissionCellDto(policy, role, allowed.Contains(role, StringComparer.OrdinalIgnoreCase)));
        }
        return Task.FromResult(new PermissionMatrixDto(policies, roles, cells));
    }

    public async Task<Unit> Handle(SavePermissionMatrixCommand request, CancellationToken ct)
    {
        var rows = request.Cells
            .Select(c => (c.PolicyName, c.RoleCode, c.IsAllowed))
            .ToList();
        await _store.ReplaceMatrixAsync(rows, ct);
        return Unit.Value;
    }
}
