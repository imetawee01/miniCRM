using Crm.Application.Common;
using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/opportunities/{id:guid}/scope")]
public sealed class ScopeController : ControllerBase
{
    private readonly ISender _sender;
    public ScopeController(ISender sender) => _sender = sender;

    [HttpGet] public Task<ScopeDto?> Get(Guid id) => _sender.Send(new GetScopeQuery(id));

    [HttpPut] [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<Unit> PutBrief(Guid id, [FromBody] UpdateScopeBriefCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpPost("items")] [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<Guid> AddItem(Guid id, [FromBody] AddScopeItemCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpPut("items/reorder")] [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<Unit> Reorder(Guid id, [FromBody] ReorderScopeItemsCommand cmd) => _sender.Send(cmd with { OpportunityId = id });

    [HttpPut("items/{itemId:guid}")] [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<Unit> UpdItem(Guid id, Guid itemId, [FromBody] UpdateScopeItemCommand cmd) => _sender.Send(cmd with { OpportunityId = id, ItemId = itemId });

    [HttpDelete("items/{itemId:guid}")] [Authorize(Policy = AuthorizationPolicies.CanManageScope)]
    public Task<Unit> DelItem(Guid id, Guid itemId) => _sender.Send(new DeleteScopeItemCommand(id, itemId));
}
