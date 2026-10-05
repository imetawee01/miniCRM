using Crm.Application.Abstractions;
using Crm.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Crm.Infrastructure.Security;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string policyName) => PolicyName = policyName;
    public string PolicyName { get; }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionStore _store;

    public PermissionAuthorizationHandler(IPermissionStore store) => _store = store;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var allowed = _store.GetAllowedRoles(requirement.PolicyName);
        if (allowed.Count == 0)
            return Task.CompletedTask;

        if (context.User.IsInRole(Domain.Common.RoleCodes.Admin)
            || allowed.Any(r => context.User.IsInRole(r)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>Builds Authorize policies from <see cref="AuthorizationPolicies"/> keys, evaluated via <see cref="IPermissionStore"/>.</summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (AuthorizationPolicies.PolicyRoles.ContainsKey(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
