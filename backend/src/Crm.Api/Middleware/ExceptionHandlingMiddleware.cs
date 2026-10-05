using System.Diagnostics;
using Crm.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crm.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblem(context, ex);
        }
    }

    private async Task WriteProblem(HttpContext context, Exception ex)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        ProblemDetails problem;
        int status;

        switch (ex)
        {
            case AppException app:
                status = app.StatusCode;
                problem = new ProblemDetails
                {
                    Title = app.Title,
                    Detail = app.Message,
                    Status = status,
                    Type = $"https://httpstatuses.com/{status}",
                    Instance = context.Request.Path
                };
                if (app.Errors is not null)
                    problem.Extensions["errors"] = app.Errors;
                if (status == StatusCodes.Status403Forbidden)
                    await TryAuditForbiddenAsync(context, app.Message);
                break;
            case DbUpdateConcurrencyException:
                status = StatusCodes.Status409Conflict;
                problem = new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = "The record was modified by another user. Reload and try again.",
                    Status = status,
                    Instance = context.Request.Path
                };
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);
                problem = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.",
                    Status = status,
                    Instance = context.Request.Path
                };
                break;
        }

        problem.Extensions["traceId"] = traceId;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task TryAuditForbiddenAsync(HttpContext context, string detail)
    {
        try
        {
            var audit = context.RequestServices.GetService<Crm.Application.Abstractions.IAuditWriter>();
            var db = context.RequestServices.GetService<Crm.Application.Abstractions.IApplicationDbContext>();
            var user = context.RequestServices.GetService<Crm.Application.Abstractions.ICurrentUser>();
            if (audit is null || db is null) return;
            audit.Add(
                "HttpRequest",
                Guid.Empty,
                Crm.Domain.Enums.AuditAction.AccessDenied,
                $"403 Forbidden: {context.Request.Method} {context.Request.Path} — {detail}",
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    path = context.Request.Path.Value,
                    method = context.Request.Method,
                    actor = user?.UserId,
                    roles = user?.Roles
                }));
            await db.SaveChangesAsync(context.RequestAborted);
        }
        catch
        {
            // Never fail the response because of audit side-effects.
        }
    }
}

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("{Method} {Path} => {Status} in {Elapsed}ms trace {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                context.TraceIdentifier);
        }
    }
}
