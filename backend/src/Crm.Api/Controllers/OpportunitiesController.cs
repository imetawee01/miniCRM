using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Application.Opportunities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/opportunities")]
public sealed class OpportunitiesController : ControllerBase
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly ISender _sender;
    private readonly IExcelExporter _excel;
    public OpportunitiesController(ISender sender, IExcelExporter excel) { _sender = sender; _excel = excel; }

    [HttpGet]
    public Task<PagedResult<OpportunityListItemDto>> List([FromQuery] OpportunityListQuery query) => _sender.Send(query);

    /// <summary>Excel export of every row matching the same filters as the list (no 200-row cap).</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ExportOpportunitiesQuery q, CancellationToken ct)
    {
        IReadOnlyList<OpportunityListItemDto> rows = await _sender.Send(q, ct);
        var bytes = _excel.Export<OpportunityListItemDto>("Opportunities",
        [
            ("Number", (OpportunityListItemDto o) => o.OpportunityNumber),
            ("Name", o => o.Name),
            ("Customer", o => o.CustomerName),
            ("Customer (AR)", o => o.CustomerNameAr),
            ("Stage", o => o.StageNameEn),
            ("Status", o => o.StatusNameEn),
            ("Pending on", o => o.PendingUserName ?? o.PendingRoleCode),
            ("Pending gate", o => o.PendingGateNameEn),
            ("Expected value (SAR)", o => o.ExpectedValueSar),
            ("Submission theme", o => o.SubmissionTheme.ToString()),
            ("Source channel", o => o.SourceChannel.ToString()),
            ("Engagement", o => o.EngagementType.ToString()),
            ("Type", o => o.OpportunityType.ToString()),
            ("Owner", o => o.OwnerDisplayName),
            ("Builder", o => o.BuilderDisplayName),
            ("Submitted by", o => o.SubmittedByDisplayName),
            ("Internal deadline", o => o.InternalDeadline),
            ("Submission deadline", o => o.SubmissionDeadline),
            ("Closed", o => o.IsClosed),
            ("Created (UTC)", o => o.CreatedAtUtc)
        ], rows);
        return File(bytes, XlsxMime, $"opportunities-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpGet("{id:guid}")]
    public Task<OpportunityDetailDto> Get(Guid id) => _sender.Send(new GetOpportunityQuery(id));

    [HttpGet("{id:guid}/summary")]
    public Task<OpportunitySummaryDto> Summary(Guid id) => _sender.Send(new GetOpportunitySummaryQuery(id));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateOpportunity)]
    public async Task<ActionResult<OpportunityDetailDto>> Create([FromBody] CreateOpportunityCommand cmd)
    {
        var result = await _sender.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<OpportunityDetailDto> Update(Guid id, [FromBody] UpdateOpportunityCommand cmd)
    {
        cmd.Id = id;
        return _sender.Send(cmd);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanAdminister)]
    public Task Delete(Guid id) => _sender.Send(new DeleteOpportunityCommand(id));

    [HttpPut("{id:guid}/deadlines")]
    [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task Deadlines(Guid id, [FromBody] SetDeadlinesCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpPut("{id:guid}/owner")]
    [Authorize(Policy = AuthorizationPolicies.CanAssignBuilder)]
    public Task Owner(Guid id, [FromBody] SetOwnerCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpGet("{id:guid}/timeline")]
    public Task<IReadOnlyList<TimelineItemDto>> Timeline(Guid id) => _sender.Send(new GetTimelineQuery(id));

    [HttpGet("{id:guid}/journey")]
    public Task<OpportunityJourneyDto> Journey(Guid id) => _sender.Send(new GetOpportunityJourneyQuery(id));

    [HttpGet("{id:guid}/next-actions")]
    public Task<OpportunityNextActionsDto> NextActions(Guid id) => _sender.Send(new GetOpportunityNextActionsQuery(id));

    [HttpGet("{id:guid}/available-transitions")]
    public Task<IReadOnlyList<AvailableTransitionDto>> Transitions(Guid id) => _sender.Send(new GetAvailableTransitionsQuery(id));

    [HttpPost("{id:guid}/status")]
    public Task Status(Guid id, [FromBody] ChangeStatusCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpPost("{id:guid}/hold")]
    [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task Hold(Guid id, [FromBody] HoldCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpPost("{id:guid}/resume")]
    [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task Resume(Guid id) => _sender.Send(new ResumeCommand(id));

    [HttpPost("{id:guid}/cancel")]
    public Task Cancel(Guid id, [FromBody] CancelOpportunityCommand cmd) => _sender.Send(cmd with { Id = id });
}
