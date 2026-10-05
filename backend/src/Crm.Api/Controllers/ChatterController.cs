using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ChatterController : ControllerBase
{
    private readonly ISender _sender;
    public ChatterController(ISender sender) => _sender = sender;

    [HttpGet("opportunities/{id:guid}/chatter")]
    public Task<IReadOnlyList<ChatterItemDto>> Chatter(Guid id, [FromQuery] string? kind) =>
        _sender.Send(new GetChatterQuery(id, kind));

    [HttpGet("opportunities/{id:guid}/smart-buttons")]
    public Task<SmartButtonsDto> SmartButtons(Guid id) =>
        _sender.Send(new GetSmartButtonsQuery(id));

    [HttpGet("opportunities/{id:guid}/activities")]
    public Task<IReadOnlyList<ActivityDto>> ForOpportunity(Guid id) =>
        _sender.Send(new GetOpportunityActivitiesQuery(id));

    [HttpGet("activities/mine")]
    public Task<IReadOnlyList<ActivityDto>> Mine([FromQuery] bool overdueOnly = false, [FromQuery] bool includeDone = false) =>
        _sender.Send(new GetMyActivitiesQuery(overdueOnly, includeDone));

    [HttpPost("opportunities/{id:guid}/activities")]
    public Task<ActivityDto> Create(Guid id, [FromBody] CreateActivityCommand cmd) =>
        _sender.Send(cmd with { OpportunityId = id });

    [HttpPut("activities/{id:guid}")]
    public Task<Unit> Update(Guid id, [FromBody] UpdateActivityCommand cmd) =>
        _sender.Send(cmd with { Id = id });

    [HttpPost("activities/{id:guid}/complete")]
    public Task<Unit> Complete(Guid id, [FromBody] CompleteActivityCommand? cmd) =>
        _sender.Send((cmd ?? new CompleteActivityCommand(id, null)) with { Id = id });

    [HttpDelete("activities/{id:guid}")]
    public Task<Unit> Delete(Guid id) =>
        _sender.Send(new DeleteActivityCommand(id));
}
