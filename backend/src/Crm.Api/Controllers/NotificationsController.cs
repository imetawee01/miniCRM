using Crm.Application.Common;
using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;
    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet] public Task<PagedResult<NotificationDto>> List([FromQuery] GetNotificationsQuery q) => _sender.Send(q);
    [HttpPost("{id:guid}/read")] public Task<Unit> Read(Guid id) => _sender.Send(new MarkReadCommand(id));
    [HttpPost("read-all")] public Task<Unit> ReadAll() => _sender.Send(new MarkAllReadCommand());
    [HttpGet("unread-count")] public Task<UnreadCountDto> Unread() => _sender.Send(new UnreadCountQuery());
}
