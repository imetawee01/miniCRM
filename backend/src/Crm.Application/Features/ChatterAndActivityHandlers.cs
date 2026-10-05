using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

// ---------------------------------------------------------------------------
// Phase 6 — chatter feed, activities, smart buttons
// ---------------------------------------------------------------------------

public record ChatterItemDto(
    string Kind, Guid Id, DateTime OccurredAtUtc,
    string AuthorName, string? Title, string Body,
    string? Meta);

public record GetChatterQuery(Guid OpportunityId, string? Kind) : IRequest<IReadOnlyList<ChatterItemDto>>;

public record SmartButtonsDto(
    int Attachments, int Emails, int Approvals, int Notes, int Comments, int ScopeItems, int Activities, Guid? ContractId);

public record GetSmartButtonsQuery(Guid OpportunityId) : IRequest<SmartButtonsDto>;

public record ActivityDto(
    Guid Id, Guid OpportunityId, ActivityType Type, string Summary,
    DateTime DueAtUtc, Guid AssignedUserId, string AssignedUserName,
    DateTime? DoneAtUtc, string? Note, DateTime CreatedAtUtc, string CreatedByName);

public record GetOpportunityActivitiesQuery(Guid OpportunityId) : IRequest<IReadOnlyList<ActivityDto>>;
public record GetMyActivitiesQuery(bool OverdueOnly, bool IncludeDone) : IRequest<IReadOnlyList<ActivityDto>>;
public record CreateActivityCommand(Guid OpportunityId, ActivityType Type, string Summary, DateTime DueAtUtc, Guid AssignedUserId, string? Note) : IRequest<ActivityDto>;
public record UpdateActivityCommand(Guid Id, ActivityType Type, string Summary, DateTime DueAtUtc, Guid AssignedUserId, string? Note) : IRequest<Unit>;
public record CompleteActivityCommand(Guid Id, string? Note) : IRequest<Unit>;
public record DeleteActivityCommand(Guid Id) : IRequest<Unit>;

public sealed class ChatterAndActivityHandlers :
    IRequestHandler<GetChatterQuery, IReadOnlyList<ChatterItemDto>>,
    IRequestHandler<GetSmartButtonsQuery, SmartButtonsDto>,
    IRequestHandler<GetOpportunityActivitiesQuery, IReadOnlyList<ActivityDto>>,
    IRequestHandler<GetMyActivitiesQuery, IReadOnlyList<ActivityDto>>,
    IRequestHandler<CreateActivityCommand, ActivityDto>,
    IRequestHandler<UpdateActivityCommand, Unit>,
    IRequestHandler<CompleteActivityCommand, Unit>,
    IRequestHandler<DeleteActivityCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IOpportunityVisibility _visibility;

    public ChatterAndActivityHandlers(
        IApplicationDbContext db, ICurrentUser user, IDateTime clock, IOpportunityVisibility visibility)
    {
        _db = db; _user = user; _clock = clock; _visibility = visibility;
    }

    public async Task<IReadOnlyList<ChatterItemDto>> Handle(GetChatterQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var kind = r.Kind?.ToLowerInvariant();
        var items = new List<ChatterItemDto>();

        if (kind is null or "note" or "all")
        {
            var notes = await _db.Notes.AsNoTracking()
                .Where(n => n.EntityType == OwnerEntityType.Opportunity && n.EntityId == r.OpportunityId && !n.IsDeleted)
                .Select(n => new ChatterItemDto("note", n.Id, n.CreatedAtUtc, n.CreatedByUser.DisplayName, null, n.Body, n.Visibility.ToString()))
                .ToListAsync(ct);
            items.AddRange(notes);
        }

        if (kind is null or "comment" or "all")
        {
            var comments = await _db.Comments.AsNoTracking()
                .Where(c => c.EntityType == OwnerEntityType.Opportunity && c.EntityId == r.OpportunityId && !c.IsDeleted)
                .Select(c => new ChatterItemDto("comment", c.Id, c.CreatedAtUtc, c.CreatedByUser.DisplayName, null, c.Body, null))
                .ToListAsync(ct);
            items.AddRange(comments);
        }

        if (kind is null or "audit" or "all")
        {
            var audits = await (
                from a in _db.AuditLogs.AsNoTracking()
                where a.OpportunityId == r.OpportunityId
                join u in _db.Users.AsNoTracking() on a.ActorUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby a.OccurredAtUtc descending
                select new ChatterItemDto(
                    "audit", a.Id, a.OccurredAtUtc,
                    u != null ? u.DisplayName : a.ActorRoleCode,
                    a.Action.ToString(), a.Description, a.ToValue)
            ).Take(100).ToListAsync(ct);
            items.AddRange(audits);
        }

        if (kind is null or "email" or "all")
        {
            var emails = await _db.GeneratedEmails.AsNoTracking()
                .Where(e => e.OpportunityId == r.OpportunityId)
                .Select(e => new ChatterItemDto("email", e.Id, e.GeneratedAtUtc, "", e.Subject, e.To, e.Status.ToString()))
                .ToListAsync(ct);
            items.AddRange(emails);
        }

        if (kind is null or "attachment" or "all")
        {
            var files = await _db.Attachments.AsNoTracking()
                .Where(a => a.EntityType == OwnerEntityType.Opportunity && a.EntityId == r.OpportunityId && !a.IsDeleted)
                .Select(a => new ChatterItemDto("attachment", a.Id, a.UploadedAtUtc, a.UploadedByUser.DisplayName, a.FileName, a.Description, a.Category.ToString()))
                .ToListAsync(ct);
            items.AddRange(files);
        }

        if (kind is null or "activity" or "all")
        {
            var acts = await _db.Activities.AsNoTracking()
                .Where(a => a.OpportunityId == r.OpportunityId)
                .Select(a => new ChatterItemDto("activity", a.Id, a.CreatedAtUtc, a.CreatedByUser.DisplayName, a.Type.ToString(), a.Summary,
                    a.DoneAtUtc != null ? "done" : a.DueAtUtc.ToString("o")))
                .ToListAsync(ct);
            items.AddRange(acts);
        }

        return items.OrderByDescending(i => i.OccurredAtUtc).Take(200).ToList();
    }

    public async Task<SmartButtonsDto> Handle(GetSmartButtonsQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var attachments = await _db.Attachments.CountAsync(a => a.EntityType == OwnerEntityType.Opportunity && a.EntityId == r.OpportunityId && !a.IsDeleted, ct);
        var emails = await _db.GeneratedEmails.CountAsync(e => e.OpportunityId == r.OpportunityId, ct);
        var approvals = await _db.GateInstances.CountAsync(g => g.OpportunityId == r.OpportunityId, ct);
        var notes = await _db.Notes.CountAsync(n => n.EntityType == OwnerEntityType.Opportunity && n.EntityId == r.OpportunityId && !n.IsDeleted, ct);
        var comments = await _db.Comments.CountAsync(c => c.EntityType == OwnerEntityType.Opportunity && c.EntityId == r.OpportunityId && !c.IsDeleted, ct);
        var scopeItems = await _db.ScopeItems.CountAsync(s => s.ScopeOfWork.OpportunityId == r.OpportunityId, ct);
        var activities = await _db.Activities.CountAsync(a => a.OpportunityId == r.OpportunityId && a.DoneAtUtc == null, ct);
        var contractId = await _db.Contracts.Where(c => c.OpportunityId == r.OpportunityId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        return new SmartButtonsDto(attachments, emails, approvals, notes, comments, scopeItems, activities, contractId);
    }

    public async Task<IReadOnlyList<ActivityDto>> Handle(GetOpportunityActivitiesQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        return await MapActivities(_db.Activities.AsNoTracking().Where(a => a.OpportunityId == r.OpportunityId), ct);
    }

    public async Task<IReadOnlyList<ActivityDto>> Handle(GetMyActivitiesQuery r, CancellationToken ct)
    {
        if (_user.UserId is not Guid me) return [];
        var q = _db.Activities.AsNoTracking().Where(a => a.AssignedUserId == me);
        if (!r.IncludeDone) q = q.Where(a => a.DoneAtUtc == null);
        if (r.OverdueOnly) q = q.Where(a => a.DoneAtUtc == null && a.DueAtUtc < _clock.UtcNow);
        return await MapActivities(q, ct);
    }

    public async Task<ActivityDto> Handle(CreateActivityCommand r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        if (_user.UserId is not Guid me) throw new UnauthorizedException("Not authenticated.");
        var a = new Activity
        {
            OpportunityId = r.OpportunityId,
            Type = r.Type,
            Summary = r.Summary.Trim(),
            DueAtUtc = DateTime.SpecifyKind(r.DueAtUtc, DateTimeKind.Utc),
            AssignedUserId = r.AssignedUserId,
            Note = r.Note,
            CreatedByUserId = me,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Activities.Add(a);
        await _db.SaveChangesAsync(ct);
        return (await MapActivities(_db.Activities.AsNoTracking().Where(x => x.Id == a.Id), ct)).Single();
    }

    public async Task<Unit> Handle(UpdateActivityCommand r, CancellationToken ct)
    {
        var a = await _db.Activities.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Activity), r.Id);
        await _visibility.EnsureCanAccessAsync(a.OpportunityId, ct);
        a.Type = r.Type;
        a.Summary = r.Summary.Trim();
        a.DueAtUtc = DateTime.SpecifyKind(r.DueAtUtc, DateTimeKind.Utc);
        a.AssignedUserId = r.AssignedUserId;
        a.Note = r.Note;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(CompleteActivityCommand r, CancellationToken ct)
    {
        var a = await _db.Activities.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Activity), r.Id);
        await _visibility.EnsureCanAccessAsync(a.OpportunityId, ct);
        a.DoneAtUtc = _clock.UtcNow;
        if (!string.IsNullOrWhiteSpace(r.Note)) a.Note = r.Note;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteActivityCommand r, CancellationToken ct)
    {
        var a = await _db.Activities.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Activity), r.Id);
        await _visibility.EnsureCanAccessAsync(a.OpportunityId, ct);
        _db.Activities.Remove(a);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static async Task<IReadOnlyList<ActivityDto>> MapActivities(IQueryable<Activity> q, CancellationToken ct) =>
        await q.OrderBy(a => a.DoneAtUtc != null).ThenBy(a => a.DueAtUtc)
            .Select(a => new ActivityDto(
                a.Id, a.OpportunityId, a.Type, a.Summary, a.DueAtUtc,
                a.AssignedUserId, a.AssignedUser.DisplayName,
                a.DoneAtUtc, a.Note, a.CreatedAtUtc, a.CreatedByUser.DisplayName))
            .ToListAsync(ct);
}
