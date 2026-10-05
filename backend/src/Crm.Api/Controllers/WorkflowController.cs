using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

/// <summary>Gates, approvals and qualification. Gate decisions are role-checked by the workflow engine per gate.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class WorkflowController : ControllerBase
{
    private readonly ISender _sender;
    public WorkflowController(ISender sender) => _sender = sender;

    // Gates / approvals
    [HttpGet("opportunities/{id:guid}/gates")] public Task<IReadOnlyList<GateInstanceDto>> Gates(Guid id) => _sender.Send(new GetOpportunityGatesQuery(id));
    [HttpGet("gates/{gateInstanceId:guid}")] public Task<GateInstanceDto> Gate(Guid gateInstanceId) => _sender.Send(new GetGateInstanceQuery(gateInstanceId));
    [HttpPost("gates/{gateInstanceId:guid}/decision")] public Task<Unit> Decide(Guid gateInstanceId, [FromBody] DecideGateCommand cmd) => _sender.Send(cmd with { GateInstanceId = gateInstanceId });
    [HttpPost("gates/{gateInstanceId:guid}/reassign")] public Task<Unit> Reassign(Guid gateInstanceId, [FromBody] ReassignGateCommand cmd) => _sender.Send(cmd with { GateInstanceId = gateInstanceId });
    [HttpGet("approvals/pending")] public Task<PagedResult<GateInstanceDto>> Pending([FromQuery] GetPendingApprovalsQuery q) => _sender.Send(q);
    [HttpGet("approvals/pending/count")] public Task<PendingCountDto> PendingCount() => _sender.Send(new GetPendingCountQuery());

    // Workflow configuration (admin)
    [HttpGet("workflow/gates")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<IReadOnlyList<WorkflowGateDto>> WfGates() => _sender.Send(new GetWorkflowGatesQuery());
    [HttpPut("workflow/gates/{id:guid}")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<Unit> WfGate(Guid id, [FromBody] UpdateWorkflowGateCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpGet("workflow/transitions")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<IReadOnlyList<TransitionDto>> Transitions() => _sender.Send(new GetWorkflowTransitionsQuery());

    // Qualification
    [HttpPost("opportunities/{id:guid}/qualification/route")] [Authorize(Policy = AuthorizationPolicies.CanReviewGw1)]
    public Task<Unit> Route(Guid id, [FromBody] SetQualificationRouteCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPost("opportunities/{id:guid}/qualification/meeting")] [Authorize(Policy = AuthorizationPolicies.CanManageMeeting)]
    public Task<Guid> CreateMeeting(Guid id, [FromBody] CreateMeetingCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpGet("opportunities/{id:guid}/qualification/meeting")] public Task<QualificationMeeting?> GetMeeting(Guid id) => _sender.Send(new GetMeetingQuery(id));
    [HttpPut("opportunities/{id:guid}/qualification/meeting")] [Authorize(Policy = AuthorizationPolicies.CanManageMeeting)]
    public Task<Unit> UpdMeeting(Guid id, [FromBody] UpdateMeetingCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPost("opportunities/{id:guid}/qualification/meeting/minutes")] [Authorize(Policy = AuthorizationPolicies.CanManageMeeting)]
    public Task<Unit> Minutes(Guid id, [FromBody] SaveMinutesCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPost("opportunities/{id:guid}/qualification/meeting/attendees")] [Authorize(Policy = AuthorizationPolicies.CanManageMeeting)]
    public Task<Unit> Attendee(Guid id, [FromBody] AddAttendeeCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPut("qualification/meeting/attendees/{attendeeId:guid}/response")]
    public Task<Unit> AttResp(Guid attendeeId, [FromBody] SetAttendeeResponseCommand cmd) => _sender.Send(cmd with { AttendeeId = attendeeId });
}
