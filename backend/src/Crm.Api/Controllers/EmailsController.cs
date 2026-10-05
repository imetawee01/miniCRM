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
public sealed class EmailsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _config;
    public EmailsController(ISender sender, IConfiguration config) { _sender = sender; _config = config; }

    [HttpGet("opportunities/{id:guid}/emails")] public Task<IReadOnlyList<GeneratedEmail>> ForOpportunity(Guid id) => _sender.Send(new GetEmailsQuery(id));
    [HttpGet("emails/{id:guid}")] public Task<GeneratedEmail> Get(Guid id) => _sender.Send(new GetEmailQuery(id));
    [HttpPost("emails/{id:guid}/regenerate")] public Task<GeneratedEmail> Regenerate(Guid id) => _sender.Send(new RegenerateEmailCommand(id));
    [HttpPost("emails/{id:guid}/mark-sent")] public Task<Unit> MarkSent(Guid id) => _sender.Send(new MarkEmailSentCommand(id));

    [HttpGet("emails/{id:guid}/eml")]
    public async Task<IActionResult> Eml(Guid id, CancellationToken ct)
    {
        var email = await _sender.Send(new GetEmailQuery(id), ct);
        var from = _config["Email:From"] ?? "crm@localhost";
        var safeSubject = string.Concat(email.Subject.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var bytes = System.Text.Encoding.UTF8.GetBytes(
            $"From: {from}\r\nTo: {email.To}\r\nCc: {email.Cc}\r\nSubject: {email.Subject}\r\nMIME-Version: 1.0\r\nContent-Type: text/html; charset=utf-8\r\n\r\n{email.BodyHtml}");
        return File(bytes, "message/rfc822", $"{safeSubject}.eml");
    }

    [HttpGet("email-templates")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<IReadOnlyList<EmailTemplate>> Templates() => _sender.Send(new GetEmailTemplatesQuery());
    [HttpPut("email-templates/{code}")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<Unit> Update(string code, [FromBody] UpdateEmailTemplateCommand cmd) => _sender.Send(cmd with { Code = code });
    [HttpPost("email-templates/{code}/preview")] [Authorize(Policy = AuthorizationPolicies.CanAdminister)] public Task<GeneratedEmail> Preview(string code, [FromBody] PreviewEmailCommand cmd) => _sender.Send(cmd with { Code = code });
}
