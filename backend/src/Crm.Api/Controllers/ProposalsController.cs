using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

/// <summary>Response development: builder, costing, service-line responses, pricing, bid bond.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ProposalsController : ControllerBase
{
    private readonly ISender _sender;
    public ProposalsController(ISender sender) => _sender = sender;

    [HttpPost("opportunities/{id:guid}/builder")] [Authorize(Policy = AuthorizationPolicies.CanAssignBuilder)]
    public Task<Unit> Builder(Guid id, [FromBody] AssignBuilderCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpGet("opportunities/{id:guid}/estimated-cost")]
    public Task<EstimatedCostDto?> Est(Guid id) => _sender.Send(new GetEstimatedCostQuery(id));
    [HttpPost("opportunities/{id:guid}/estimated-cost")] [Authorize(Policy = AuthorizationPolicies.CanEditPricing)]
    public Task<Unit> EstPost(Guid id, [FromBody] SubmitEstimatedCostCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpGet("opportunities/{id:guid}/sl-responses")] public Task<IReadOnlyList<SlResponseDto>> Sl(Guid id) => _sender.Send(new GetSlResponsesQuery(id));
    [HttpPost("opportunities/{id:guid}/sl-responses")] [Authorize(Policy = AuthorizationPolicies.CanManageSlResponses)]
    public Task<Guid> SlPost(Guid id, [FromBody] CreateSlResponseCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPut("sl-responses/{id:guid}")] [Authorize(Policy = AuthorizationPolicies.CanRespondAsServiceLine)]
    public Task<Unit> SlPut(Guid id, [FromBody] UpdateSlResponseCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPost("sl-responses/{id:guid}/submit")] [Authorize(Policy = AuthorizationPolicies.CanRespondAsServiceLine)]
    public Task<Unit> SlSubmit(Guid id) => _sender.Send(new SubmitSlResponseCommand(id));
    [HttpPost("sl-responses/{id:guid}/return")] [Authorize(Policy = AuthorizationPolicies.CanManageSlResponses)]
    public Task<Unit> SlReturn(Guid id, [FromBody] ReturnSlResponseCommand cmd) => _sender.Send(cmd with { Id = id });

    [HttpGet("opportunities/{id:guid}/pricing")]
    public Task<IReadOnlyList<ProposalPricingDto>> Pricing(Guid id) => _sender.Send(new GetPricingQuery(id));
    [HttpPost("opportunities/{id:guid}/pricing")] [Authorize(Policy = AuthorizationPolicies.CanEditPricing)]
    public Task<ProposalPricingDto> PricingPost(Guid id, [FromBody] CreatePricingCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpPost("opportunities/{id:guid}/proposal/ready")] [Authorize(Policy = AuthorizationPolicies.CanBuildProposal)]
    public Task Ready(Guid id) => _sender.Send(new MarkProposalReadyCommand(id));

    [HttpGet("proposals/my-tasks")] public Task<PagedResult<ProposalTaskDto>> MyTasks([FromQuery] GetMyProposalTasksQuery q) => _sender.Send(q);

    [HttpGet("opportunities/{id:guid}/bid-bond")] public Task<BidBond?> Bond(Guid id) => _sender.Send(new GetBidBondQuery(id));
    [HttpPost("opportunities/{id:guid}/bid-bond/request")] [Authorize(Policy = AuthorizationPolicies.CanManageBidBond)]
    public Task BondReq(Guid id) => _sender.Send(new RequestBidBondCommand(id));
    [HttpPost("opportunities/{id:guid}/bid-bond/issue")] [Authorize(Policy = AuthorizationPolicies.CanSubmit)]
    public Task BondIssue(Guid id, [FromBody] IssueBidBondCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
    [HttpPost("opportunities/{id:guid}/bid-bond/reject")] [Authorize(Policy = AuthorizationPolicies.CanSubmit)]
    public Task BondReject(Guid id, [FromBody] RejectBidBondCommand cmd) => _sender.Send(cmd with { OpportunityId = id });
}
