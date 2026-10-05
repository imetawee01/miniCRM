using System.Text.Json;
using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

public record LookupItemDto(string Value, string NameEn, string NameAr);
public record StageLookupDto(Guid Id, string Code, string NameEn, string NameAr, int SortOrder);
public record StatusLookupDto(Guid Id, Guid StageId, string Code, string NameEn, string NameAr, bool IsTerminal, int SortOrder);
public record ServiceLineDto(Guid Id, string Code, string NameEn, string NameAr, Guid? LeadUserId, bool IsActive, int SortOrder, string? ColourHex);
public record LookupsAllDto(
    IReadOnlyList<StageLookupDto> Stages,
    IReadOnlyList<StatusLookupDto> Statuses,
    IReadOnlyList<LookupItemDto> SourceChannels,
    IReadOnlyList<LookupItemDto> SubmissionThemes,
    IReadOnlyList<ServiceLineDto> ServiceLines,
    IReadOnlyList<LookupItemDto> ProposalLanguages,
    IReadOnlyList<LookupItemDto> AttachmentCategories,
    IReadOnlyList<LookupItemDto> Roles);

public record GetLookupsAllQuery : IRequest<LookupsAllDto>;
public record GetStagesQuery : IRequest<IReadOnlyList<StageLookupDto>>;
public record GetStatusesQuery(Guid? StageId) : IRequest<IReadOnlyList<StatusLookupDto>>;
public record GetServiceLinesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<ServiceLineDto>>;

public sealed class LookupHandlers :
    IRequestHandler<GetLookupsAllQuery, LookupsAllDto>,
    IRequestHandler<GetStagesQuery, IReadOnlyList<StageLookupDto>>,
    IRequestHandler<GetStatusesQuery, IReadOnlyList<StatusLookupDto>>,
    IRequestHandler<GetServiceLinesQuery, IReadOnlyList<ServiceLineDto>>
{
    private readonly IApplicationDbContext _db;
    public LookupHandlers(IApplicationDbContext db) => _db = db;

    public async Task<LookupsAllDto> Handle(GetLookupsAllQuery request, CancellationToken ct)
    {
        var stages = await Handle(new GetStagesQuery(), ct);
        var statuses = await Handle(new GetStatusesQuery(null), ct);
        var sl = await Handle(new GetServiceLinesQuery(), ct);
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.NameEn)
            .Select(r => new LookupItemDto(r.Code, r.NameEn, r.NameAr)).ToListAsync(ct);
        return new LookupsAllDto(stages, statuses, EnumLookup<SourceChannel>(), EnumLookup<SubmissionTheme>(), sl,
            EnumLookup<ProposalLanguage>(), EnumLookup<AttachmentCategory>(), roles);
    }

    public async Task<IReadOnlyList<StageLookupDto>> Handle(GetStagesQuery request, CancellationToken ct) =>
        await _db.Stages.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
            .Select(s => new StageLookupDto(s.Id, s.Code, s.NameEn, s.NameAr, s.SortOrder)).ToListAsync(ct);

    public async Task<IReadOnlyList<StatusLookupDto>> Handle(GetStatusesQuery request, CancellationToken ct)
    {
        var q = _db.Statuses.AsNoTracking().Where(s => s.IsActive);
        if (request.StageId is Guid id) q = q.Where(s => s.StageId == id);
        return await q.OrderBy(s => s.SortOrder)
            .Select(s => new StatusLookupDto(s.Id, s.StageId, s.Code, s.NameEn, s.NameAr, s.IsTerminal, s.SortOrder))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceLineDto>> Handle(GetServiceLinesQuery request, CancellationToken ct)
    {
        var q = _db.ServiceLines.AsNoTracking().AsQueryable();
        if (!request.IncludeInactive) q = q.Where(s => s.IsActive);
        return await q.OrderBy(s => s.SortOrder).ThenBy(s => s.NameEn)
            .Select(s => new ServiceLineDto(s.Id, s.Code, s.NameEn, s.NameAr, s.LeadUserId, s.IsActive, s.SortOrder, s.ColourHex))
            .ToListAsync(ct);
    }

    private static IReadOnlyList<LookupItemDto> EnumLookup<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Select(v => new LookupItemDto(v.ToString(), v.ToString(), v.ToString())).ToList();
}

public record NoteDto(Guid Id, string Body, NoteVisibility Visibility, Guid CreatedByUserId, string AuthorName, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc);
public record CommentDto(Guid Id, string Body, Guid? ParentCommentId, Guid CreatedByUserId, string AuthorName, DateTime CreatedAtUtc, DateTime? EditedAtUtc, IReadOnlyList<CommentDto> Replies);
public record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Description, AttachmentCategory Category, Guid UploadedByUserId, string UploaderName, DateTime UploadedAtUtc);

public record GetNotesQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<NoteDto>>;
public record CreateNoteCommand : IRequest<NoteDto>
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Body { get; set; } = string.Empty;
    public NoteVisibility Visibility { get; set; }
}

public record UpdateNoteCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
}
public record DeleteNoteCommand(Guid Id) : IRequest<Unit>;
public record GetCommentsQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<CommentDto>>;
public record CreateCommentCommand : IRequest<CommentDto>
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public IReadOnlyList<Guid>? MentionedUserIds { get; set; }
}

public record UpdateCommentCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
}
public record DeleteCommentCommand(Guid Id) : IRequest<Unit>;
public record GetAttachmentsQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<AttachmentDto>>;
public record GetAttachmentQuery(Guid Id) : IRequest<AttachmentDto>;
public record UpdateAttachmentCommand(Guid Id, string Description, AttachmentCategory Category) : IRequest<Unit>;
public record DeleteAttachmentCommand(Guid Id) : IRequest<Unit>;

public static class EntityTypeMapper
{
    public static OwnerEntityType Parse(string entityType) => entityType.ToLowerInvariant() switch
    {
        "opportunities" or "opportunity" => OwnerEntityType.Opportunity,
        "gates" or "gateinstance" or "gate" => OwnerEntityType.GateInstance,
        "scope-items" or "scopeitem" => OwnerEntityType.ScopeItem,
        "contracts" or "contract" => OwnerEntityType.Contract,
        _ => throw new BusinessRuleException($"Unknown entity type '{entityType}'.")
    };
}

public sealed class CollaborationHandlers :
    IRequestHandler<GetNotesQuery, IReadOnlyList<NoteDto>>,
    IRequestHandler<CreateNoteCommand, NoteDto>,
    IRequestHandler<UpdateNoteCommand, Unit>,
    IRequestHandler<DeleteNoteCommand, Unit>,
    IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>,
    IRequestHandler<CreateCommentCommand, CommentDto>,
    IRequestHandler<UpdateCommentCommand, Unit>,
    IRequestHandler<DeleteCommentCommand, Unit>,
    IRequestHandler<GetAttachmentsQuery, IReadOnlyList<AttachmentDto>>,
    IRequestHandler<GetAttachmentQuery, AttachmentDto>,
    IRequestHandler<UpdateAttachmentCommand, Unit>,
    IRequestHandler<DeleteAttachmentCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;

    public CollaborationHandlers(IApplicationDbContext db, ICurrentUser user, IDateTime clock)
    {
        _db = db; _user = user; _clock = clock;
    }

    public async Task<IReadOnlyList<NoteDto>> Handle(GetNotesQuery r, CancellationToken ct)
    {
        var t = EntityTypeMapper.Parse(r.EntityType);
        return await _db.Notes.AsNoTracking().Include(n => n.CreatedByUser)
            .Where(n => n.EntityType == t && n.EntityId == r.EntityId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NoteDto(n.Id, n.Body, n.Visibility, n.CreatedByUserId, n.CreatedByUser.DisplayName, n.CreatedAtUtc, n.ModifiedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<NoteDto> Handle(CreateNoteCommand r, CancellationToken ct)
    {
        var n = new Note
        {
            EntityType = EntityTypeMapper.Parse(r.EntityType),
            EntityId = r.EntityId,
            Body = r.Body,
            Visibility = r.Visibility,
            CreatedByUserId = _user.UserId!.Value,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Notes.Add(n);
        await _db.SaveChangesAsync(ct);
        return new NoteDto(n.Id, n.Body, n.Visibility, n.CreatedByUserId, _user.DisplayName ?? "", n.CreatedAtUtc, null);
    }

    public async Task<Unit> Handle(UpdateNoteCommand r, CancellationToken ct)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Note), r.Id);
        if (n.CreatedByUserId != _user.UserId) throw new ForbiddenException();
        if (_clock.UtcNow - n.CreatedAtUtc > TimeSpan.FromMinutes(15)) throw new BusinessRuleException("Notes can only be edited within 15 minutes.");
        n.Body = r.Body;
        n.ModifiedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteNoteCommand r, CancellationToken ct)
    {
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Note), r.Id);
        n.IsDeleted = true; n.DeletedAtUtc = _clock.UtcNow; n.DeletedByUserId = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery r, CancellationToken ct)
    {
        var t = EntityTypeMapper.Parse(r.EntityType);
        var all = await _db.Comments.AsNoTracking().Include(c => c.CreatedByUser)
            .Where(c => c.EntityType == t && c.EntityId == r.EntityId)
            .OrderBy(c => c.CreatedAtUtc).ToListAsync(ct);
        CommentDto Map(Comment c) => new(c.Id, c.IsDeleted ? "[deleted]" : c.Body, c.ParentCommentId, c.CreatedByUserId, c.CreatedByUser.DisplayName, c.CreatedAtUtc, c.EditedAtUtc,
            all.Where(x => x.ParentCommentId == c.Id).Select(Map).ToList());
        return all.Where(c => c.ParentCommentId is null).Select(Map).ToList();
    }

    public async Task<CommentDto> Handle(CreateCommentCommand r, CancellationToken ct)
    {
        var c = new Comment
        {
            EntityType = EntityTypeMapper.Parse(r.EntityType),
            EntityId = r.EntityId,
            Body = r.Body,
            ParentCommentId = r.ParentCommentId,
            MentionedUserIdsJson = JsonSerializer.Serialize(r.MentionedUserIds ?? []),
            CreatedByUserId = _user.UserId!.Value,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Comments.Add(c);
        await _db.SaveChangesAsync(ct);
        return new CommentDto(c.Id, c.Body, c.ParentCommentId, c.CreatedByUserId, _user.DisplayName ?? "", c.CreatedAtUtc, null, []);
    }

    public async Task<Unit> Handle(UpdateCommentCommand r, CancellationToken ct)
    {
        var c = await _db.Comments.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Comment), r.Id);
        if (c.CreatedByUserId != _user.UserId) throw new ForbiddenException();
        c.Body = r.Body; c.EditedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteCommentCommand r, CancellationToken ct)
    {
        var c = await _db.Comments.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Comment), r.Id);
        c.IsDeleted = true; c.DeletedAtUtc = _clock.UtcNow; c.DeletedByUserId = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<AttachmentDto>> Handle(GetAttachmentsQuery r, CancellationToken ct)
    {
        var t = EntityTypeMapper.Parse(r.EntityType);
        return await _db.Attachments.AsNoTracking().Include(a => a.UploadedByUser)
            .Where(a => a.EntityType == t && a.EntityId == r.EntityId)
            .OrderByDescending(a => a.UploadedAtUtc)
            .Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.Description, a.Category, a.UploadedByUserId, a.UploadedByUser.DisplayName, a.UploadedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<AttachmentDto> Handle(GetAttachmentQuery r, CancellationToken ct)
    {
        var a = await _db.Attachments.Include(x => x.UploadedByUser).FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Attachment), r.Id);
        return new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.Description, a.Category, a.UploadedByUserId, a.UploadedByUser.DisplayName, a.UploadedAtUtc);
    }

    public async Task<Unit> Handle(UpdateAttachmentCommand r, CancellationToken ct)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Attachment), r.Id);
        if (string.IsNullOrWhiteSpace(r.Description)) throw new BusinessRuleException("Description is required.");
        a.Description = r.Description; a.Category = r.Category;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteAttachmentCommand r, CancellationToken ct)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Attachment), r.Id);
        a.IsDeleted = true; a.DeletedAtUtc = _clock.UtcNow; a.DeletedByUserId = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
