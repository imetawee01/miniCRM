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
public sealed class ContractsController : ControllerBase
{
    private const string Manage = AuthorizationPolicies.CanManageContract;
    private readonly ISender _sender;
    public ContractsController(ISender sender) => _sender = sender;

    [HttpGet("contracts")] public Task<PagedResult<ContractDto>> List([FromQuery] ContractListQuery q) => _sender.Send(q);
    [HttpGet("contracts/{id:guid}")] public Task<ContractDto> Get(Guid id) => _sender.Send(new GetContractQuery(id));
    [HttpPost("opportunities/{id:guid}/contract")] [Authorize(Policy = Manage)] public Task<ContractDto> Create(Guid id) => _sender.Send(new CreateContractCommand(id));
    [HttpPut("contracts/{id:guid}")] [Authorize(Policy = Manage)] public Task<Unit> Update(Guid id, [FromBody] UpdateContractCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPost("contracts/{id:guid}/status")] [Authorize(Policy = Manage)] public Task<Unit> Status(Guid id, [FromBody] ChangeContractStatusCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpGet("contracts/{id:guid}/negotiation-rounds")] public Task<IReadOnlyList<ContractNegotiationRound>> Rounds(Guid id) => _sender.Send(new GetRoundsQuery(id));
    [HttpPost("contracts/{id:guid}/negotiation-rounds")] [Authorize(Policy = Manage)] public Task<Guid> AddRound(Guid id, [FromBody] CreateRoundCommand cmd) => _sender.Send(cmd with { ContractId = id });
    [HttpPut("contracts/negotiation-rounds/{roundId:guid}")] [Authorize(Policy = Manage)] public Task<Unit> UpdRound(Guid roundId, [FromBody] UpdateRoundCommand cmd) => _sender.Send(cmd with { RoundId = roundId });
    [HttpPost("contracts/negotiation-rounds/{roundId:guid}/close")] [Authorize(Policy = Manage)] public Task<Unit> CloseRound(Guid roundId, [FromBody] CloseRoundCommand cmd) => _sender.Send(cmd with { RoundId = roundId });
    [HttpGet("contracts/{id:guid}/milestones")] public Task<IReadOnlyList<ContractMilestone>> Milestones(Guid id) => _sender.Send(new GetMilestonesQuery(id));
    [HttpPost("contracts/{id:guid}/milestones")] [Authorize(Policy = Manage)] public Task<Guid> AddMilestone(Guid id, [FromBody] CreateMilestoneCommand cmd) => _sender.Send(cmd with { ContractId = id });
    [HttpPut("contracts/milestones/{id:guid}")] [Authorize(Policy = Manage)] public Task<Unit> UpdMilestone(Guid id, [FromBody] UpdateMilestoneCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPost("contracts/{id:guid}/sign")] [Authorize(Policy = Manage)] public Task<Unit> Sign(Guid id, [FromBody] SignContractCommand cmd) => _sender.Send(cmd with { Id = id });
}
