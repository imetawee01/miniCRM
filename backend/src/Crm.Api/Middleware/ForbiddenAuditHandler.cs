using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Crm.Api.Middleware;

/// <summary>Writes an AccessDenied audit row whenever authorization fails with 403.</summary>
public sealed class ForbiddenAuditHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForbiddenAuditHandler> _logger;

    public ForbiddenAuditHandler(IServiceScopeFactory scopeFactory, ILogger<ForbiddenAuditHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        await _default.HandleAsync(next, context, policy, authorizeResult);

        if (authorizeResult.Succeeded || context.Response.StatusCode != StatusCodes.Status403Forbidden)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
            var user = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
            var policyNames = string.Join(",", policy.Requirements
                .OfType<Crm.Infrastructure.Security.PermissionRequirement>()
                .Select(r => r.PolicyName)
                .DefaultIfEmpty("(authorize)"));
            audit.Add(
                "HttpRequest",
                Guid.Empty,
                AuditAction.AccessDenied,
                $"403 Forbidden: {context.Request.Method} {context.Request.Path} policy={policyNames}",
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    path = context.Request.Path.Value,
                    method = context.Request.Method,
                    policy = policyNames,
                    actor = user.UserId,
                    roles = user.Roles
                }));
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            await db.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist 403 audit for {Path}", context.Request.Path);
        }
    }
}
