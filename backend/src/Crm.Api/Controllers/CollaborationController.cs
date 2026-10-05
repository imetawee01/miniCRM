using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Application.Features;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

/// <summary>Notes, comments and attachments — polymorphic over {entityType}/{entityId}.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class CollaborationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IFileStorage _files;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;

    public CollaborationController(ISender sender, IFileStorage files, IApplicationDbContext db, ICurrentUser user, IDateTime clock, IAuditWriter audit)
    {
        _sender = sender; _files = files; _db = db; _user = user; _clock = clock; _audit = audit;
    }

    [HttpGet("{entityType}/{entityId:guid}/notes")] public Task<IReadOnlyList<NoteDto>> Notes(string entityType, Guid entityId) => _sender.Send(new GetNotesQuery(entityType, entityId));
    [HttpPost("{entityType}/{entityId:guid}/notes")] public Task<NoteDto> AddNote(string entityType, Guid entityId, [FromBody] CreateNoteCommand cmd) => _sender.Send(cmd with { EntityType = entityType, EntityId = entityId });
    [HttpPut("notes/{id:guid}")] public Task<Unit> UpdNote(Guid id, [FromBody] UpdateNoteCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpDelete("notes/{id:guid}")] public Task<Unit> DelNote(Guid id) => _sender.Send(new DeleteNoteCommand(id));

    [HttpGet("{entityType}/{entityId:guid}/comments")] public Task<IReadOnlyList<CommentDto>> Comments(string entityType, Guid entityId) => _sender.Send(new GetCommentsQuery(entityType, entityId));
    [HttpPost("{entityType}/{entityId:guid}/comments")] public Task<CommentDto> AddComment(string entityType, Guid entityId, [FromBody] CreateCommentCommand cmd) => _sender.Send(cmd with { EntityType = entityType, EntityId = entityId });
    [HttpPut("comments/{id:guid}")] public Task<Unit> UpdComment(Guid id, [FromBody] UpdateCommentCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpDelete("comments/{id:guid}")] public Task<Unit> DelComment(Guid id) => _sender.Send(new DeleteCommentCommand(id));

    [HttpGet("{entityType}/{entityId:guid}/attachments")] public Task<IReadOnlyList<AttachmentDto>> Attachments(string entityType, Guid entityId) => _sender.Send(new GetAttachmentsQuery(entityType, entityId));
    [HttpGet("attachments/{id:guid}")] public Task<AttachmentDto> Attachment(Guid id) => _sender.Send(new GetAttachmentQuery(id));
    [HttpPut("attachments/{id:guid}")] public Task<Unit> UpdAttachment(Guid id, [FromBody] UpdateAttachmentCommand cmd) => _sender.Send(cmd with { Id = id });
    [HttpDelete("attachments/{id:guid}")] public Task<Unit> DelAttachment(Guid id) => _sender.Send(new DeleteAttachmentCommand(id));

    [HttpPost("{entityType}/{entityId:guid}/attachments")]
    [RequestSizeLimit(26_214_400)]
    public async Task<ActionResult<AttachmentDto>> Upload(string entityType, Guid entityId, IFormFile file, [FromForm] string description, [FromForm] AttachmentCategory category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationAppException(new Dictionary<string, string[]> { ["description"] = ["Description is required."] });
        if (file is null || file.Length == 0)
            throw new ValidationAppException(new Dictionary<string, string[]> { ["file"] = ["A non-empty file is required."] });

        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(stream, file.FileName, file.ContentType, ct);
        var att = new Attachment
        {
            EntityType = EntityTypeMapper.Parse(entityType),
            EntityId = entityId,
            FileName = stored.OriginalFileName,
            StoredFileName = stored.StoredFileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Description = description,
            Category = category,
            UploadedByUserId = _user.UserId!.Value,
            UploadedAtUtc = _clock.UtcNow
        };
        _db.Attachments.Add(att);
        await _db.SaveChangesAsync(ct);
        return Ok(new AttachmentDto(att.Id, att.FileName, att.ContentType, att.SizeBytes, att.Description, att.Category, att.UploadedByUserId, _user.DisplayName ?? "", att.UploadedAtUtc));
    }

    [HttpGet("attachments/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var att = await _db.Attachments.FindAsync([id], ct) ?? throw new NotFoundException(nameof(Attachment), id);
        _audit.Add("Attachment", att.Id, AuditAction.AttachmentDownloaded, att.FileName, null);
        await _db.SaveChangesAsync(ct);
        var stream = await _files.OpenReadAsync(att.StoredFileName, ct);
        return File(stream, att.ContentType, att.FileName);
    }
}
