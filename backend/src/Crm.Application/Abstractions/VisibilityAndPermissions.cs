using Crm.Domain.Entities;

namespace Crm.Application.Abstractions;

public interface IOpportunityVisibility
{
    /// <summary>Restrict opportunity queries to rows the current user may see.</summary>
    IQueryable<Opportunity> Apply(IQueryable<Opportunity> query);

    /// <summary>Throw NotFound/Forbidden if the user cannot access the opportunity.</summary>
    Task EnsureCanAccessAsync(Guid opportunityId, CancellationToken ct = default);

    bool CanViewPricingFields { get; }
}

public interface IPermissionStore
{
    IReadOnlyList<string> GetAllowedRoles(string policyName);
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetMatrix();
    Task ReplaceMatrixAsync(IReadOnlyList<(string PolicyName, string RoleCode, bool IsAllowed)> rows, CancellationToken ct = default);
    void Invalidate();
}
