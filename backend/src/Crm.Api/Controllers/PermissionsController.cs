using Crm.Application.Common;
using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.CanAdminister)]
[Route("api/v1/admin")]
public sealed class PermissionsController : ControllerBase
{
    private readonly ISender _sender;
    public PermissionsController(ISender sender) => _sender = sender;

    [HttpGet("permissions")]
    public Task<PermissionMatrixDto> Get() => _sender.Send(new GetPermissionMatrixQuery());

    [HttpPut("permissions")]
    public Task<Unit> Save([FromBody] SavePermissionMatrixCommand cmd) => _sender.Send(cmd);
}
