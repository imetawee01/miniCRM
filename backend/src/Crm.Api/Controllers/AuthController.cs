using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Application.Identity;
using Crm.Application.Opportunities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public Task<AuthResponse> Login([FromBody] LoginCommand cmd) => _sender.Send(cmd);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public Task<AuthResponse> Refresh([FromBody] RefreshCommand cmd) => _sender.Send(cmd);

    [HttpPost("logout")]
    [Authorize]
    public Task Logout([FromBody] LogoutCommand? cmd) => _sender.Send(cmd ?? new LogoutCommand(null));

    [HttpGet("me")]
    [Authorize]
    public Task<UserProfileDto> Me() => _sender.Send(new GetMeQuery());

    [HttpPost("change-password")]
    [Authorize]
    public Task ChangePassword([FromBody] ChangePasswordCommand cmd) => _sender.Send(cmd);

    /// <summary>TODO(SSO): return live Entra config when enabled.</summary>
    [HttpGet("sso/config")]
    [AllowAnonymous]
    public IActionResult SsoConfig() => Ok(new { ssoEnabled = false });

    /// <summary>TODO(SSO): start the Entra ID challenge.</summary>
    [HttpGet("sso/challenge")]
    [AllowAnonymous]
    public IActionResult SsoChallenge() => StatusCode(StatusCodes.Status501NotImplemented, new { title = "SSO is not enabled" });
}
