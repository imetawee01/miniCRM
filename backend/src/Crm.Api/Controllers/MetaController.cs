using Crm.Application.Query;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/meta")]
public sealed class MetaController : ControllerBase
{
    private readonly ISender _sender;
    public MetaController(ISender sender) => _sender = sender;

    [HttpGet("{entity}/fields")]
    public Task<IReadOnlyList<MetaFieldDto>> Fields(string entity) =>
        _sender.Send(new GetEntityMetaFieldsQuery(entity));
}
