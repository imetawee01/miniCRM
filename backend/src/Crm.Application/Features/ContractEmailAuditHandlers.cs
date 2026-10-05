using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

/// <summary>Flat contract payload — avoids EF navigation cycles (Opportunity ↔ Customer) on serialize.</summary>
public record ContractDto(
    Guid Id,
    Guid OpportunityId,
    string? OpportunityNumber,
    string? OpportunityName,
    string? CustomerName,
    string ContractNumber,
    ContractStatus ContractStatus,
    decimal ContractValueSar,
    DateTime? StartDate,
    DateTime? EndDate,
    int? DurationMonths,
    DateTime? SignedAtUtc,
    string? SignedByCustomerRepresentative,
    string? PaymentTerms,
    string? Notes);

public record ContractListQuery : PagedQuery, IRequest<PagedResult<ContractDto>>
{
    public ContractStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal? ValueFrom { get; set; }
    public decimal? ValueTo { get; set; }
    public DateTime? SignedFrom { get; set; }
    public DateTime? SignedTo { get; set; }
    public int? ExpiringWithinDays { get; set; }
}

public record GetContractQuery(Guid Id) : IRequest<ContractDto>;
public record CreateContractCommand(Guid OpportunityId) : IRequest<ContractDto>;
public record UpdateContractCommand(Guid Id, decimal ContractValueSar, DateTime? StartDate, DateTime? EndDate, int? DurationMonths, string? PaymentTerms, string? Notes) : IRequest<Unit>;
public record ChangeContractStatusCommand(Guid Id, ContractStatus ToStatus, string? Reason) : IRequest<Unit>;
public record GetRoundsQuery(Guid ContractId) : IRequest<IReadOnlyList<ContractNegotiationRound>>;
public record CreateRoundCommand(Guid ContractId, string RequestedChanges, string? OurPosition, string? CustomerPosition) : IRequest<Guid>;
public record UpdateRoundCommand(Guid RoundId, string RequestedChanges, string? OurPosition, string? CustomerPosition) : IRequest<Unit>;
public record CloseRoundCommand(Guid RoundId, NegotiationRoundStatus Status) : IRequest<Unit>;
public record GetMilestonesQuery(Guid ContractId) : IRequest<IReadOnlyList<ContractMilestone>>;
public record CreateMilestoneCommand(Guid ContractId, string Title, DateTime DueDate, decimal? AmountSar) : IRequest<Guid>;
public record UpdateMilestoneCommand(Guid Id, string Title, DateTime DueDate, decimal? AmountSar, bool IsCompleted) : IRequest<Unit>;
public record SignContractCommand(Guid Id, DateTime SignedAtUtc, string Signatory, Guid? AttachmentId) : IRequest<Unit>;

public sealed class ContractHandlers :
    IRequestHandler<ContractListQuery, PagedResult<ContractDto>>,
    IRequestHandler<GetContractQuery, ContractDto>,
    IRequestHandler<CreateContractCommand, ContractDto>,
    IRequestHandler<UpdateContractCommand, Unit>,
    IRequestHandler<ChangeContractStatusCommand, Unit>,
    IRequestHandler<GetRoundsQuery, IReadOnlyList<ContractNegotiationRound>>,
    IRequestHandler<CreateRoundCommand, Guid>,
    IRequestHandler<UpdateRoundCommand, Unit>,
    IRequestHandler<CloseRoundCommand, Unit>,
    IRequestHandler<GetMilestonesQuery, IReadOnlyList<ContractMilestone>>,
    IRequestHandler<CreateMilestoneCommand, Guid>,
    IRequestHandler<UpdateMilestoneCommand, Unit>,
    IRequestHandler<SignContractCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly INumberGenerator _numbers;
    private readonly IWorkflowEngine _workflow;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;
    private readonly IOpportunityVisibility _visibility;

    public ContractHandlers(IApplicationDbContext db, INumberGenerator numbers, IWorkflowEngine workflow, ICurrentUser user, IDateTime clock, IAuditWriter audit, IOpportunityVisibility visibility)
    {
        _db = db; _numbers = numbers; _workflow = workflow; _user = user; _clock = clock; _audit = audit; _visibility = visibility;
    }

    public async Task<PagedResult<ContractDto>> Handle(ContractListQuery q, CancellationToken ct)
    {
        var query = _db.Contracts.AsNoTracking().Include(c => c.Opportunity).ThenInclude(o => o.Customer).AsQueryable();
        // Restrict to opportunities the user can see
        var visibleOppIds = _visibility.Apply(_db.Opportunities.AsNoTracking()).Select(o => o.Id);
        query = query.Where(c => visibleOppIds.Contains(c.OpportunityId));
        if (q.Status is ContractStatus s) query = query.Where(c => c.ContractStatus == s);
        if (q.CustomerId is Guid cid) query = query.Where(c => c.Opportunity.CustomerId == cid);
        if (q.ValueFrom is decimal vf) query = query.Where(c => c.ContractValueSar >= vf);
        if (q.ValueTo is decimal vt) query = query.Where(c => c.ContractValueSar <= vt);
        if (q.SignedFrom is DateTime sf) query = query.Where(c => c.SignedAtUtc >= sf);
        if (q.SignedTo is DateTime st) query = query.Where(c => c.SignedAtUtc <= st);
        if (q.ExpiringWithinDays is int days)
        {
            var until = _clock.UtcNow.AddDays(days);
            query = query.Where(c => c.EndDate != null && c.EndDate <= until && c.EndDate >= _clock.UtcNow);
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAtUtc).Skip(q.Skip).Take(q.Take).ToListAsync(ct);
        var canView = _visibility.CanViewPricingFields;
        var dtos = items.Select(c => ToDto(c, canView)).ToList();
        return PagedResult<ContractDto>.Create(dtos, q.Page, q.Take, total);
    }

    public async Task<ContractDto> Handle(GetContractQuery r, CancellationToken ct)
    {
        var c = await _db.Contracts.AsNoTracking()
            .Include(c => c.Opportunity).ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(c => c.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Contract), r.Id);
        await _visibility.EnsureCanAccessAsync(c.OpportunityId, ct);
        return ToDto(c, _visibility.CanViewPricingFields);
    }

    public async Task<ContractDto> Handle(CreateContractCommand r, CancellationToken ct)
    {
        var o = await _db.Opportunities.Include(x => x.Status).Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.OpportunityId);
        if (o.Status.Code != StatusCodes.Won) throw new BusinessRuleException("Contract can only be created from a Won opportunity.");
        if (await _db.Contracts.AnyAsync(c => c.OpportunityId == o.Id, ct)) throw new ConflictException("A contract already exists.");
        var c = new Contract
        {
            OpportunityId = o.Id,
            ContractNumber = await _numbers.NextContractNumberAsync(ct),
            ContractStatus = ContractStatus.Won,
            ContractValueSar = o.ExpectedValueSar
        };
        _db.Contracts.Add(c);
        await _db.SaveChangesAsync(ct);
        c.Opportunity = o;
        return ToDto(c, _visibility.CanViewPricingFields);
    }

    private static ContractDto ToDto(Contract c, bool canViewPricing) =>
        new(
            c.Id,
            c.OpportunityId,
            c.Opportunity?.OpportunityNumber,
            c.Opportunity?.Name,
            c.Opportunity?.Customer?.NameEn,
            c.ContractNumber,
            c.ContractStatus,
            canViewPricing ? c.ContractValueSar : 0m,
            c.StartDate,
            c.EndDate,
            c.DurationMonths,
            c.SignedAtUtc,
            c.SignedByCustomerRepresentative,
            canViewPricing ? c.PaymentTerms : null,
            c.Notes);

    public async Task<Unit> Handle(UpdateContractCommand r, CancellationToken ct)
    {
        var c = await Load(r.Id, ct);
        c.ContractValueSar = r.ContractValueSar; c.StartDate = r.StartDate; c.EndDate = r.EndDate;
        c.DurationMonths = r.DurationMonths; c.PaymentTerms = r.PaymentTerms; c.Notes = r.Notes;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ChangeContractStatusCommand r, CancellationToken ct)
    {
        var c = await _db.Contracts.Include(x => x.Opportunity).ThenInclude(o => o.Status).FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Contract), r.Id);
        var o = c.Opportunity;
        if (r.ToStatus == ContractStatus.ContractNegotiation)
        {
            var st = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.ContractNegotiation, ct);
            await _workflow.ChangeStatusAsync(o, st.Id, r.Reason, _user.Roles, ct);
            await _workflow.OpenGateAsync(o, GateCodes.ContractSignoff, ct: ct);
        }
        else if (r.ToStatus == ContractStatus.ContractSigned)
        {
            var st = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.ContractSigned, ct);
            await _workflow.ChangeStatusAsync(o, st.Id, r.Reason, _user.Roles, ct);
        }
        c.ContractStatus = r.ToStatus;
        _audit.Add("Contract", c.Id, AuditAction.ContractStageChanged, r.ToStatus.ToString(), o.Id);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<ContractNegotiationRound>> Handle(GetRoundsQuery r, CancellationToken ct) =>
        await _db.ContractNegotiationRounds.AsNoTracking().Where(x => x.ContractId == r.ContractId).OrderBy(x => x.RoundNumber).ToListAsync(ct);

    public async Task<Guid> Handle(CreateRoundCommand r, CancellationToken ct)
    {
        var max = await _db.ContractNegotiationRounds.Where(x => x.ContractId == r.ContractId).Select(x => (int?)x.RoundNumber).MaxAsync(ct) ?? 0;
        var round = new ContractNegotiationRound
        {
            ContractId = r.ContractId, RoundNumber = max + 1, RequestedChanges = r.RequestedChanges,
            OurPosition = r.OurPosition, CustomerPosition = r.CustomerPosition,
            OpenedAtUtc = _clock.UtcNow, OpenedByUserId = _user.UserId!.Value
        };
        _db.ContractNegotiationRounds.Add(round);
        await _db.SaveChangesAsync(ct);
        return round.Id;
    }

    public async Task<Unit> Handle(UpdateRoundCommand r, CancellationToken ct)
    {
        var x = await _db.ContractNegotiationRounds.FirstOrDefaultAsync(n => n.Id == r.RoundId, ct) ?? throw new NotFoundException(nameof(ContractNegotiationRound), r.RoundId);
        x.RequestedChanges = r.RequestedChanges; x.OurPosition = r.OurPosition; x.CustomerPosition = r.CustomerPosition;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(CloseRoundCommand r, CancellationToken ct)
    {
        var x = await _db.ContractNegotiationRounds.FirstOrDefaultAsync(n => n.Id == r.RoundId, ct) ?? throw new NotFoundException(nameof(ContractNegotiationRound), r.RoundId);
        x.Status = r.Status; x.ClosedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<ContractMilestone>> Handle(GetMilestonesQuery r, CancellationToken ct) =>
        await _db.ContractMilestones.AsNoTracking().Where(m => m.ContractId == r.ContractId).OrderBy(m => m.DueDate).ToListAsync(ct);

    public async Task<Guid> Handle(CreateMilestoneCommand r, CancellationToken ct)
    {
        var m = new ContractMilestone { ContractId = r.ContractId, Title = r.Title, DueDate = DateTime.SpecifyKind(r.DueDate, DateTimeKind.Utc), AmountSar = r.AmountSar };
        _db.ContractMilestones.Add(m);
        await _db.SaveChangesAsync(ct);
        return m.Id;
    }

    public async Task<Unit> Handle(UpdateMilestoneCommand r, CancellationToken ct)
    {
        var m = await _db.ContractMilestones.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(ContractMilestone), r.Id);
        m.Title = r.Title; m.DueDate = DateTime.SpecifyKind(r.DueDate, DateTimeKind.Utc); m.AmountSar = r.AmountSar;
        if (r.IsCompleted && !m.IsCompleted) m.CompletedAtUtc = _clock.UtcNow;
        m.IsCompleted = r.IsCompleted;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SignContractCommand r, CancellationToken ct)
    {
        var c = await _db.Contracts.Include(x => x.Opportunity).FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Contract), r.Id);
        var approved = await _db.GateInstances.Include(g => g.Gate)
            .AnyAsync(g => g.OpportunityId == c.OpportunityId && g.Gate.Code == GateCodes.ContractSignoff && g.State == GateState.Approved, ct);
        if (!approved) throw new BusinessRuleException("Contract sign-off gate must be approved first.");
        c.SignedAtUtc = DateTime.SpecifyKind(r.SignedAtUtc, DateTimeKind.Utc);
        c.SignedByCustomerRepresentative = r.Signatory;
        c.ContractStatus = ContractStatus.ContractSigned;
        var st = await _db.Statuses.FirstAsync(s => s.Code == StatusCodes.ContractSigned, ct);
        await _workflow.ChangeStatusAsync(c.Opportunity, st.Id, "Signed", _user.Roles, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<Contract> Load(Guid id, CancellationToken ct) =>
        await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(Contract), id);
}

public record GetEmailsQuery(Guid OpportunityId) : IRequest<IReadOnlyList<GeneratedEmail>>;
public record GetEmailQuery(Guid Id) : IRequest<GeneratedEmail>;
public record RegenerateEmailCommand(Guid Id) : IRequest<GeneratedEmail>;
public record MarkEmailSentCommand(Guid Id) : IRequest<Unit>;
public record GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplate>>;
public record UpdateEmailTemplateCommand(string Code, string SubjectTemplate, string BodyTemplateHtml, string DefaultTo, string DefaultCc, bool IsActive) : IRequest<Unit>;
public record PreviewEmailCommand(string Code, Guid OpportunityId) : IRequest<GeneratedEmail>;

public sealed class EmailHandlers :
    IRequestHandler<GetEmailsQuery, IReadOnlyList<GeneratedEmail>>,
    IRequestHandler<GetEmailQuery, GeneratedEmail>,
    IRequestHandler<RegenerateEmailCommand, GeneratedEmail>,
    IRequestHandler<MarkEmailSentCommand, Unit>,
    IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplate>>,
    IRequestHandler<UpdateEmailTemplateCommand, Unit>,
    IRequestHandler<PreviewEmailCommand, GeneratedEmail>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailComposer _composer;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;

    public EmailHandlers(IApplicationDbContext db, IEmailComposer composer, ICurrentUser user, IDateTime clock, IAuditWriter audit)
    {
        _db = db; _composer = composer; _user = user; _clock = clock; _audit = audit;
    }

    public async Task<IReadOnlyList<GeneratedEmail>> Handle(GetEmailsQuery r, CancellationToken ct) =>
        await _db.GeneratedEmails.AsNoTracking().Where(e => e.OpportunityId == r.OpportunityId).OrderByDescending(e => e.GeneratedAtUtc).ToListAsync(ct);

    public async Task<GeneratedEmail> Handle(GetEmailQuery r, CancellationToken ct) =>
        await _db.GeneratedEmails.AsNoTracking().FirstOrDefaultAsync(e => e.Id == r.Id, ct) ?? throw new NotFoundException(nameof(GeneratedEmail), r.Id);

    public async Task<GeneratedEmail> Handle(RegenerateEmailCommand r, CancellationToken ct)
    {
        var existing = await _db.GeneratedEmails.FirstOrDefaultAsync(e => e.Id == r.Id, ct) ?? throw new NotFoundException(nameof(GeneratedEmail), r.Id);
        var o = await _db.Opportunities.Include(x => x.Customer).FirstAsync(x => x.Id == existing.OpportunityId, ct);
        var next = await _composer.ComposeAsync(existing.TemplateCode, o, _user.UserId!.Value, ct: ct);
        await _db.SaveChangesAsync(ct);
        return next;
    }

    public async Task<Unit> Handle(MarkEmailSentCommand r, CancellationToken ct)
    {
        var e = await _db.GeneratedEmails.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(GeneratedEmail), r.Id);
        e.Status = GeneratedEmailStatus.Sent; e.SentAtUtc = _clock.UtcNow;
        _audit.Add("GeneratedEmail", e.Id, AuditAction.EmailSent, "Marked sent manually", e.OpportunityId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<EmailTemplate>> Handle(GetEmailTemplatesQuery r, CancellationToken ct) =>
        await _db.EmailTemplates.AsNoTracking().OrderBy(t => t.Code).ToListAsync(ct);

    public async Task<Unit> Handle(UpdateEmailTemplateCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var t = await _db.EmailTemplates.FirstOrDefaultAsync(x => x.Code == r.Code, ct) ?? throw new NotFoundException(nameof(EmailTemplate), r.Code);
        t.SubjectTemplate = r.SubjectTemplate; t.BodyTemplateHtml = r.BodyTemplateHtml;
        t.DefaultTo = r.DefaultTo; t.DefaultCc = r.DefaultCc; t.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<GeneratedEmail> Handle(PreviewEmailCommand r, CancellationToken ct)
    {
        var o = await _db.Opportunities.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.OpportunityId);
        var email = await _composer.ComposeAsync(r.Code, o, _user.UserId!.Value, ct: ct);
        await _db.SaveChangesAsync(ct);
        return email;
    }
}

public record NotificationDto(Guid Id, string Title, string Body, string? MessageKey, string? ParamsJson, string? LinkUrl, Guid? OpportunityId, bool IsRead, DateTime CreatedAtUtc, NotificationType Type);
public record UnreadCountDto(int Count);
public record GetNotificationsQuery : PagedQuery, IRequest<PagedResult<NotificationDto>>;
public record MarkReadCommand(Guid Id) : IRequest<Unit>;
public record MarkAllReadCommand : IRequest<Unit>;
public record UnreadCountQuery : IRequest<UnreadCountDto>;

public sealed class NotificationHandlers :
    IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>,
    IRequestHandler<MarkReadCommand, Unit>,
    IRequestHandler<MarkAllReadCommand, Unit>,
    IRequestHandler<UnreadCountQuery, UnreadCountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    public NotificationHandlers(IApplicationDbContext db, ICurrentUser user, IDateTime clock) { _db = db; _user = user; _clock = clock; }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery q, CancellationToken ct)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == _user.UserId)
            .OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAtUtc);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(q.Skip).Take(q.Take)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.MessageKey, n.ParamsJson, n.LinkUrl, n.OpportunityId, n.IsRead, n.CreatedAtUtc, n.Type)).ToListAsync(ct);
        return PagedResult<NotificationDto>.Create(items, q.Page, q.Take, total);
    }

    public async Task<Unit> Handle(MarkReadCommand r, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == r.Id && x.UserId == _user.UserId, ct)
            ?? throw new NotFoundException(nameof(AppNotification), r.Id);
        n.IsRead = true; n.ReadAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(MarkAllReadCommand r, CancellationToken ct)
    {
        var list = await _db.Notifications.Where(n => n.UserId == _user.UserId && !n.IsRead).ToListAsync(ct);
        foreach (var n in list) { n.IsRead = true; n.ReadAtUtc = _clock.UtcNow; }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<UnreadCountDto> Handle(UnreadCountQuery r, CancellationToken ct) =>
        new(await _db.Notifications.CountAsync(n => n.UserId == _user.UserId && !n.IsRead, ct));
}

public record AuditListQuery : PagedQuery, IRequest<PagedResult<AuditLog>>
{
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? ActorUserId { get; set; }
    public AuditAction? Action { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
public record GetOpportunityAuditQuery(Guid OpportunityId) : IRequest<IReadOnlyList<AuditLog>>;

public sealed class AuditHandlers :
    IRequestHandler<AuditListQuery, PagedResult<AuditLog>>,
    IRequestHandler<GetOpportunityAuditQuery, IReadOnlyList<AuditLog>>
{
    private readonly IApplicationDbContext _db;
    public AuditHandlers(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLog>> Handle(AuditListQuery q, CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.EntityType)) query = query.Where(a => a.EntityType == q.EntityType);
        if (q.EntityId is Guid eid) query = query.Where(a => a.EntityId == eid);
        if (q.OpportunityId is Guid oid) query = query.Where(a => a.OpportunityId == oid);
        if (q.ActorUserId is Guid aid) query = query.Where(a => a.ActorUserId == aid);
        if (q.Action is AuditAction act) query = query.Where(a => a.Action == act);
        if (q.FromUtc is DateTime f) query = query.Where(a => a.OccurredAtUtc >= f);
        if (q.ToUtc is DateTime t) query = query.Where(a => a.OccurredAtUtc <= t);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.OccurredAtUtc).Skip(q.Skip).Take(q.Take).ToListAsync(ct);
        return PagedResult<AuditLog>.Create(items, q.Page, q.Take, total);
    }

    public async Task<IReadOnlyList<AuditLog>> Handle(GetOpportunityAuditQuery r, CancellationToken ct) =>
        await _db.AuditLogs.AsNoTracking().Where(a => a.OpportunityId == r.OpportunityId).OrderByDescending(a => a.OccurredAtUtc).ToListAsync(ct);
}
