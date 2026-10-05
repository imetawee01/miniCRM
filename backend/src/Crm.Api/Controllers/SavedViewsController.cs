using Crm.Application.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class SavedViewsController : ControllerBase
{
    private readonly ISender _sender;
    public SavedViewsController(ISender sender) => _sender = sender;

    [HttpGet("saved-views")]
    public Task<IReadOnlyList<SavedViewDto>> List([FromQuery] string entityType) =>
        _sender.Send(new GetSavedViewsQuery(entityType));

    [HttpPost("saved-views")]
    public Task<Guid> Create([FromBody] CreateSavedViewCommand cmd) => _sender.Send(cmd);

    [HttpPut("saved-views/{id:guid}")]
    public Task<Unit> Update(Guid id, [FromBody] UpdateSavedViewCommand cmd) =>
        _sender.Send(cmd with { Id = id });

    [HttpDelete("saved-views/{id:guid}")]
    public Task<Unit> Delete(Guid id) => _sender.Send(new DeleteSavedViewCommand(id));
}
