using Crm.Application.Common;
using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

/// <summary>Administration of users and service lines. For pickers use GET /users/pickable (LookupsController).</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.CanAdminister)]
[Route("api/v1")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    public UsersController(ISender sender) => _sender = sender;

    [HttpGet("users")] public Task<PagedResult<UserListItemDto>> List([FromQuery] GetUsersQuery q) => _sender.Send(q);
    [HttpPost("users")] public Task<Guid> Create([FromBody] CreateUserCommand cmd) => _sender.Send(cmd);
    [HttpPut("users/{id:guid}")] public Task<Unit> Update(Guid id, [FromBody] UpdateUserCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPut("users/{id:guid}/roles")] public Task<Unit> Roles(Guid id, [FromBody] SetUserRolesCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPost("users/{id:guid}/activate")] public Task<Unit> Activate(Guid id) => _sender.Send(new ActivateUserCommand(id, true));
    [HttpPost("users/{id:guid}/deactivate")] public Task<Unit> Deactivate(Guid id) => _sender.Send(new ActivateUserCommand(id, false));

    [HttpPost("service-lines")] public Task<Guid> CreateServiceLine([FromBody] CreateServiceLineCommand cmd) => _sender.Send(cmd);
    [HttpPut("service-lines/{id:guid}")] public Task<Unit> UpdateServiceLine(Guid id, [FromBody] UpdateServiceLineCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpPost("service-lines/{id:guid}/deactivate")] public Task<Unit> DeactivateServiceLine(Guid id) => _sender.Send(new DeactivateServiceLineCommand(id));
}
