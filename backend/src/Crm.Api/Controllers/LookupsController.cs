using Crm.Application.Features;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class LookupsController : ControllerBase
{
    private readonly ISender _sender;
    public LookupsController(ISender sender) => _sender = sender;

    [HttpGet("lookups/all")] public Task<LookupsAllDto> LookupsAll() => _sender.Send(new GetLookupsAllQuery());
    [HttpGet("lookups/stages")] public Task<IReadOnlyList<StageLookupDto>> Stages() => _sender.Send(new GetStagesQuery());
    [HttpGet("lookups/statuses")] public Task<IReadOnlyList<StatusLookupDto>> Statuses([FromQuery] Guid? stageId) => _sender.Send(new GetStatusesQuery(stageId));
    [HttpGet("lookups/service-lines")] public Task<IReadOnlyList<ServiceLineDto>> ServiceLines() => _sender.Send(new GetServiceLinesQuery());
    [HttpGet("lookups/source-channels")] public IActionResult SourceChannels() => Ok(Enum.GetNames<SourceChannel>().Select(v => new { value = v, nameEn = v }));
    [HttpGet("lookups/submission-themes")] public IActionResult Themes() => Ok(Enum.GetNames<SubmissionTheme>().Select(v => new { value = v, nameEn = v }));
    [HttpGet("lookups/proposal-languages")] public IActionResult Languages() => Ok(Enum.GetNames<ProposalLanguage>().Select(v => new { value = v, nameEn = v }));
    [HttpGet("lookups/attachment-categories")] public IActionResult Categories() => Ok(Enum.GetNames<AttachmentCategory>().Select(v => new { value = v, nameEn = v }));
    [HttpGet("roles")] public Task<IReadOnlyList<LookupItemDto>> Roles() => _sender.Send(new GetRolesQuery());
    [HttpGet("service-lines")]
    public Task<IReadOnlyList<ServiceLineDto>> ServiceLinesList([FromQuery] bool includeInactive = false) =>
        _sender.Send(new GetServiceLinesQuery(includeInactive));

    /// <summary>Non-admin user list for pickers (id, display name, roles). Optional role filter (comma/pipe separated).</summary>
    [HttpGet("users/pickable")]
    public Task<IReadOnlyList<UserPickDto>> Pickable([FromQuery] string? role, [FromQuery] string? search) => _sender.Send(new GetPickableUsersQuery(role, search));
}
