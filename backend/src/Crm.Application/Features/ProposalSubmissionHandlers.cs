using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

public record AssignBuilderCommand(Guid OpportunityId, BuilderType BuilderType, Guid BuilderUserId) : IRequest<Unit>;
public record GetEstimatedCostQuery(Guid OpportunityId) : IRequest<EstimatedCostDto?>;
public record SubmitEstimatedCostCommand(Guid OpportunityId, decimal Amount, string? Notes) : IRequest<Unit>;
public record GetSlResponsesQuery(Guid OpportunityId) : IRequest<IReadOnlyList<SlResponseDto>>;
public record CreateSlResponseCommand(Guid OpportunityId, Guid ServiceLineId, Guid? ScopeItemId, DateTime? DueAtUtc) : IRequest<Guid>;
public record UpdateSlResponseCommand(Guid Id, decimal? CostSar, Guid? TechnicalProposalAttachmentId, Guid? CostingAttachmentId) : IRequest<Unit>;
public record SubmitSlResponseCommand(Guid Id) : IRequest<Unit>;
public record ReturnSlResponseCommand(Guid Id, string Reason) : IRequest<Unit>;
public record GetPricingQuery(Guid OpportunityId) : IRequest<IReadOnlyList<ProposalPricingDto>>;
public record CreatePricingCommand(Guid OpportunityId, decimal PriceSar, decimal CostSar) : IRequest<ProposalPricingDto>;
public record MarkProposalReadyCommand(Guid OpportunityId) : IRequest<Unit>;
public record GetMyProposalTasksQuery : PagedQuery, IRequest<PagedResult<ProposalTaskDto>>;

public sealed class ProposalHandlers :
    IRequestHandler<AssignBuilderCommand, Unit>,
    IRequestHandler<GetEstimatedCostQuery, EstimatedCostDto?>,
    IRequestHandler<SubmitEstimatedCostCommand, Unit>,
    IRequestHandler<GetSlResponsesQuery, IReadOnlyList<SlResponseDto>>,
    IRequestHandler<CreateSlResponseCommand, Guid>,
    IRequestHandler<UpdateSlResponseCommand, Unit>,
    IRequestHandler<SubmitSlResponseCommand, Unit>,
    IRequestHandler<ReturnSlResponseCommand, Unit>,
    IRequestHandler<GetPricingQuery, IReadOnlyList<ProposalPricingDto>>,
    IRequestHandler<CreatePricingCommand, ProposalPricingDto>,
    IRequestHandler<MarkProposalReadyCommand, Unit>,
    IRequestHandler<GetMyProposalTasksQuery, PagedResult<ProposalTaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IWorkflowEngine _workflow;
    private readonly ICurrentUser _user;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;
    private readonly IOpportunityVisibility _visibility;

    public ProposalHandlers(IApplicationDbContext db, IWorkflowEngine workflow, ICurrentUser user, IDateTime clock, IAuditWriter audit, IOpportunityVisibility visibility)
    {
        _db = db; _workflow = workflow; _user = user; _clock = clock; _audit = audit; _visibility = visibility;
    }

    public async Task<Unit> Handle(AssignBuilderCommand r, CancellationToken ct)
    {
        if (!_user.HasRole(RoleCodes.BidsPresales) && !_user.HasRole(RoleCodes.BidsMgmt) && !_user.IsAdmin)
            throw new ForbiddenException("Only Bids & Presales or Bids Management can assign a builder.");

        var o = await Opp(r.OpportunityId, ct);
        if (o.IsClosed) throw new BusinessRuleException("A closed opportunity cannot be assigned a builder.");

        var builderUser = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Id == r.BuilderUserId && x.IsActive, ct)
            ?? throw new NotFoundException(nameof(User), r.BuilderUserId);

        var expectedRole = r.BuilderType == BuilderType.Presales ? RoleCodes.Presales : RoleCodes.Sl;
        if (!builderUser.UserRoles.Any(ur => ur.Role.Code == expectedRole))
            throw new BusinessRuleException($"The selected builder must hold the {expectedRole} role.",
                new Dictionary<string, string[]> { ["builderUserId"] = [$"User does not have the {expectedRole} role."] });

        await _workflow.AssignBuilderAsync(o, r.BuilderType, builderUser.Id, ct);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<EstimatedCostDto?> Handle(GetEstimatedCostQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var e = await _db.EstimatedCosts.AsNoTracking().FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct);
        return PricingFieldMask.From(e, _visibility.CanViewPricingFields);
    }

    public async Task<Unit> Handle(SubmitEstimatedCostCommand r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var existing = await _db.EstimatedCosts.FirstOrDefaultAsync(e => e.OpportunityId == r.OpportunityId, ct);
        if (existing is null)
        {
            _db.EstimatedCosts.Add(new EstimatedCost
            {
                OpportunityId = r.OpportunityId, Amount = r.Amount, Notes = r.Notes,
                SubmittedByUserId = _user.UserId!.Value, SubmittedAtUtc = _clock.UtcNow
            });
        }
        else { existing.Amount = r.Amount; existing.Notes = r.Notes; existing.SubmittedAtUtc = _clock.UtcNow; }
        _audit.Add("EstimatedCost", r.OpportunityId, AuditAction.EstimatedCostSubmitted, $"Amount {r.Amount}", r.OpportunityId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<SlResponseDto>> Handle(GetSlResponsesQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var canView = _visibility.CanViewPricingFields;
        var rows = await _db.SlResponses.AsNoTracking().Include(s => s.ServiceLine)
            .Where(s => s.OpportunityId == r.OpportunityId).ToListAsync(ct);
        return rows.Select(s => PricingFieldMask.From(s, canView)).ToList();
    }

    public async Task<Guid> Handle(CreateSlResponseCommand r, CancellationToken ct)
    {
        var s = new SlResponse { OpportunityId = r.OpportunityId, ServiceLineId = r.ServiceLineId, ScopeItemId = r.ScopeItemId, DueAtUtc = r.DueAtUtc };
        _db.SlResponses.Add(s);
        await _db.SaveChangesAsync(ct);
        return s.Id;
    }

    public async Task<Unit> Handle(UpdateSlResponseCommand r, CancellationToken ct)
    {
        var s = await _db.SlResponses.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(SlResponse), r.Id);
        s.CostSar = r.CostSar; s.TechnicalProposalAttachmentId = r.TechnicalProposalAttachmentId; s.CostingAttachmentId = r.CostingAttachmentId;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SubmitSlResponseCommand r, CancellationToken ct)
    {
        var s = await _db.SlResponses.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(SlResponse), r.Id);
        s.Status = SlResponseStatus.Submitted; s.SubmittedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ReturnSlResponseCommand r, CancellationToken ct)
    {
        var s = await _db.SlResponses.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(SlResponse), r.Id);
        s.Status = SlResponseStatus.Returned; s.ReturnReason = r.Reason;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<ProposalPricingDto>> Handle(GetPricingQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var canView = _visibility.CanViewPricingFields;
        var rows = await _db.ProposalPricings.AsNoTracking()
            .Where(p => p.OpportunityId == r.OpportunityId).OrderBy(p => p.Version).ToListAsync(ct);
        return rows.Select(p => PricingFieldMask.From(p, canView)).ToList();
    }

    public async Task<ProposalPricingDto> Handle(CreatePricingCommand r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.OpportunityId, ct);
        var current = await _db.ProposalPricings.Where(p => p.OpportunityId == r.OpportunityId).ToListAsync(ct);
        foreach (var p in current) p.IsCurrent = false;
        var next = new ProposalPricing
        {
            OpportunityId = r.OpportunityId,
            PriceSar = r.PriceSar,
            CostSar = r.CostSar,
            MarginPercent = ProposalPricing.ComputeMargin(r.PriceSar, r.CostSar),
            Version = current.Count == 0 ? 1 : current.Max(p => p.Version) + 1,
            IsCurrent = true,
            CreatedByUserId = _user.UserId!.Value,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.ProposalPricings.Add(next);
        await _db.SaveChangesAsync(ct);
        return PricingFieldMask.From(next, _visibility.CanViewPricingFields);
    }

    public async Task<Unit> Handle(MarkProposalReadyCommand r, CancellationToken ct)
    {
        var o = await Opp(r.OpportunityId, ct);
        var review = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.InternalReview && s.Stage.Code == StageCodes.ResponseDevelopment, ct);
        await _workflow.ChangeStatusAsync(o, review.Id, "Proposal marked ready", _user.Roles, ct);
        await _workflow.OpenGateAsync(o, GateCodes.ProposalReview, ct: ct);
        _audit.Add("Opportunity", o.Id, AuditAction.ProposalSubmittedForReview, "Marked ready", o.Id);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<PagedResult<ProposalTaskDto>> Handle(GetMyProposalTasksQuery r, CancellationToken ct)
    {
        var userId = _user.UserId;
        var q = _db.Opportunities.AsNoTracking()
            .Where(o => o.BuilderUserId == userId && !o.IsClosed
                && (o.Status.Code == StatusCodes.InProgress || o.Status.Code == StatusCodes.InternalReview));
        if (!string.IsNullOrWhiteSpace(r.Search))
            q = q.Where(o => o.Name.Contains(r.Search) || o.OpportunityNumber.Contains(r.Search) || o.Customer.NameEn.Contains(r.Search));
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(o => o.Deadlines.InternalDeadline ?? o.Deadlines.SubmissionDeadline).ThenBy(o => o.CreatedAtUtc)
            .Skip(r.Skip).Take(r.Take)
            .Select(o => new ProposalTaskDto(o.Id, o.OpportunityNumber, o.Name, o.Customer.NameEn,
                o.Deadlines.InternalDeadline ?? o.Deadlines.SubmissionDeadline, o.BuilderType,
                o.Status.Code, o.Status.NameEn, o.Status.NameAr))
            .ToListAsync(ct);
        return PagedResult<ProposalTaskDto>.Create(items, r.Page, r.Take, total);
    }

    private async Task<Opportunity> Opp(Guid id, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(id, ct);
        return await _db.Opportunities.Include(o => o.Status).Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(Opportunity), id);
    }
}

public record GetBidBondQuery(Guid OpportunityId) : IRequest<BidBond?>;
public record RequestBidBondCommand(Guid OpportunityId) : IRequest<Unit>;
public record IssueBidBondCommand(Guid OpportunityId, decimal Amount, DateTime ValidUntil, string IssuingBank, Guid? AttachmentId) : IRequest<Unit>;
public record RejectBidBondCommand(Guid OpportunityId, string Reason) : IRequest<Unit>;

public sealed class BidBondHandlers :
    IRequestHandler<GetBidBondQuery, BidBond?>,
    IRequestHandler<RequestBidBondCommand, Unit>,
    IRequestHandler<IssueBidBondCommand, Unit>,
    IRequestHandler<RejectBidBondCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly IAuditWriter _audit;
    public BidBondHandlers(IApplicationDbContext db, IDateTime clock, IAuditWriter audit) { _db = db; _clock = clock; _audit = audit; }

    public Task<BidBond?> Handle(GetBidBondQuery r, CancellationToken ct) =>
        _db.BidBonds.AsNoTracking().FirstOrDefaultAsync(b => b.OpportunityId == r.OpportunityId, ct);

    public async Task<Unit> Handle(RequestBidBondCommand r, CancellationToken ct)
    {
        var b = await Ensure(r.OpportunityId, ct);
        b.Required = true; b.Status = BidBondStatus.Requested; b.RequestedAtUtc = _clock.UtcNow;
        _audit.Add("BidBond", b.Id, AuditAction.BidBondRequested, "Requested", r.OpportunityId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(IssueBidBondCommand r, CancellationToken ct)
    {
        var b = await Ensure(r.OpportunityId, ct);
        b.Status = BidBondStatus.Issued; b.AmountSar = r.Amount;
        b.ValidUntil = DateTime.SpecifyKind(r.ValidUntil, DateTimeKind.Utc);
        b.IssuingBank = r.IssuingBank; b.IssuedAtUtc = _clock.UtcNow;
        _audit.Add("BidBond", b.Id, AuditAction.BidBondIssued, r.IssuingBank, r.OpportunityId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(RejectBidBondCommand r, CancellationToken ct)
    {
        var b = await Ensure(r.OpportunityId, ct);
        b.Status = BidBondStatus.Rejected; b.RejectReason = r.Reason;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<BidBond> Ensure(Guid oppId, CancellationToken ct)
    {
        var b = await _db.BidBonds.FirstOrDefaultAsync(x => x.OpportunityId == oppId, ct);
        if (b is not null) return b;
        b = new BidBond { OpportunityId = oppId };
        _db.BidBonds.Add(b);
        return b;
    }
}

public record SubmitOpportunityCommand(Guid OpportunityId, SubmissionChannel Channel, string? Reference, DateTime SubmittedAtUtc) : IRequest<Unit>;
public record GetSubmissionQuery(Guid OpportunityId) : IRequest<Submission?>;
public record RecordOutcomeCommand(Guid OpportunityId, OutcomeResult Result, DateTime AnnouncedAtUtc, decimal? AwardedValueSar, string? CompetitorName, string? LossReason, string? Notes) : IRequest<Unit>;
public record GetOutcomeQuery(Guid OpportunityId) : IRequest<OpportunityOutcome?>;

public sealed class SubmissionHandlers :
    IRequestHandler<SubmitOpportunityCommand, Unit>,
    IRequestHandler<GetSubmissionQuery, Submission?>,
    IRequestHandler<RecordOutcomeCommand, Unit>,
    IRequestHandler<GetOutcomeQuery, OpportunityOutcome?>
{
    private readonly IApplicationDbContext _db;
    private readonly IWorkflowEngine _workflow;
    private readonly ICurrentUser _user;
    private readonly INumberGenerator _numbers;
    private readonly IAuditWriter _audit;

    public SubmissionHandlers(IApplicationDbContext db, IWorkflowEngine workflow, ICurrentUser user, INumberGenerator numbers, IAuditWriter audit)
    {
        _db = db; _workflow = workflow; _user = user; _numbers = numbers; _audit = audit;
    }

    public async Task<Unit> Handle(SubmitOpportunityCommand r, CancellationToken ct)
    {
        var o = await _db.Opportunities.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.OpportunityId);

        var pricing = await _db.ProposalPricings.AnyAsync(p => p.OpportunityId == o.Id && p.IsCurrent, ct);
        if (!pricing) throw new BusinessRuleException("Submission is blocked: current proposal pricing is required.");

        var mgmt = await _db.GateInstances.Include(g => g.Gate)
            .AnyAsync(g => g.OpportunityId == o.Id && g.Gate.Code == GateCodes.MgmtApproval && g.State == GateState.Approved, ct);
        if (!mgmt) throw new BusinessRuleException("Submission is blocked: management approval is required.");

        if (o.RequiresBidBond)
        {
            var bond = await _db.BidBonds.FirstOrDefaultAsync(b => b.OpportunityId == o.Id, ct);
            if (bond is null || bond.Status != BidBondStatus.Issued)
                throw new BusinessRuleException("Submission is blocked: required bid bond is not issued.");
        }

        var submitted = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.Submitted && s.Stage.Code == StageCodes.Submission, ct);
        await _workflow.ChangeStatusAsync(o, submitted.Id, "Submitted", _user.Roles, ct);
        _db.Submissions.Add(new Submission
        {
            OpportunityId = o.Id,
            Channel = r.Channel,
            Reference = r.Reference,
            SubmittedAtUtc = DateTime.SpecifyKind(r.SubmittedAtUtc, DateTimeKind.Utc),
            SubmittedByUserId = _user.UserId!.Value
        });
        _audit.Add("Opportunity", o.Id, AuditAction.Submitted, r.Channel.ToString(), o.Id);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public Task<Submission?> Handle(GetSubmissionQuery r, CancellationToken ct) =>
        _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.OpportunityId == r.OpportunityId, ct);

    public async Task<Unit> Handle(RecordOutcomeCommand r, CancellationToken ct)
    {
        var o = await _db.Opportunities.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == r.OpportunityId, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.OpportunityId);

        _db.OpportunityOutcomes.Add(new OpportunityOutcome
        {
            OpportunityId = o.Id,
            Result = r.Result,
            AnnouncedAtUtc = DateTime.SpecifyKind(r.AnnouncedAtUtc, DateTimeKind.Utc),
            AwardedValueSar = r.AwardedValueSar,
            CompetitorName = r.CompetitorName,
            LossReason = r.LossReason,
            Notes = r.Notes
        });

        if (r.Result == OutcomeResult.Lost)
        {
            var lost = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.Lost && s.Stage.Code == StageCodes.Submission, ct);
            await _workflow.ChangeStatusAsync(o, lost.Id, r.LossReason, _user.Roles, ct);
        }
        else
        {
            var won = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Code == StatusCodes.Won && s.Stage.Code == StageCodes.Contracting, ct);
            await _workflow.ChangeStatusAsync(o, won.Id, "Won", _user.Roles, ct);
            _db.Contracts.Add(new Contract
            {
                OpportunityId = o.Id,
                ContractNumber = await _numbers.NextContractNumberAsync(ct),
                ContractStatus = ContractStatus.Won,
                ContractValueSar = r.AwardedValueSar ?? o.ExpectedValueSar
            });
        }
        _audit.Add("Opportunity", o.Id, AuditAction.OutcomeRecorded, r.Result.ToString(), o.Id);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public Task<OpportunityOutcome?> Handle(GetOutcomeQuery r, CancellationToken ct) =>
        _db.OpportunityOutcomes.AsNoTracking().FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct);
}
