using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Opportunities;

public record JourneyStepDto(
    string Code,
    string NameEn,
    string NameAr,
    string State, // done | current | pending | skipped | notStarted
    string? ActorName,
    string? ActorRole,
    DateTime? AtUtc,
    string? Decision,
    string? Reason,
    int? Round,
    Guid? GateInstanceId);

public record OpportunityJourneyDto(
    Guid OpportunityId,
    string StageCode,
    string StatusCode,
    bool IsClosed,
    bool IsOnHold,
    IReadOnlyList<JourneyStepDto> Steps);

public record NextActionDto(
    string Code,
    string LabelKey,
    string HeadlineEn,
    string HeadlineAr,
    string DescriptionEn,
    string DescriptionAr,
    string? ResponsibleRole,
    string? ResponsibleUserName,
    bool CanCurrentUserAct,
    string? PrimaryRoute,
    Guid? GateInstanceId,
    IReadOnlyList<string> Blockers);

public record OpportunityNextActionsDto(
    Guid OpportunityId,
    NextActionDto? Primary,
    IReadOnlyList<NextActionDto> Secondary);

public record GetOpportunityJourneyQuery(Guid Id) : IRequest<OpportunityJourneyDto>;
public record GetOpportunityNextActionsQuery(Guid Id) : IRequest<OpportunityNextActionsDto>;

public sealed class JourneyHandlers :
    IRequestHandler<GetOpportunityJourneyQuery, OpportunityJourneyDto>,
    IRequestHandler<GetOpportunityNextActionsQuery, OpportunityNextActionsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IOpportunityVisibility _visibility;

    public JourneyHandlers(IApplicationDbContext db, ICurrentUser user, IOpportunityVisibility visibility)
    {
        _db = db;
        _user = user;
        _visibility = visibility;
    }

    public async Task<OpportunityJourneyDto> Handle(GetOpportunityJourneyQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.Id, ct);
        var o = await _db.Opportunities.AsNoTracking()
            .Include(x => x.Stage).Include(x => x.Status)
            .Include(x => x.BuilderUser).Include(x => x.OwnerUser)
            .FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.Id);

        var gates = await _db.GateInstances.AsNoTracking()
            .Include(g => g.Gate).Include(g => g.AssignedUser).Include(g => g.DecidedByUser)
            .Where(g => g.OpportunityId == r.Id)
            .OrderBy(g => g.OpenedAtUtc).ThenBy(g => g.Round)
            .ToListAsync(ct);

        var hasBuilder = o.BuilderUserId is not null;
        var hasCost = await _db.EstimatedCosts.AsNoTracking().AnyAsync(e => e.OpportunityId == r.Id, ct);
        var hasPricing = await _db.ProposalPricings.AsNoTracking().AnyAsync(p => p.OpportunityId == r.Id && p.IsCurrent, ct);
        var bond = await _db.BidBonds.AsNoTracking().FirstOrDefaultAsync(b => b.OpportunityId == r.Id, ct);
        var submission = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.OpportunityId == r.Id, ct);
        var outcome = await _db.OpportunityOutcomes.AsNoTracking().FirstOrDefaultAsync(x => x.OpportunityId == r.Id, ct);
        var contract = await _db.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.OpportunityId == r.Id, ct);
        var deadlinesSet = o.Deadlines.SubmissionDeadline is not null || o.Deadlines.InternalDeadline is not null;

        GateInstance? Latest(string code) => gates.Where(g => g.Gate.Code == code).OrderByDescending(g => g.Round).ThenByDescending(g => g.OpenedAtUtc).FirstOrDefault();
        GateInstance? First(string code) => gates.Where(g => g.Gate.Code == code).OrderBy(g => g.OpenedAtUtc).FirstOrDefault();

        var steps = new List<JourneyStepDto>();

        void AddGate(string code, string en, string ar, GateInstance? g, bool required = true)
        {
            if (g is null)
            {
                steps.Add(new JourneyStepDto(code, en, ar, required ? "notStarted" : "skipped", null, null, null, null, null, null, null));
                return;
            }
            var state = g.State switch
            {
                GateState.Pending => "current",
                GateState.Approved => "done",
                GateState.Rejected => "done",
                GateState.Returned => "done",
                _ => "pending"
            };
            steps.Add(new JourneyStepDto(code, en, ar, state,
                g.DecidedByUser?.DisplayName ?? g.AssignedUser?.DisplayName,
                g.AssignedRoleCode,
                g.DecidedAtUtc ?? g.OpenedAtUtc,
                g.Decision, g.Reason, g.Round, g.Id));
        }

        AddGate(GateCodes.Gw1Review, "1st Gateway Review", "مراجعة البوابة الأولى", First(GateCodes.Gw1Review));

        var routeDone = o.RequiresQualificationMeeting is not null;
        var qualMeeting = Latest(GateCodes.QualMeeting);
        var qualDecision = Latest(GateCodes.QualDecision);
        if (!routeDone)
            steps.Add(new JourneyStepDto("QUAL_ROUTE", "Qualification route", "مسار التأهيل",
                o.Status.Code == StatusCodes.AwaitingAssessment && Latest(GateCodes.Gw1Review)?.State == GateState.Approved ? "current" : "notStarted",
                null, RoleCodes.BidsPresales, null, null, null, null, null));
        else if (o.RequiresQualificationMeeting == true)
            AddGate(GateCodes.QualMeeting, "Qualification meeting", "اجتماع التأهيل", qualMeeting);
        else
            AddGate(GateCodes.QualDecision, "Service line qualification", "تأهيل خط الخدمة", qualDecision);

        var builderState = hasBuilder ? "done"
            : (o.Status.Code is StatusCodes.Qualified or StatusCodes.InProgress || qualMeeting?.State == GateState.Approved || qualDecision?.State == GateState.Approved)
                ? "current" : "notStarted";
        steps.Add(new JourneyStepDto("BUILDER", "Builder assignment", "تعيين المنفّذ", builderState,
            o.BuilderUser?.DisplayName, o.BuilderType?.ToString(), hasBuilder ? o.ModifiedAtUtc : null, null, null, null, null));

        steps.Add(new JourneyStepDto("DEADLINES", "Deadlines", "المواعيد",
            deadlinesSet ? "done" : hasBuilder ? "current" : "notStarted",
            o.OwnerUser?.DisplayName, null, deadlinesSet ? o.Deadlines.SubmissionDeadline : null, null, null, null, null));

        steps.Add(new JourneyStepDto("EST_COST", "Estimated cost", "التكلفة التقديرية",
            hasCost ? "done" : hasBuilder ? "current" : "notStarted", null, RoleCodes.Presales, null, null, null, null, null));

        if (o.RequiresBidBond)
        {
            var bondState = bond?.Status switch
            {
                BidBondStatus.Issued => "done",
                BidBondStatus.Requested => "current",
                BidBondStatus.Rejected => "done",
                _ => hasBuilder ? "current" : "notStarted"
            };
            steps.Add(new JourneyStepDto("BID_BOND", "Bid bond", "الضمان الابتدائي", bondState, null, RoleCodes.BidsMgmt, bond?.IssuedAtUtc ?? bond?.RequestedAtUtc, bond?.Status.ToString(), bond?.RejectReason, null, null));
        }
        else
            steps.Add(new JourneyStepDto("BID_BOND", "Bid bond", "الضمان الابتدائي", "skipped", null, null, null, "NotRequired", null, null, null));

        var proposalGates = gates.Where(g => g.Gate.Code == GateCodes.ProposalReview).OrderBy(g => g.Round).ToList();
        if (proposalGates.Count == 0)
            AddGate(GateCodes.ProposalReview, "Proposal review", "مراجعة العرض", null, required: hasBuilder);
        else
            foreach (var g in proposalGates)
                AddGate($"{GateCodes.ProposalReview}_R{g.Round}", $"Proposal review (round {g.Round})", $"مراجعة العرض (جولة {g.Round})", g);

        var mgmtGates = gates.Where(g => g.Gate.Code == GateCodes.MgmtApproval).OrderBy(g => g.Round).ToList();
        if (mgmtGates.Count == 0)
            AddGate(GateCodes.MgmtApproval, "Management approval", "اعتماد الإدارة", null, required: proposalGates.Any(g => g.State == GateState.Approved));
        else
            foreach (var g in mgmtGates)
                AddGate($"{GateCodes.MgmtApproval}_R{g.Round}", $"Management approval (round {g.Round})", $"اعتماد الإدارة (جولة {g.Round})", g);

        var submitted = submission is not null || o.Status.Code is StatusCodes.Submitted or StatusCodes.Won or StatusCodes.Lost or StatusCodes.ContractNegotiation or StatusCodes.ContractSigned;
        steps.Add(new JourneyStepDto("SUBMIT", "Submission", "التقديم",
            submitted ? "done" : mgmtGates.Any(g => g.State == GateState.Approved) ? "current" : "notStarted",
            null, RoleCodes.BidsMgmt, submission?.SubmittedAtUtc, submission?.Channel.ToString(), null, null, null));

        steps.Add(new JourneyStepDto("OUTCOME", "Outcome", "النتيجة",
            outcome is not null ? "done" : submitted ? "current" : "notStarted",
            null, null, outcome?.AnnouncedAtUtc, outcome?.Result.ToString(), outcome?.LossReason, null, null));

        var signoff = Latest(GateCodes.ContractSignoff);
        if (outcome?.Result == OutcomeResult.Won || contract is not null || o.Stage.Code == StageCodes.Contracting)
            AddGate(GateCodes.ContractSignoff, "Contract sign-off", "اعتماد العقد", signoff);
        else if (outcome?.Result == OutcomeResult.Lost)
            steps.Add(new JourneyStepDto(GateCodes.ContractSignoff, "Contract sign-off", "اعتماد العقد", "skipped", null, null, null, "Lost", null, null, null));
        else
            AddGate(GateCodes.ContractSignoff, "Contract sign-off", "اعتماد العقد", null, required: false);

        // Ensure only one "current" — prefer first pending gate / earliest current.
        var firstCurrent = steps.FindIndex(s => s.State == "current");
        if (firstCurrent >= 0)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (i != firstCurrent && steps[i].State == "current")
                    steps[i] = steps[i] with { State = "pending" };
            }
        }

        return new OpportunityJourneyDto(o.Id, o.Stage.Code, o.Status.Code, o.IsClosed,
            o.Status.Code == StatusCodes.Hold, steps);
    }

    public async Task<OpportunityNextActionsDto> Handle(GetOpportunityNextActionsQuery r, CancellationToken ct)
    {
        await _visibility.EnsureCanAccessAsync(r.Id, ct);
        var o = await _db.Opportunities.AsNoTracking()
            .Include(x => x.Stage).Include(x => x.Status)
            .Include(x => x.BuilderUser)
            .FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Opportunity), r.Id);

        if (o.IsClosed)
            return new OpportunityNextActionsDto(o.Id, null, []);

        var pendingGates = await _db.GateInstances.AsNoTracking()
            .Include(g => g.Gate).Include(g => g.AssignedUser)
            .Where(g => g.OpportunityId == r.Id && g.State == GateState.Pending)
            .OrderByDescending(g => g.OpenedAtUtc)
            .ToListAsync(ct);

        var secondary = new List<NextActionDto>();
        NextActionDto? primary = null;

        foreach (var g in pendingGates)
        {
            var canAct = _user.IsAdmin
                || (g.AssignedUserId is Guid uid && uid == _user.UserId)
                || RoleMatches(g.AssignedRoleCode, _user.Roles);
            var action = new NextActionDto(
                $"GATE_{g.Gate.Code}",
                $"actions.gate.{g.Gate.Code}",
                $"Waiting for {g.Gate.NameEn}",
                $"بانتظار {g.Gate.NameAr}",
                g.AssignedUser?.DisplayName is string n
                    ? $"Assigned to {n}. Opened {g.OpenedAtUtc:u}."
                    : $"Pending on role {g.AssignedRoleCode}.",
                g.AssignedUser?.DisplayName is string na
                    ? $"مسند إلى {na}."
                    : $"بانتظار الدور {g.AssignedRoleCode}.",
                g.AssignedRoleCode,
                g.AssignedUser?.DisplayName,
                canAct,
                $"/opportunities/{o.Id}/approvals",
                g.Id,
                []);
            if (primary is null && canAct) primary = action;
            else if (primary is null) primary = action;
            else secondary.Add(action);
        }

        if (primary is null)
        {
            if (o.Status.Code == StatusCodes.Hold)
            {
                primary = Act("RESUME", "actions.resume", "Opportunity is on hold", "الفرصة موقوفة",
                    o.HoldReason ?? "Resume when ready.", o.HoldReason ?? "استأنف عند الجاهزية.",
                    RoleCodes.BidsPresales, null, CanAny(RoleCodes.BidsPresales, RoleCodes.BidsMgmt),
                    $"/opportunities/{o.Id}", null, []);
            }
            else if (LatestApproved(pendingGates) is null
                     && await Gw1Approved(o.Id, ct)
                     && o.RequiresQualificationMeeting is null
                     && o.Status.Code == StatusCodes.AwaitingAssessment)
            {
                primary = Act("QUAL_ROUTE", "actions.qualRoute", "Choose qualification route", "اختر مسار التأهيل",
                    "Does this opportunity require a qualification meeting?",
                    "هل تتطلب هذه الفرصة اجتماع تأهيل؟",
                    RoleCodes.BidsPresales, null, CanAny(RoleCodes.BidsPresales),
                    $"/opportunities/{o.Id}/qualification", null, []);
            }
            else if (o.Status.Code is StatusCodes.Qualified && o.BuilderUserId is null)
            {
                primary = Act("ASSIGN_BUILDER", "actions.assignBuilder", "Assign builder & deadlines", "عيّن المنفّذ والمواعيد",
                    "Qualification passed — assign a builder to start response development.",
                    "تم التأهيل — عيّن منفّذاً لبدء تطوير الرد.",
                    RoleCodes.BidsPresales, null, CanAny(RoleCodes.BidsPresales, RoleCodes.BidsMgmt),
                    $"/opportunities/{o.Id}/proposal", null, []);
            }
            else if (o.BuilderUserId == _user.UserId && o.Status.Code == StatusCodes.InProgress)
            {
                var blockers = new List<string>();
                if (!await _db.ProposalPricings.AnyAsync(p => p.OpportunityId == o.Id && p.IsCurrent, ct))
                    blockers.Add("Pricing is required before marking ready.");
                primary = Act("MARK_READY", "actions.markReady", "Continue proposal build", "متابعة بناء العرض",
                    "Complete costing/pricing, then mark the proposal ready for review.",
                    "أكمل التكلفة والتسعير ثم أرسل العرض للمراجعة.",
                    null, o.BuilderUser?.DisplayName, true,
                    $"/opportunities/{o.Id}/proposal", null, blockers);
            }
            else if (o.Status.Code == StatusCodes.InternalReview
                     && await _db.GateInstances.AnyAsync(g => g.OpportunityId == o.Id && g.Gate.Code == GateCodes.MgmtApproval && g.State == GateState.Approved, ct))
            {
                var blockers = new List<string>();
                if (o.RequiresBidBond)
                {
                    var bond = await _db.BidBonds.AsNoTracking().FirstOrDefaultAsync(b => b.OpportunityId == o.Id, ct);
                    if (bond is null || bond.Status != BidBondStatus.Issued)
                        blockers.Add("Bid bond is required and not issued.");
                }
                if (!await _db.ProposalPricings.AnyAsync(p => p.OpportunityId == o.Id && p.IsCurrent, ct))
                    blockers.Add("Current proposal pricing is required.");
                primary = Act("SUBMIT", "actions.submit", "Ready to submit", "جاهز للتقديم",
                    "Management approved. Submit the opportunity.",
                    "اعتمدت الإدارة. قدّم الفرصة.",
                    RoleCodes.BidsMgmt, null, CanAny(RoleCodes.BidsMgmt) && blockers.Count == 0,
                    $"/submission/{o.Id}", null, blockers);
            }
            else if (o.Status.Code == StatusCodes.Submitted)
            {
                primary = Act("OUTCOME", "actions.outcome", "Record outcome", "تسجيل النتيجة",
                    "Record won/lost when the client announces the result.",
                    "سجّل الفوز أو الخسارة عند إعلان النتيجة.",
                    RoleCodes.BidsMgmt, null, CanAny(RoleCodes.BidsMgmt, RoleCodes.AM),
                    $"/submission/{o.Id}/outcome", null, []);
            }
        }

        return new OpportunityNextActionsDto(o.Id, primary, secondary);
    }

    private static NextActionDto Act(
        string code, string labelKey, string en, string ar, string descEn, string descAr,
        string? role, string? user, bool canAct, string? route, Guid? gateId, IReadOnlyList<string> blockers)
        => new(code, labelKey, en, ar, descEn, descAr, role, user, canAct, route, gateId, blockers);

    private static GateInstance? LatestApproved(IReadOnlyList<GateInstance> _) => null;

    private async Task<bool> Gw1Approved(Guid opportunityId, CancellationToken ct) =>
        await _db.GateInstances.AsNoTracking().AnyAsync(g =>
            g.OpportunityId == opportunityId && g.Gate.Code == GateCodes.Gw1Review && g.State == GateState.Approved, ct);

    private bool CanAny(params string[] roles) =>
        _user.IsAdmin || roles.Any(r => _user.HasRole(r));

    private static bool RoleMatches(string required, IReadOnlyList<string> actual)
    {
        var needed = required.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return needed.Any(n => actual.Contains(n, StringComparer.OrdinalIgnoreCase));
    }
}
