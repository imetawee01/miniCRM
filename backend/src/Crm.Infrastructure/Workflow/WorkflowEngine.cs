using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Workflow;

/// <summary>
/// Configurable engine that reads WorkflowGate + StatusTransition from the database.
/// Controllers must not hardcode the flow.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditWriter _audit;
    private readonly IEmailComposer _email;
    private readonly INotificationPublisher _notifications;

    public WorkflowEngine(
        IApplicationDbContext db,
        IDateTime clock,
        ICurrentUser user,
        IAuditWriter audit,
        IEmailComposer email,
        INotificationPublisher notifications)
    {
        _db = db;
        _clock = clock;
        _user = user;
        _audit = audit;
        _email = email;
        _notifications = notifications;
    }

    public async Task<GateInstance> OpenGateAsync(Opportunity opportunity, string gateCode, Guid? assignedUserId = null, int? round = null, CancellationToken ct = default)
    {
        var gate = await _db.WorkflowGates.FirstOrDefaultAsync(g => g.Code == gateCode && g.IsActive, ct)
            ?? throw new NotFoundException(nameof(WorkflowGate), gateCode);

        var instance = new GateInstance
        {
            Id = Guid.NewGuid(),
            OpportunityId = opportunity.Id,
            GateId = gate.Id,
            Gate = gate,
            State = GateState.Pending,
            AssignedRoleCode = gate.ResponsibleRoleCode,
            AssignedUserId = assignedUserId,
            OpenedAtUtc = _clock.UtcNow,
            Round = round ?? 1
        };
        _db.GateInstances.Add(instance);
        _audit.Add("GateInstance", instance.Id, AuditAction.GateOpened, $"Opened gate {gateCode} round {instance.Round}", opportunity.Id, toValue: gateCode);
        opportunity.AddDomainEvent(new Domain.Events.GateOpenedEvent(opportunity.Id, instance.Id, gateCode, _clock.UtcNow));
        var content = NotificationContents.GateAssigned(gate.NameEn, opportunity.OpportunityNumber, opportunity.Name);
        var link = $"/opportunities/{opportunity.Id}/approvals";
        if (assignedUserId is Guid assignee)
            await _notifications.PublishAsync(assignee, NotificationType.GateAssigned, content, link, opportunity.Id, ct);
        else
            await _notifications.PublishToRolesAsync(gate.ResponsibleRoleCode, NotificationType.GateAssigned, content, link, opportunity.Id, ct);
        return instance;
    }

    public async Task ApplyGateDecisionAsync(
        GateInstance instance,
        string decision,
        string? reason,
        Guid actorUserId,
        IReadOnlyList<string> actorRoles,
        CancellationToken ct = default)
    {
        if (instance.State != GateState.Pending)
            throw new BusinessRuleException("This gate has already been decided.");

        var gate = instance.Gate ?? await _db.WorkflowGates.FirstAsync(g => g.Id == instance.GateId, ct);
        var allowed = gate.AllowedDecisions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!allowed.Contains(decision, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException($"Decision '{decision}' is not allowed. Allowed: {gate.AllowedDecisions}");

        if (!RoleMatches(gate.ResponsibleRoleCode, actorRoles) && !actorRoles.Contains(RoleCodes.Admin))
            throw new ForbiddenException($"Role {gate.ResponsibleRoleCode} is required to decide this gate.");

        var isNegative = decision is "Reject" or "Rejected" or "Return" or "NotQualified" or "NotPassed";
        if (gate.RequiresReasonOnReject && isNegative && string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("A reason is required for this decision.");

        var isApprove = !isNegative && decision.Equals("Approve", StringComparison.OrdinalIgnoreCase);
        if (gate.RequiresAttachmentOnApprove && isApprove)
        {
            var hasAttachment = await _db.Attachments.AsNoTracking()
                .AnyAsync(a => a.EntityType == OwnerEntityType.GateInstance && a.EntityId == instance.Id && !a.IsDeleted, ct);
            if (!hasAttachment)
                throw new BusinessRuleException("An attachment is required before this gate can be approved.");
        }

        var opportunity = instance.Opportunity
            ?? await _db.Opportunities.Include(o => o.Status).Include(o => o.Stage)
                .FirstAsync(o => o.Id == instance.OpportunityId, ct);

        instance.Decision = decision;
        instance.Reason = reason;
        instance.DecidedAtUtc = _clock.UtcNow;
        instance.DecidedByUserId = actorUserId;

        var action = isNegative
            ? (decision.Equals("Return", StringComparison.OrdinalIgnoreCase) ? AuditAction.GateReturned : AuditAction.GateRejected)
            : AuditAction.GateApproved;

        instance.State = decision.Equals("Return", StringComparison.OrdinalIgnoreCase) ? GateState.Returned
            : isNegative ? GateState.Rejected
            : GateState.Approved;

        _audit.Add("GateInstance", instance.Id, action, $"Gate {gate.Code} decided: {decision}", opportunity.Id, toValue: decision);

        await ApplySideEffectsAsync(opportunity, gate.Code, decision, reason, ct);
    }

    public async Task<IReadOnlyList<Status>> GetAvailableTransitionsAsync(Opportunity opportunity, IReadOnlyList<string> roleCodes, CancellationToken ct = default)
    {
        var transitions = await _db.StatusTransitions
            .Include(t => t.ToStatus).ThenInclude(s => s.Stage)
            .Where(t => t.FromStatusId == opportunity.StatusId)
            .ToListAsync(ct);

        return transitions
            .Where(t => RoleMatches(t.RequiredRoleCode, roleCodes) || roleCodes.Contains(RoleCodes.Admin))
            .Select(t => t.ToStatus)
            .ToList();
    }

    public async Task ChangeStatusAsync(Opportunity opportunity, Guid toStatusId, string? reason, IReadOnlyList<string> roleCodes, CancellationToken ct = default)
    {
        var transition = await _db.StatusTransitions
            .Include(t => t.ToStatus)
            .FirstOrDefaultAsync(t => t.FromStatusId == opportunity.StatusId && t.ToStatusId == toStatusId, ct);

        if (transition is null)
        {
            var legal = await _db.StatusTransitions.Include(t => t.ToStatus)
                .Where(t => t.FromStatusId == opportunity.StatusId)
                .Select(t => t.ToStatus.NameEn)
                .ToListAsync(ct);
            throw new ConflictException(
                "Illegal status transition.",
                new Dictionary<string, string[]> { ["toStatusId"] = [$"Legal next statuses: {string.Join(", ", legal)}"] });
        }

        if (!RoleMatches(transition.RequiredRoleCode, roleCodes) && !roleCodes.Contains(RoleCodes.Admin))
            throw new ForbiddenException();

        if (transition.RequiresReason && string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("A reason is required for this transition.");

        await MoveToStatusAsync(opportunity, toStatusId, reason, ct);
    }

    public async Task HoldAsync(Opportunity opportunity, string reason, CancellationToken ct = default)
    {
        if (opportunity.IsClosed)
            throw new BusinessRuleException("A closed opportunity cannot be held.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("A reason is required to hold an opportunity.");

        opportunity.HoldPriorStatusId = opportunity.StatusId;
        opportunity.HoldPriorStageId = opportunity.StageId;
        opportunity.HoldReason = reason;
        await MoveToStatusAsync(opportunity, SeedIds.StHold, reason, ct);
        _audit.Add("Opportunity", opportunity.Id, AuditAction.OpportunityHeld, reason, opportunity.Id);
    }

    public async Task ResumeAsync(Opportunity opportunity, CancellationToken ct = default)
    {
        if (opportunity.StatusId != SeedIds.StHold || opportunity.HoldPriorStatusId is null)
            throw new BusinessRuleException("This opportunity is not on hold.");

        var prior = opportunity.HoldPriorStatusId.Value;
        opportunity.HoldPriorStatusId = null;
        opportunity.HoldPriorStageId = null;
        var reason = opportunity.HoldReason;
        opportunity.HoldReason = null;
        await MoveToStatusAsync(opportunity, prior, reason, ct);
        _audit.Add("Opportunity", opportunity.Id, AuditAction.OpportunityResumed, "Resumed from hold", opportunity.Id);
    }

    public async Task CancelAsync(Opportunity opportunity, string reason, IReadOnlyList<string> roleCodes, CancellationToken ct = default)
    {
        var allowed = new[] { RoleCodes.BidsPresales, RoleCodes.BidsMgmt, RoleCodes.Admin };
        if (!roleCodes.Any(r => allowed.Contains(r)))
            throw new ForbiddenException();
        if (opportunity.IsClosed)
            throw new BusinessRuleException("Already closed.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleException("A reason is required to cancel.");

        var cancelId = opportunity.StageId == SeedIds.StageQualification ? SeedIds.StQualCanceled : SeedIds.StRdCanceled;
        await MoveToStatusAsync(opportunity, cancelId, reason, ct);
        _audit.Add("Opportunity", opportunity.Id, AuditAction.OpportunityCanceled, reason, opportunity.Id);
    }

    public async Task AssignBuilderAsync(Opportunity opportunity, BuilderType builderType, Guid builderUserId, CancellationToken ct = default)
    {
        // Section 7.2 #1: qualification-meeting branch rejoins at Builder Assignment; bid bond is a parallel side-track, not a terminus.
        opportunity.BuilderType = builderType;
        opportunity.BuilderUserId = builderUserId;
        opportunity.OwnerUserId = builderUserId;
        if (opportunity.StatusId != SeedIds.StQualified)
            await MoveToStatusAsync(opportunity, SeedIds.StQualified, "Qualified — builder assigned", ct);
        await MoveToStatusAsync(opportunity, SeedIds.StInProgress, "Moved to response development", ct);
        _audit.Add("Opportunity", opportunity.Id, AuditAction.BuilderAssigned, $"Builder {builderType}", opportunity.Id, toValue: builderUserId.ToString());
        await _email.ComposeAsync(EmailTemplateCodes.BuilderNotification, opportunity, _user.UserId ?? Guid.Empty, ct: ct);
        await _notifications.PublishAsync(builderUserId, NotificationType.Info,
            NotificationContents.BuilderAssigned(opportunity.OpportunityNumber, opportunity.Name),
            $"/opportunities/{opportunity.Id}/proposal", opportunity.Id, ct);
    }

    private async Task ApplySideEffectsAsync(Opportunity opportunity, string gateCode, string decision, string? reason, CancellationToken ct)
    {
        switch (gateCode)
        {
            case GateCodes.Gw1Review when decision.Equals("Reject", StringComparison.OrdinalIgnoreCase):
                await MoveToStatusAsync(opportunity, SeedIds.StNotQualified, reason, ct);
                await _email.ComposeAsync(EmailTemplateCodes.Gw1Rejected, opportunity, _user.UserId ?? Guid.Empty,
                    new Dictionary<string, string> { ["RejectionReasonsHtml"] = $"<li>{System.Net.WebUtility.HtmlEncode(reason)}</li>" }, ct);
                break;

            case GateCodes.Gw1Review when decision.Equals("Approve", StringComparison.OrdinalIgnoreCase):
                // Qualification route is offered immediately in the UI (chained dialog).
                break;

            case GateCodes.QualDecision when decision.Equals("NotQualified", StringComparison.OrdinalIgnoreCase):
                await MoveToStatusAsync(opportunity, SeedIds.StNotQualified, reason, ct);
                break;

            case GateCodes.QualDecision when decision.Equals("Qualified", StringComparison.OrdinalIgnoreCase):
                if (opportunity.StatusId != SeedIds.StQualified)
                    await MoveToStatusAsync(opportunity, SeedIds.StQualified, "Qualified by service line", ct);
                break;

            case GateCodes.QualMeeting when decision.Equals("NotPassed", StringComparison.OrdinalIgnoreCase):
                await MoveToStatusAsync(opportunity, SeedIds.StNotQualified, reason, ct);
                break;

            case GateCodes.QualMeeting when decision.Equals("Passed", StringComparison.OrdinalIgnoreCase):
                if (opportunity.StatusId != SeedIds.StQualified)
                    await MoveToStatusAsync(opportunity, SeedIds.StQualified, "Passed qualification meeting", ct);
                break;

            case GateCodes.ProposalReview when decision.Equals("Return", StringComparison.OrdinalIgnoreCase):
                // Section 7.2 #2: return to builder, re-open gate with incremented round.
                await MoveToStatusAsync(opportunity, SeedIds.StInProgress, reason, ct);
                await OpenGateAsync(opportunity, GateCodes.ProposalReview, opportunity.BuilderUserId, await NextRoundAsync(opportunity.Id, GateCodes.ProposalReview, ct), ct);
                _audit.Add("Opportunity", opportunity.Id, AuditAction.ProposalReturned, reason ?? "Returned for updates", opportunity.Id);
                break;

            case GateCodes.ProposalReview when decision.Equals("Approve", StringComparison.OrdinalIgnoreCase):
                // Section 7.2 #3: both builder paths pass through PROPOSAL_REVIEW before approval.
                await _email.ComposeAsync(EmailTemplateCodes.ApprovalRequest, opportunity, _user.UserId ?? Guid.Empty, ct: ct);
                await OpenGateAsync(opportunity, GateCodes.MgmtApproval, ct: ct);
                _audit.Add("Opportunity", opportunity.Id, AuditAction.ApprovalRequested, "Management approval requested", opportunity.Id);
                break;

            case GateCodes.MgmtApproval when decision.Equals("Reject", StringComparison.OrdinalIgnoreCase):
                await MoveToStatusAsync(opportunity, SeedIds.StNotApproved, reason, ct);
                await MoveToStatusAsync(opportunity, SeedIds.StInProgress, "Loop back for pricing updates", ct);
                await OpenGateAsync(opportunity, GateCodes.ProposalReview, opportunity.BuilderUserId, await NextRoundAsync(opportunity.Id, GateCodes.ProposalReview, ct), ct);
                break;

            case GateCodes.MgmtApproval when decision.Equals("Approve", StringComparison.OrdinalIgnoreCase):
                break;

            case GateCodes.ContractSignoff when decision.Equals("Approve", StringComparison.OrdinalIgnoreCase):
                await MoveToStatusAsync(opportunity, SeedIds.StContractSigned, reason, ct);
                var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.OpportunityId == opportunity.Id, ct);
                if (contract is not null) contract.ContractStatus = ContractStatus.ContractSigned;
                break;

            case GateCodes.ContractSignoff when decision.Equals("Reject", StringComparison.OrdinalIgnoreCase):
                break;
        }
    }

    private async Task<int> NextRoundAsync(Guid opportunityId, string gateCode, CancellationToken ct)
    {
        var max = await (
            from g in _db.GateInstances
            join w in _db.WorkflowGates on g.GateId equals w.Id
            where g.OpportunityId == opportunityId && w.Code == gateCode
            select g.Round).DefaultIfEmpty(0).MaxAsync(ct);
        return max + 1;
    }

    private async Task MoveToStatusAsync(Opportunity opportunity, Guid toStatusId, string? reason, CancellationToken ct)
    {
        var to = await _db.Statuses.Include(s => s.Stage).FirstAsync(s => s.Id == toStatusId, ct);
        var fromId = opportunity.StatusId;
        var fromStageId = opportunity.StageId;
        opportunity.StatusId = to.Id;
        opportunity.Status = to;
        opportunity.StageId = to.StageId;
        opportunity.Stage = to.Stage;
        if (to.IsTerminal)
        {
            opportunity.IsClosed = true;
            opportunity.ClosedAtUtc = _clock.UtcNow;
        }
        else
        {
            opportunity.IsClosed = false;
            opportunity.ClosedAtUtc = null;
        }

        _audit.Add("Opportunity", opportunity.Id, AuditAction.StatusChanged, reason ?? $"Status → {to.NameEn}",
            opportunity.Id, fromValue: fromId.ToString(), toValue: to.Id.ToString());
        if (fromStageId != to.StageId)
            _audit.Add("Opportunity", opportunity.Id, AuditAction.StageChanged, $"Stage → {to.Stage.NameEn}",
                opportunity.Id, fromValue: fromStageId.ToString(), toValue: to.StageId.ToString());
    }

    private static bool RoleMatches(string required, IReadOnlyList<string> actual)
    {
        var needed = required.Split(new[] { ',', '|' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return needed.Any(n => actual.Contains(n, StringComparer.OrdinalIgnoreCase));
    }
}
