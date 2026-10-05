using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/opportunities/{id:guid}")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISender _sender;
    public SubmissionsController(ISender sender) => _sender = sender;

    [HttpPost("submit")] [Authorize(Policy = AuthorizationPolicies.CanSubmit)]
    public Task Submit(Guid id, [FromBody] SubmitOpportunityCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpGet("submission")] public Task<Submission?> GetSubmission(Guid id) => _sender.Send(new GetSubmissionQuery(id));

    [HttpPost("outcome")] [Authorize(Policy = AuthorizationPolicies.CanRecordOutcome)]
    public Task Outcome(Guid id, [FromBody] RecordOutcomeCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpGet("outcome")] public Task<OpportunityOutcome?> GetOutcome(Guid id) => _sender.Send(new GetOutcomeQuery(id));
}
