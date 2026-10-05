using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

public record GateInstanceDto(
    Guid Id, Guid OpportunityId, string OpportunityNumber, string OpportunityName, string CustomerName,
    Guid GateId, string GateCode, string GateNameEn, string GateNameAr, string AllowedDecisions,
    GateState State, string AssignedRoleCode, Guid? AssignedUserId, string? AssignedUserName,
    DateTime OpenedAtUtc, DateTime? DecidedAtUtc, Guid? DecidedByUserId, string? DecidedByName,
    string? Decision, string? Reason, int Round, bool RequiresReasonOnReject, bool RequiresAttachmentOnApprove);
public record PendingCountDto(int Count);

public record GetOpportunityGatesQuery(Guid OpportunityId) : IRequest<IReadOnlyList<GateInstanceDto>>;
public record GetGateInstanceQuery(Guid GateInstanceId) : IRequest<GateInstanceDto>;
public record DecideGateCommand(Guid GateInstanceId, string Decision, string? Reason, string? NoteBody, IReadOnlyList<Guid>? AttachmentIds) : IRequest<Unit>;
public record ReassignGateCommand(Guid GateInstanceId, Guid AssignedUserId) : IRequest<Unit>;
public record GetPendingApprovalsQuery : PagedQuery, IRequest<PagedResult<GateInstanceDto>>;
public record GetPendingCountQuery : IRequest<PendingCountDto>;
public record WorkflowGateDto(Guid Id, string Code, string NameEn, string NameAr, int SortOrder, string ResponsibleRoleCode, string AllowedDecisions, bool RequiresReasonOnReject, bool RequiresAttachmentOnApprove, bool IsActive);
public record GetWorkflowGatesQuery : IRequest<IReadOnlyList<WorkflowGateDto>>;
public record UpdateWorkflowGateCommand(Guid Id, string ResponsibleRoleCode, string AllowedDecisions, bool RequiresReasonOnReject, bool RequiresAttachmentOnApprove, bool IsActive) : IRequest<Unit>;
public record TransitionDto(Guid Id, Guid FromStatusId, string FromName, Guid ToStatusId, string ToName, string RequiredRoleCode, bool RequiresReason);
public record GetWorkflowTransitionsQuery : IRequest<IReadOnlyList<TransitionDto>>;

public sealed class GateHandlers :
    IRequestHandler<GetOpportunityGatesQuery, IReadOnlyList<GateInstanceDto>>,
    IRequestHandler<GetGateInstanceQuery, GateInstanceDto>,
    IRequestHandler<DecideGateCommand, Unit>,
    IRequestHandler<ReassignGateCommand, Unit>,
    IRequestHandler<GetPendingApprovalsQuery, PagedResult<GateInstanceDto>>,
    IRequestHandler<GetPendingCountQuery, PendingCountDto>,
    IRequestHandler<GetWorkflowGatesQuery, IReadOnlyList<WorkflowGateDto>>,
    IRequestHandler<UpdateWorkflowGateCommand, Unit>,
    IRequestHandler<GetWorkflowTransitionsQuery, IReadOnlyList<TransitionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IWorkflowEngine _workflow;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;

    public GateHandlers(IApplicationDbContext db, IWorkflowEngine workflow, ICurrentUser user, IDateTime clock)
    {
        _db = db; _workflow = workflow; _user = user; _clock = clock;
    }

    public async Task<IReadOnlyList<GateInstanceDto>> Handle(GetOpportunityGatesQuery r, CancellationToken ct) =>
        (await Query().Where(g => g.OpportunityId == r.OpportunityId).OrderBy(g => g.OpenedAtUtc).ToListAsync(ct)).Select(Map).ToList();

    public async Task<GateInstanceDto> Handle(GetGateInstanceQuery r, CancellationToken ct) =>
        Map(await Query().FirstOrDefaultAsync(g => g.Id == r.GateInstanceId, ct) ?? throw new NotFoundException(nameof(GateInstance), r.GateInstanceId));

    public async Task<Unit> Handle(DecideGateCommand r, CancellationToken ct)
    {
        var instance = await _db.GateInstances.Include(g => g.Gate).Include(g => g.Opportunity).ThenInclude(o => o.Status)
            .FirstOrDefaultAsync(g => g.Id == r.GateInstanceId, ct) ?? throw new NotFoundException(nameof(GateInstance), r.GateInstanceId);

        var isPositive = r.Decision is "Approve" or "Approved" or "Qualified" or "Passed";
        if (instance.Gate.RequiresAttachmentOnApprove && isPositive)
        {
            var hasNew = r.AttachmentIds is { Count: > 0 };
            var hasExisting = await _db.Attachments.AnyAsync(a => a.EntityType == OwnerEntityType.GateInstance && a.EntityId == instance.Id, ct);
            if (!hasNew && !hasExisting)
                throw new BusinessRuleException("This gate requires at least one attachment before it can be approved.",
                    new Dictionary<string, string[]> { ["attachmentIds"] = ["Upload a supporting document to the gate before approving."] });
        }

        await _workflow.ApplyGateDecisionAsync(instance, r.Decision, r.Reason, _user.UserId!.Value, _user.Roles, ct);

        if (!string.IsNullOrWhiteSpace(r.NoteBody))
        {
            _db.Notes.Add(new Note
            {
                EntityType = OwnerEntityType.GateInstance,
                EntityId = instance.Id,
                Body = r.NoteBody,
                Visibility = NoteVisibility.Shared,
                CreatedByUserId = _user.UserId.Value,
                CreatedAtUtc = _clock.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ReassignGateCommand r, CancellationToken ct)
    {
        var g = await _db.GateInstances.FirstOrDefaultAsync(x => x.Id == r.GateInstanceId, ct) ?? throw new NotFoundException(nameof(GateInstance), r.GateInstanceId);
        g.AssignedUserId = r.AssignedUserId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<PagedResult<GateInstanceDto>> Handle(GetPendingApprovalsQuery r, CancellationToken ct)
    {
        var q = PendingQuery();
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(g => g.OpenedAtUtc).Skip(r.Skip).Take(r.Take).ToListAsync(ct);
        return PagedResult<GateInstanceDto>.Create(items.Select(Map).ToList(), r.Page, r.Take, total);
    }

    public async Task<PendingCountDto> Handle(GetPendingCountQuery r, CancellationToken ct) => new(await PendingQuery().CountAsync(ct));

    public async Task<IReadOnlyList<WorkflowGateDto>> Handle(GetWorkflowGatesQuery r, CancellationToken ct) =>
        await _db.WorkflowGates.AsNoTracking().OrderBy(g => g.SortOrder)
            .Select(g => new WorkflowGateDto(g.Id, g.Code, g.NameEn, g.NameAr, g.SortOrder, g.ResponsibleRoleCode, g.AllowedDecisions, g.RequiresReasonOnReject, g.RequiresAttachmentOnApprove, g.IsActive))
            .ToListAsync(ct);

    public async Task<Unit> Handle(UpdateWorkflowGateCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var g = await _db.WorkflowGates.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(WorkflowGate), r.Id);
        g.ResponsibleRoleCode = r.ResponsibleRoleCode;
        g.AllowedDecisions = r.AllowedDecisions;
        g.RequiresReasonOnReject = r.RequiresReasonOnReject;
        g.RequiresAttachmentOnApprove = r.RequiresAttachmentOnApprove;
        g.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<TransitionDto>> Handle(GetWorkflowTransitionsQuery r, CancellationToken ct) =>
        await _db.StatusTransitions.AsNoTracking().Include(t => t.FromStatus).Include(t => t.ToStatus)
            .Select(t => new TransitionDto(t.Id, t.FromStatusId, t.FromStatus.NameEn, t.ToStatusId, t.ToStatus.NameEn, t.RequiredRoleCode, t.RequiresReason))
            .ToListAsync(ct);

    private IQueryable<GateInstance> Query() =>
        _db.GateInstances.AsNoTracking().Include(g => g.Gate)
            .Include(g => g.Opportunity).ThenInclude(o => o.Customer)
            .Include(g => g.AssignedUser).Include(g => g.DecidedByUser);

    private IQueryable<GateInstance> PendingQuery()
    {
        var q = Query().Where(g => g.State == GateState.Pending);
        if (_user.IsAdmin) return q;
        return q.WhereAssignedTo(_user.UserId, _user.Roles);
    }

    internal static GateInstanceDto Map(GateInstance g) => new(
        g.Id, g.OpportunityId, g.Opportunity.OpportunityNumber, g.Opportunity.Name, g.Opportunity.Customer?.NameEn ?? "",
        g.GateId, g.Gate.Code, g.Gate.NameEn, g.Gate.NameAr, g.Gate.AllowedDecisions,
        g.State, g.AssignedRoleCode, g.AssignedUserId, g.AssignedUser?.DisplayName,
        g.OpenedAtUtc, g.DecidedAtUtc, g.DecidedByUserId, g.DecidedByUser?.DisplayName,
        g.Decision, g.Reason, g.Round, g.Gate.RequiresReasonOnReject, g.Gate.RequiresAttachmentOnApprove);
}

public record SetQualificationRouteCommand(Guid OpportunityId, bool RequiresQualificationMeeting) : IRequest<Unit>;
public record CreateMeetingCommand(Guid OpportunityId, DateTime ScheduledAtUtc, string? Location, string? MeetingLink, string? Agenda) : IRequest<Guid>;
public record GetMeetingQuery(Guid OpportunityId) : IRequest<QualificationMeeting?>;
public record UpdateMeetingCommand(Guid OpportunityId, DateTime ScheduledAtUtc, string? Location, string? MeetingLink, string? Agenda) : IRequest<Unit>;
public record SaveMinutesCommand(Guid OpportunityId, string Minutes) : IRequest<Unit>;
public record AddAttendeeCommand(Guid OpportunityId, Guid UserId, Guid? ServiceLineId) : IRequest<Unit>;
public record SetAttendeeResponseCommand(Guid AttendeeId, AttendeeResponse Response) : IRequest<Unit>;

public sealed class QualificationHandlers :
    IRequestHandler<SetQualificationRouteCommand, Unit>,
    IRequestHandler<CreateMeetingCommand, Guid>,
    IRequestHandler<GetMeetingQuery, QualificationMeeting?>,
    IRequestHandler<UpdateMeetingCommand, Unit>,
    IRequestHandler<SaveMinutesCommand, Unit>,
    IRequestHandler<AddAttendeeCommand, Unit>,
    IRequestHandler<SetAttendeeResponseCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IWorkflowEngine _workflow;
    private readonly IEmailComposer _email;
    private readonly ICurrentUser _user;

    public QualificationHandlers(IApplicationDbContext db, IWorkflowEngine workflow, IEmailComposer email, ICurrentUser user)
    {
        _db = db; _workflow = workflow; _email = email; _user = user;
    }

    public async Task<Unit> Handle(SetQualificationRouteCommand r, CancellationToken ct)
    {
        var o = await LoadOpp(r.OpportunityId, ct);
        o.RequiresQualificationMeeting = r.RequiresQualificationMeeting;
                
        if (r.RequiresQualificationMeeting)
        {
            await _email.ComposeAsync(EmailTemplateCodes.QualMeetingRequest, o, _user.UserId!.Value, ct: ct);
            await _workflow.OpenGateAsync(o, GateCodes.QualMeeting, ct: ct);
        }
        else
        {
            await _email.ComposeAsync(EmailTemplateCodes.QualRequest, o, _user.UserId!.Value, ct: ct);
            await _workflow.OpenGateAsync(o, GateCodes.QualDecision, ct: ct);
        }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Guid> Handle(CreateMeetingCommand r, CancellationToken ct)
    {
        var meeting = new QualificationMeeting
        {
            OpportunityId = r.OpportunityId,
            ScheduledAtUtc = DateTime.SpecifyKind(r.ScheduledAtUtc, DateTimeKind.Utc),
            Location = r.Location,
            MeetingLink = r.MeetingLink,
            Agenda = r.Agenda
        };
        _db.QualificationMeetings.Add(meeting);
        await _db.SaveChangesAsync(ct);
        return meeting.Id;
    }

    public Task<QualificationMeeting?> Handle(GetMeetingQuery r, CancellationToken ct) =>
        _db.QualificationMeetings.Include(m => m.Attendees).ThenInclude(a => a.User)
            .Where(m => m.OpportunityId == r.OpportunityId).OrderByDescending(m => m.ScheduledAtUtc).FirstOrDefaultAsync(ct);

    public async Task<Unit> Handle(UpdateMeetingCommand r, CancellationToken ct)
    {
        var m = await _db.QualificationMeetings.OrderByDescending(x => x.ScheduledAtUtc).FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(QualificationMeeting), r.OpportunityId);
        m.ScheduledAtUtc = DateTime.SpecifyKind(r.ScheduledAtUtc, DateTimeKind.Utc);
        m.Location = r.Location; m.MeetingLink = r.MeetingLink; m.Agenda = r.Agenda;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SaveMinutesCommand r, CancellationToken ct)
    {
        var m = await _db.QualificationMeetings.OrderByDescending(x => x.ScheduledAtUtc).FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(QualificationMeeting), r.OpportunityId);
        m.MinutesOfMeeting = r.Minutes;
        m.HeldAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(AddAttendeeCommand r, CancellationToken ct)
    {
        var m = await _db.QualificationMeetings.OrderByDescending(x => x.ScheduledAtUtc).FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(QualificationMeeting), r.OpportunityId);
        _db.QualificationMeetingAttendees.Add(new QualificationMeetingAttendee { MeetingId = m.Id, UserId = r.UserId, ServiceLineId = r.ServiceLineId });
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetAttendeeResponseCommand r, CancellationToken ct)
    {
        var a = await _db.QualificationMeetingAttendees.FirstOrDefaultAsync(x => x.Id == r.AttendeeId, ct)
            ?? throw new NotFoundException(nameof(QualificationMeetingAttendee), r.AttendeeId);
        a.Response = r.Response;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<Opportunity> LoadOpp(Guid id, CancellationToken ct) =>
        await _db.Opportunities.Include(o => o.Customer).Include(o => o.Status).FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new NotFoundException(nameof(Opportunity), id);
}
