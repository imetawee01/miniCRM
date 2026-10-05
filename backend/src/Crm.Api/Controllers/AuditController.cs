using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IExcelExporter _excel;
    public AuditController(ISender sender, IExcelExporter excel) { _sender = sender; _excel = excel; }

    [HttpGet("audit")] [Authorize(Policy = AuthorizationPolicies.CanViewAudit)]
    public Task<PagedResult<AuditLog>> List([FromQuery] AuditListQuery q) => _sender.Send(q);

    [HttpGet("opportunities/{id:guid}/audit")]
    public Task<IReadOnlyList<AuditLog>> ForOpportunity(Guid id) => _sender.Send(new GetOpportunityAuditQuery(id));

    [HttpGet("audit/export")] [Authorize(Policy = AuthorizationPolicies.CanViewAudit)]
    public async Task<IActionResult> Export([FromQuery] AuditListQuery q, CancellationToken ct)
    {
        q.Page = 1; q.PageSize = 200;
        var all = new List<AuditLog>();
        while (all.Count < 50_000)
        {
            var page = await _sender.Send(q, ct);
            all.AddRange(page.Items);
            if (page.Items.Count < q.PageSize || q.Page >= page.TotalPages) break;
            q.Page++;
        }
        var bytes = _excel.Export("Audit",
        [
            ("Occurred (UTC)", (AuditLog a) => a.OccurredAtUtc),
            ("Action", a => a.Action.ToString()),
            ("Entity", a => a.EntityType),
            ("Entity Id", a => a.EntityId),
            ("Opportunity Id", a => a.OpportunityId),
            ("Actor role", a => a.ActorRoleCode),
            ("Description", a => a.Description),
            ("From", a => a.FromValue),
            ("To", a => a.ToValue)
        ], all);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"audit-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }
}
