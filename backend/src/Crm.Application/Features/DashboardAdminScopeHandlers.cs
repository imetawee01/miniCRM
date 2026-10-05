using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

public record ScopeDto(Guid Id, Guid OpportunityId, string Brief, IReadOnlyList<ScopeItemDto> Items);
public record ScopeItemDto(Guid Id, Guid ScopeOfWorkId, string Title, string? Description, Guid ServiceLineId, string? ServiceLineNameEn, string? ServiceLineNameAr, Guid? AssignedUserId, string? AssignedUserName, string? Comment, int SortOrder);
public record GetScopeQuery(Guid OpportunityId) : IRequest<ScopeDto?>;
public record UpdateScopeBriefCommand(Guid OpportunityId, string Brief) : IRequest<Unit>;
public record AddScopeItemCommand(Guid OpportunityId, string Title, string? Description, Guid ServiceLineId, Guid? AssignedUserId, string? Comment) : IRequest<Guid>;
public record UpdateScopeItemCommand(Guid OpportunityId, Guid ItemId, string Title, string? Description, Guid ServiceLineId, Guid? AssignedUserId, string? Comment) : IRequest<Unit>;
public record DeleteScopeItemCommand(Guid OpportunityId, Guid ItemId) : IRequest<Unit>;
public record ReorderScopeItemsCommand(Guid OpportunityId, IReadOnlyList<Guid> ItemIds) : IRequest<Unit>;

public sealed class ScopeHandlers :
    IRequestHandler<GetScopeQuery, ScopeDto?>,
    IRequestHandler<UpdateScopeBriefCommand, Unit>,
    IRequestHandler<AddScopeItemCommand, Guid>,
    IRequestHandler<UpdateScopeItemCommand, Unit>,
    IRequestHandler<DeleteScopeItemCommand, Unit>,
    IRequestHandler<ReorderScopeItemsCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public ScopeHandlers(IApplicationDbContext db) => _db = db;

    public async Task<ScopeDto?> Handle(GetScopeQuery r, CancellationToken ct)
    {
        var s = await _db.ScopesOfWork.AsNoTracking()
            .Include(x => x.Items).ThenInclude(i => i.ServiceLine)
            .Include(x => x.Items).ThenInclude(i => i.AssignedUser)
            .FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct);
        if (s is null) return new ScopeDto(Guid.Empty, r.OpportunityId, string.Empty, Array.Empty<ScopeItemDto>());
        return new ScopeDto(s.Id, s.OpportunityId, s.Brief, s.Items.OrderBy(i => i.SortOrder)
            .Select(i => new ScopeItemDto(i.Id, i.ScopeOfWorkId, i.Title, i.Description, i.ServiceLineId, i.ServiceLine?.NameEn, i.ServiceLine?.NameAr, i.AssignedUserId, i.AssignedUser?.DisplayName, i.Comment, i.SortOrder)).ToList());
    }

    public async Task<Unit> Handle(UpdateScopeBriefCommand r, CancellationToken ct)
    {
        var s = await _db.ScopesOfWork.FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct);
        if (s is null) { s = new ScopeOfWork { OpportunityId = r.OpportunityId, Brief = r.Brief }; _db.ScopesOfWork.Add(s); }
        else s.Brief = r.Brief;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Guid> Handle(AddScopeItemCommand r, CancellationToken ct)
    {
        var s = await _db.ScopesOfWork.Include(x => x.Items).FirstOrDefaultAsync(x => x.OpportunityId == r.OpportunityId, ct);
        if (s is null) { s = new ScopeOfWork { OpportunityId = r.OpportunityId }; _db.ScopesOfWork.Add(s); await _db.SaveChangesAsync(ct); }
        var item = new ScopeItem
        {
            ScopeOfWorkId = s.Id, Title = r.Title, Description = r.Description, ServiceLineId = r.ServiceLineId,
            AssignedUserId = r.AssignedUserId, Comment = r.Comment, SortOrder = s.Items.Count + 1
        };
        _db.ScopeItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<Unit> Handle(UpdateScopeItemCommand r, CancellationToken ct)
    {
        var item = await _db.ScopeItems.FirstOrDefaultAsync(x => x.Id == r.ItemId, ct) ?? throw new NotFoundException(nameof(ScopeItem), r.ItemId);
        item.Title = r.Title; item.Description = r.Description; item.ServiceLineId = r.ServiceLineId;
        item.AssignedUserId = r.AssignedUserId; item.Comment = r.Comment;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteScopeItemCommand r, CancellationToken ct)
    {
        var item = await _db.ScopeItems.FirstOrDefaultAsync(x => x.Id == r.ItemId, ct) ?? throw new NotFoundException(nameof(ScopeItem), r.ItemId);
        _db.ScopeItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ReorderScopeItemsCommand r, CancellationToken ct)
    {
        var items = await _db.ScopeItems.Where(i => r.ItemIds.Contains(i.Id)).ToListAsync(ct);
        for (var i = 0; i < r.ItemIds.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == r.ItemIds[i]);
            if (item is not null) item.SortOrder = i + 1;
        }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------------------
// Dashboard & reports
// ---------------------------------------------------------------------------

/// <summary>Optional reporting window. When both bounds are null the whole history is used.</summary>
public record ReportRange
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public record StageBucketDto(string StageCode, string StageNameEn, string StageNameAr, int Count, decimal ValueSar);
public record StatusBucketDto(string StatusCode, string StatusNameEn, string StatusNameAr, string StageCode, int Count, decimal ValueSar);
public record KpiDto(
    int OpenCount, decimal OpenValueSar, double WinRate, int OverdueDeadlines, double AvgCycleDays,
    int WonCount, decimal WonValueSar, int PendingApprovals,
    IReadOnlyList<StageBucketDto> ByStage, IReadOnlyList<StatusBucketDto> ByStatus);
public record PipelineDto(IReadOnlyList<StageBucketDto> Items);
public record ProposalTaskDto(Guid OpportunityId, string OpportunityNumber, string OpportunityName, string CustomerName,
    DateTime? DueAtUtc, BuilderType? BuilderType, string StatusCode, string StatusNameEn, string StatusNameAr);
public record DeadlineDto(Guid OpportunityId, string OpportunityNumber, string OpportunityName, string Label, string Kind, DateTime DueAtUtc);
public record MyWorkDto(IReadOnlyList<GateInstanceDto> PendingGates, IReadOnlyList<ProposalTaskDto> BuilderTasks, IReadOnlyList<DeadlineDto> UpcomingDeadlines);
public record GetKpisQuery : ReportRange, IRequest<KpiDto>;
public record GetMyWorkQuery : IRequest<MyWorkDto>;
public record GetPipelineQuery : IRequest<PipelineDto>;
public record FunnelDto(int Created, int Gw1Passed, int Qualified, int Submitted, int Won, int Lost);
public record GetFunnelQuery : ReportRange, IRequest<FunnelDto>;
public record WinLossRow(string GroupKey, string GroupLabel, string GroupLabelAr, int Won, int Lost, double WinRate, decimal WonValue, decimal LostValue, decimal AwardedValueSar);
public record GetWinLossQuery : ReportRange, IRequest<IReadOnlyList<WinLossRow>>
{
    public string GroupBy { get; set; } = "theme";
}
public record CycleTimeRow(string StageOrGate, string Kind, string NameEn, string NameAr, double AvgDays, double MedianDays, int Count);
public record GetCycleTimeQuery : ReportRange, IRequest<IReadOnlyList<CycleTimeRow>>;
public record SlPerformanceRow(Guid ServiceLineId, string NameEn, string NameAr, double AvgTurnaroundDays, double OnTimePercent, int SubmittedCount, int ReturnedCount, int PendingCount);
public record GetSlPerformanceQuery : ReportRange, IRequest<IReadOnlyList<SlPerformanceRow>>;

public sealed class DashboardHandlers :
    IRequestHandler<GetKpisQuery, KpiDto>,
    IRequestHandler<GetMyWorkQuery, MyWorkDto>,
    IRequestHandler<GetPipelineQuery, PipelineDto>,
    IRequestHandler<GetFunnelQuery, FunnelDto>,
    IRequestHandler<GetWinLossQuery, IReadOnlyList<WinLossRow>>,
    IRequestHandler<GetCycleTimeQuery, IReadOnlyList<CycleTimeRow>>,
    IRequestHandler<GetSlPerformanceQuery, IReadOnlyList<SlPerformanceRow>>
{
    private static readonly string[] WonCodes = [StatusCodes.Won, StatusCodes.ContractNegotiation, StatusCodes.ContractSigned];
    private static readonly string[] SubmittedOrLater = [StatusCodes.Submitted, StatusCodes.Lost, StatusCodes.Won, StatusCodes.ContractNegotiation, StatusCodes.ContractSigned];
    private static readonly string[] QualifiedOrLater = [StatusCodes.Qualified, StatusCodes.InProgress, StatusCodes.InternalReview, StatusCodes.Hold, StatusCodes.NotApproved, .. SubmittedOrLater];

    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _user;
    private readonly IOpportunityVisibility _visibility;
    public DashboardHandlers(IApplicationDbContext db, IDateTime clock, ICurrentUser user, IOpportunityVisibility visibility)
    {
        _db = db; _clock = clock; _user = user; _visibility = visibility;
    }

    private IQueryable<Opportunity> InRange(IQueryable<Opportunity> q, ReportRange r)
    {
        q = _visibility.Apply(q);
        if (r.FromUtc is DateTime f) q = q.Where(o => o.CreatedAtUtc >= f);
        if (r.ToUtc is DateTime t) q = q.Where(o => o.CreatedAtUtc <= t);
        return q;
    }

    public async Task<KpiDto> Handle(GetKpisQuery r, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var all = InRange(_db.Opportunities.AsNoTracking(), r);
        var open = all.Where(o => !o.IsClosed);

        var openCount = await open.CountAsync(ct);
        var openValue = await open.SumAsync(o => (decimal?)o.ExpectedValueSar, ct) ?? 0m;
        var wonCount = await all.CountAsync(o => WonCodes.Contains(o.Status.Code), ct);
        var wonValue = await all.Where(o => WonCodes.Contains(o.Status.Code)).SumAsync(o => (decimal?)o.ExpectedValueSar, ct) ?? 0m;
        var lostCount = await all.CountAsync(o => o.Status.Code == StatusCodes.Lost, ct);
        var decided = wonCount + lostCount;
        var overdue = await open.CountAsync(o => o.Deadlines.SubmissionDeadline != null && o.Deadlines.SubmissionDeadline < now, ct);

        var byStage = await open.GroupBy(o => new { o.Stage.Code, o.Stage.NameEn, o.Stage.NameAr, o.Stage.SortOrder })
            .Select(g => new { g.Key, Count = g.Count(), Value = g.Sum(x => x.ExpectedValueSar) })
            .OrderBy(g => g.Key.SortOrder)
            .Select(g => new StageBucketDto(g.Key.Code, g.Key.NameEn, g.Key.NameAr, g.Count, g.Value))
            .ToListAsync(ct);

        var byStatus = await open.GroupBy(o => new { o.Status.Code, o.Status.NameEn, o.Status.NameAr, StageCode = o.Stage.Code, o.Stage.SortOrder, StatusOrder = o.Status.SortOrder })
            .Select(g => new { g.Key, Count = g.Count(), Value = g.Sum(x => x.ExpectedValueSar) })
            .OrderBy(g => g.Key.SortOrder).ThenBy(g => g.Key.StatusOrder)
            .Select(g => new StatusBucketDto(g.Key.Code, g.Key.NameEn, g.Key.NameAr, g.Key.StageCode, g.Count, g.Value))
            .ToListAsync(ct);

        // Average cycle: created → submission record.
        var cycles = await (from s in _db.Submissions.AsNoTracking()
                            join o in all on s.OpportunityId equals o.Id
                            select new { o.CreatedAtUtc, s.SubmittedAtUtc }).ToListAsync(ct);
        var avgCycle = cycles.Count == 0 ? 0 : Math.Round(cycles.Average(c => (c.SubmittedAtUtc - c.CreatedAtUtc).TotalDays), 1);

        var pendingApprovals = await PendingGatesForUser().CountAsync(ct);

        return new KpiDto(openCount, openValue,
            decided == 0 ? 0 : Math.Round(wonCount * 100.0 / decided, 1),
            overdue, avgCycle, wonCount, wonValue, pendingApprovals, byStage, byStatus);
    }

    public async Task<MyWorkDto> Handle(GetMyWorkQuery r, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var gates = await PendingGatesForUser()
            .Include(g => g.Gate).Include(g => g.Opportunity).ThenInclude(o => o.Customer).Include(g => g.AssignedUser)
            .OrderBy(g => g.OpenedAtUtc).Take(20).ToListAsync(ct);

        var builderId = _user.UserId;
        var tasks = await _visibility.Apply(_db.Opportunities.AsNoTracking()).Include(o => o.Status).Include(o => o.Customer)
            .Where(o => o.BuilderUserId == builderId && !o.IsClosed
                && (o.Status.Code == StatusCodes.InProgress || o.Status.Code == StatusCodes.InternalReview))
            .OrderBy(o => o.Deadlines.InternalDeadline)
            .Select(o => new ProposalTaskDto(o.Id, o.OpportunityNumber, o.Name, o.Customer.NameEn,
                o.Deadlines.InternalDeadline ?? o.Deadlines.SubmissionDeadline, o.BuilderType,
                o.Status.Code, o.Status.NameEn, o.Status.NameAr))
            .ToListAsync(ct);

        var mine = _visibility.Apply(_db.Opportunities.AsNoTracking()).Where(o => !o.IsClosed);
        var horizon = now.AddDays(30);
        var upcomingRaw = await mine
            .Select(o => new
            {
                o.Id, o.OpportunityNumber, o.Name,
                o.Deadlines.QualificationDeadline, o.Deadlines.InquiriesDeadline, o.Deadlines.EstimatedCostDeadline,
                o.Deadlines.InternalDeadline, o.Deadlines.SubmissionDeadline
            })
            .ToListAsync(ct);
        var upcoming = upcomingRaw
            .SelectMany(o => new[]
            {
                (Kind: "Qualification", Due: o.QualificationDeadline),
                (Kind: "Inquiries", Due: o.InquiriesDeadline),
                (Kind: "EstimatedCost", Due: o.EstimatedCostDeadline),
                (Kind: "Internal", Due: o.InternalDeadline),
                (Kind: "Submission", Due: o.SubmissionDeadline)
            }.Where(d => d.Due != null && d.Due <= horizon)
             .Select(d => new DeadlineDto(o.Id, o.OpportunityNumber, o.Name, d.Kind, d.Kind, d.Due!.Value)))
            .OrderBy(d => d.DueAtUtc).Take(15).ToList();

        return new MyWorkDto(gates.Select(GateHandlers.Map).ToList(), tasks, upcoming);
    }

    public async Task<PipelineDto> Handle(GetPipelineQuery r, CancellationToken ct)
    {
        var items = await _visibility.Apply(_db.Opportunities.AsNoTracking()).Where(o => !o.IsClosed)
            .GroupBy(o => new { o.Stage.Code, o.Stage.NameEn, o.Stage.NameAr, o.Stage.SortOrder })
            .Select(g => new { g.Key, Count = g.Count(), Value = g.Sum(x => x.ExpectedValueSar) })
            .OrderBy(g => g.Key.SortOrder)
            .Select(g => new StageBucketDto(g.Key.Code, g.Key.NameEn, g.Key.NameAr, g.Count, g.Value))
            .ToListAsync(ct);
        return new PipelineDto(items);
    }

    public async Task<FunnelDto> Handle(GetFunnelQuery r, CancellationToken ct)
    {
        var all = InRange(_db.Opportunities.AsNoTracking(), r);
        var created = await all.CountAsync(ct);
        var gw1Passed = await _db.GateInstances.AsNoTracking()
            .Where(g => g.Gate.Code == GateCodes.Gw1Review && g.State == GateState.Approved)
            .Join(all, g => g.OpportunityId, o => o.Id, (g, o) => o.Id).Distinct().CountAsync(ct);
        var qualified = await all.CountAsync(o => QualifiedOrLater.Contains(o.Status.Code), ct);
        var submitted = await all.CountAsync(o => SubmittedOrLater.Contains(o.Status.Code), ct);
        var won = await all.CountAsync(o => WonCodes.Contains(o.Status.Code), ct);
        var lost = await all.CountAsync(o => o.Status.Code == StatusCodes.Lost, ct);
        return new FunnelDto(created, gw1Passed, qualified, submitted, won, lost);
    }

    public async Task<IReadOnlyList<WinLossRow>> Handle(GetWinLossQuery r, CancellationToken ct)
    {
        var decidedQ = InRange(_db.Opportunities.AsNoTracking(), r)
            .Where(o => WonCodes.Contains(o.Status.Code) || o.Status.Code == StatusCodes.Lost);

        var rows = await decidedQ.Select(o => new DecidedRow(
            o.Id,
            WonCodes.Contains(o.Status.Code),
            o.ExpectedValueSar,
            _db.OpportunityOutcomes.Where(x => x.OpportunityId == o.Id).Select(x => x.AwardedValueSar).FirstOrDefault(),
            o.SubmissionTheme.ToString(),
            o.SourceChannel.ToString(),
            o.Customer.NameEn, o.Customer.NameAr,
            o.OwnerUserId, o.OwnerUser != null ? o.OwnerUser.DisplayName : "",
            o.EngagementType.ToString(),
            o.OpportunityType.ToString(),
            _db.ScopeItems.Where(si => si.ScopeOfWork.OpportunityId == o.Id)
                .Select(si => new GroupLabel(si.ServiceLineId.ToString(), si.ServiceLine.NameEn, si.ServiceLine.NameAr)).Distinct().ToList()
        )).ToListAsync(ct);

        IEnumerable<(GroupLabel Group, DecidedRow Row)> keyed = (r.GroupBy ?? "theme").ToLowerInvariant() switch
        {
            "customer" => rows.Select(x => (new GroupLabel(x.CustomerEn, x.CustomerEn, x.CustomerAr), x)),
            "source" => rows.Select(x => (new GroupLabel(x.Source, x.Source, x.Source), x)),
            "owner" => rows.Select(x => (new GroupLabel(x.OwnerId?.ToString() ?? "", string.IsNullOrEmpty(x.OwnerName) ? "—" : x.OwnerName, string.IsNullOrEmpty(x.OwnerName) ? "—" : x.OwnerName), x)),
            "engagement" => rows.Select(x => (new GroupLabel(x.Engagement, x.Engagement, x.Engagement), x)),
            "type" => rows.Select(x => (new GroupLabel(x.Type, x.Type, x.Type), x)),
            "serviceline" or "service-line" => rows.SelectMany(x => (x.ServiceLines.Count == 0 ? [new GroupLabel("", "—", "—")] : x.ServiceLines).Select(sl => (sl, x))),
            _ => rows.Select(x => (new GroupLabel(x.Theme, x.Theme, x.Theme), x))
        };

        return keyed.GroupBy(k => k.Group.Key).Select(g =>
        {
            var first = g.First().Group;
            var items = g.Select(k => k.Row).ToList();
            var won = items.Count(i => i.IsWon);
            var lost = items.Count - won;
            var wonValue = items.Where(i => i.IsWon).Sum(i => i.ExpectedValueSar);
            var lostValue = items.Where(i => !i.IsWon).Sum(i => i.ExpectedValueSar);
            var awarded = items.Where(i => i.IsWon).Sum(i => i.Awarded ?? i.ExpectedValueSar);
            var total = won + lost;
            return new WinLossRow(g.Key, first.LabelEn, first.LabelAr, won, lost,
                total == 0 ? 0 : Math.Round(won * 100.0 / total, 1), wonValue, lostValue, awarded);
        }).OrderByDescending(x => x.Won + x.Lost).ToList();
    }

    public async Task<IReadOnlyList<CycleTimeRow>> Handle(GetCycleTimeQuery r, CancellationToken ct)
    {
        var opps = await InRange(_db.Opportunities.AsNoTracking(), r)
            .Select(o => new { o.Id, o.CreatedAtUtc, o.StageId }).ToListAsync(ct);
        var oppIds = opps.Select(o => o.Id).ToHashSet();
        var stages = await _db.Stages.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync(ct);
        var stageById = stages.ToDictionary(s => s.Id.ToString(), s => s);

        var changes = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == AuditAction.StageChanged && a.OpportunityId != null)
            .Select(a => new { a.OpportunityId, a.OccurredAtUtc, a.FromValue, a.ToValue })
            .ToListAsync(ct);

        var stageDurations = new Dictionary<Guid, List<double>>();
        foreach (var opp in opps)
        {
            var seq = changes.Where(c => c.OpportunityId == opp.Id).OrderBy(c => c.OccurredAtUtc).ToList();
            var start = opp.CreatedAtUtc;
            foreach (var c in seq)
            {
                if (c.FromValue is not null && stageById.TryGetValue(c.FromValue, out var st))
                {
                    if (!stageDurations.TryGetValue(st.Id, out var list)) stageDurations[st.Id] = list = new List<double>();
                    list.Add((c.OccurredAtUtc - start).TotalDays);
                }
                start = c.OccurredAtUtc;
            }
        }

        var result = new List<CycleTimeRow>();
        foreach (var s in stages)
        {
            stageDurations.TryGetValue(s.Id, out var list);
            list ??= new List<double>();
            result.Add(new CycleTimeRow(s.Code, "Stage", s.NameEn, s.NameAr, Avg(list), Median(list), list.Count));
        }

        var gates = await _db.GateInstances.AsNoTracking()
            .Where(g => g.DecidedAtUtc != null && oppIds.Contains(g.OpportunityId))
            .Select(g => new { g.Gate.Code, g.Gate.NameEn, g.Gate.NameAr, g.Gate.SortOrder, g.OpenedAtUtc, Decided = g.DecidedAtUtc!.Value })
            .ToListAsync(ct);
        foreach (var g in gates.GroupBy(x => new { x.Code, x.NameEn, x.NameAr, x.SortOrder }).OrderBy(x => x.Key.SortOrder))
        {
            var list = g.Select(x => (x.Decided - x.OpenedAtUtc).TotalDays).ToList();
            result.Add(new CycleTimeRow(g.Key.Code, "Gate", g.Key.NameEn, g.Key.NameAr, Avg(list), Median(list), list.Count));
        }
        return result;
    }

    public async Task<IReadOnlyList<SlPerformanceRow>> Handle(GetSlPerformanceQuery r, CancellationToken ct)
    {
        var oppIds = InRange(_db.Opportunities.AsNoTracking(), r).Select(o => o.Id);
        var responses = await _db.SlResponses.AsNoTracking()
            .Where(s => oppIds.Contains(s.OpportunityId))
            .Select(s => new { s.ServiceLineId, s.ServiceLine.NameEn, s.ServiceLine.NameAr, s.Status, s.SubmittedAtUtc, s.DueAtUtc, s.CreatedAtUtc })
            .ToListAsync(ct);
        var lines = await _db.ServiceLines.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.NameEn).ToListAsync(ct);

        return lines.Select(sl =>
        {
            var mine = responses.Where(x => x.ServiceLineId == sl.Id).ToList();
            var submitted = mine.Where(x => x.SubmittedAtUtc != null).ToList();
            var turnaround = submitted.Select(x => (x.SubmittedAtUtc!.Value - x.CreatedAtUtc).TotalDays).ToList();
            var withDue = submitted.Where(x => x.DueAtUtc != null).ToList();
            var onTime = withDue.Count == 0 ? 0 : Math.Round(withDue.Count(x => x.SubmittedAtUtc <= x.DueAtUtc) * 100.0 / withDue.Count, 1);
            return new SlPerformanceRow(sl.Id, sl.NameEn, sl.NameAr, Avg(turnaround), onTime,
                submitted.Count, mine.Count(x => x.Status == SlResponseStatus.Returned), mine.Count(x => x.Status == SlResponseStatus.Pending));
        }).ToList();
    }

    private sealed record GroupLabel(string Key, string LabelEn, string LabelAr);
    private sealed record DecidedRow(Guid Id, bool IsWon, decimal ExpectedValueSar, decimal? Awarded, string Theme, string Source,
        string CustomerEn, string CustomerAr, Guid? OwnerId, string OwnerName, string Engagement, string Type, List<GroupLabel> ServiceLines);

    private IQueryable<GateInstance> PendingGatesForUser()
    {
        var q = _db.GateInstances.AsNoTracking().Where(g => g.State == GateState.Pending);
        if (_user.IsAdmin) return q;
        return q.WhereAssignedTo(_user.UserId, _user.Roles);
    }

    private static double Avg(List<double> list) => list.Count == 0 ? 0 : Math.Round(list.Average(), 1);
    private static double Median(List<double> list)
    {
        if (list.Count == 0) return 0;
        var sorted = list.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        return Math.Round(sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2, 1);
    }
}

// ---------------------------------------------------------------------------
// Admin: users, customers, service lines
// ---------------------------------------------------------------------------

public record UserListItemDto(Guid Id, string Email, string DisplayName, string? JobTitle, bool IsActive, IReadOnlyList<string> Roles);
public record UserPickDto(Guid Id, string DisplayName, string Email, IReadOnlyList<string> Roles);
public record GetUsersQuery : PagedQuery, IRequest<PagedResult<UserListItemDto>>;
/// <summary>Lightweight, non-admin user list for pickers. Optional role filter accepts comma or pipe separated codes.</summary>
public record GetPickableUsersQuery(string? Role, string? Search) : IRequest<IReadOnlyList<UserPickDto>>;
public record CreateUserCommand(string Email, string DisplayName, string Password, string? JobTitle, IReadOnlyList<string> RoleCodes) : IRequest<Guid>;
public record UpdateUserCommand(Guid Id, string DisplayName, string? JobTitle) : IRequest<Unit>;
public record SetUserRolesCommand(Guid Id, IReadOnlyList<string> RoleCodes) : IRequest<Unit>;
public record ActivateUserCommand(Guid Id, bool Active) : IRequest<Unit>;
public record GetRolesQuery : IRequest<IReadOnlyList<LookupItemDto>>;
public record CustomerDto(Guid Id, string NameEn, string NameAr, string Sector, bool IsGovernment, string? Website);
public record GetCustomersQuery : PagedQuery, IRequest<PagedResult<CustomerDto>>;
public record GetCustomerQuery(Guid Id) : IRequest<Customer>;
public record CreateCustomerContactDto(string Name, string? Email, string? Phone, string? Title);
public record CreateCustomerCommand(
    string NameEn,
    string NameAr,
    string Sector,
    bool IsGovernment,
    string? Website,
    bool IsQuickCreated = false,
    CreateCustomerContactDto? PrimaryContact = null) : IRequest<Guid>;
public record UpdateCustomerCommand(Guid Id, string NameEn, string NameAr, string Sector, bool IsGovernment, string? Website) : IRequest<Unit>;
public record CreateCustomerContactCommand(Guid CustomerId, string Name, string? Email, string? Phone, string? Title, bool IsPrimary) : IRequest<Guid>;
public record GetCustomerContactsQuery(Guid CustomerId) : IRequest<IReadOnlyList<CustomerContactDto>>;
public record CustomerContactDto(Guid Id, Guid CustomerId, string Name, string? Email, string? Phone, string? Title, bool IsPrimary);
public record CreateServiceLineCommand(string Code, string NameEn, string NameAr, Guid? LeadUserId, int SortOrder = 0, string? ColourHex = null) : IRequest<Guid>;
public record UpdateServiceLineCommand(Guid Id, string Code, string NameEn, string NameAr, Guid? LeadUserId, int SortOrder, string? ColourHex, bool IsActive) : IRequest<Unit>;
public record DeactivateServiceLineCommand(Guid Id) : IRequest<Unit>;

public sealed class AdminHandlers :
    IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>,
    IRequestHandler<GetPickableUsersQuery, IReadOnlyList<UserPickDto>>,
    IRequestHandler<CreateUserCommand, Guid>,
    IRequestHandler<UpdateUserCommand, Unit>,
    IRequestHandler<SetUserRolesCommand, Unit>,
    IRequestHandler<ActivateUserCommand, Unit>,
    IRequestHandler<GetRolesQuery, IReadOnlyList<LookupItemDto>>,
    IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>,
    IRequestHandler<GetCustomerQuery, Customer>,
    IRequestHandler<CreateCustomerCommand, Guid>,
    IRequestHandler<UpdateCustomerCommand, Unit>,
    IRequestHandler<CreateCustomerContactCommand, Guid>,
    IRequestHandler<GetCustomerContactsQuery, IReadOnlyList<CustomerContactDto>>,
    IRequestHandler<CreateServiceLineCommand, Guid>,
    IRequestHandler<UpdateServiceLineCommand, Unit>,
    IRequestHandler<DeactivateServiceLineCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly PasswordHasher<User> _hasher = new();
    public AdminHandlers(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery q, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(u => u.Email.Contains(q.Search) || u.DisplayName.Contains(q.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.DisplayName).Skip(q.Skip).Take(q.Take).ToListAsync(ct);
        return PagedResult<UserListItemDto>.Create(items.Select(u => new UserListItemDto(u.Id, u.Email, u.DisplayName, u.JobTitle, u.IsActive, u.UserRoles.Select(r => r.Role.Code).ToList())).ToList(), q.Page, q.Take, total);
    }

    public async Task<IReadOnlyList<UserPickDto>> Handle(GetPickableUsersQuery q, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Role))
        {
            var codes = q.Role.Split([',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(u => u.UserRoles.Any(ur => codes.Contains(ur.Role.Code)));
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(u => u.DisplayName.Contains(q.Search) || u.Email.Contains(q.Search));
        return await query.OrderBy(u => u.DisplayName).Take(100)
            .Select(u => new UserPickDto(u.Id, u.DisplayName, u.Email, u.UserRoles.Select(r => r.Role.Code).ToList()))
            .ToListAsync(ct);
    }

    public async Task<Guid> Handle(CreateUserCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var u = new User { Email = r.Email, DisplayName = r.DisplayName, JobTitle = r.JobTitle, IsActive = true };
        u.PasswordHash = _hasher.HashPassword(u, r.Password);
        _db.Users.Add(u);
        var roles = await _db.Roles.Where(x => r.RoleCodes.Contains(x.Code)).ToListAsync(ct);
        foreach (var role in roles) _db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = role.Id });
        await _db.SaveChangesAsync(ct);
        return u.Id;
    }

    public async Task<Unit> Handle(UpdateUserCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(User), r.Id);
        u.DisplayName = r.DisplayName; u.JobTitle = r.JobTitle;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetUserRolesCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var existing = await _db.UserRoles.Where(ur => ur.UserId == r.Id).ToListAsync(ct);
        _db.UserRoles.RemoveRange(existing);
        var roles = await _db.Roles.Where(x => r.RoleCodes.Contains(x.Code)).ToListAsync(ct);
        foreach (var role in roles) _db.UserRoles.Add(new UserRole { UserId = r.Id, RoleId = role.Id });
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(ActivateUserCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var u = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(User), r.Id);
        u.IsActive = r.Active;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<LookupItemDto>> Handle(GetRolesQuery r, CancellationToken ct) =>
        await _db.Roles.AsNoTracking().Select(x => new LookupItemDto(x.Code, x.NameEn, x.NameAr)).ToListAsync(ct);

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery q, CancellationToken ct)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(c => c.NameEn.Contains(q.Search) || c.NameAr.Contains(q.Search));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.NameEn).Skip(q.Skip).Take(q.Take)
            .Select(c => new CustomerDto(c.Id, c.NameEn, c.NameAr, c.Sector, c.IsGovernment, c.Website)).ToListAsync(ct);
        return PagedResult<CustomerDto>.Create(items, q.Page, q.Take, total);
    }

    public async Task<Customer> Handle(GetCustomerQuery r, CancellationToken ct) =>
        await _db.Customers.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Customer), r.Id);

    public async Task<Guid> Handle(CreateCustomerCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.NameEn)) throw new BusinessRuleException("Customer English name is required.");
        var exists = await _db.Customers.AnyAsync(c => c.NameEn == r.NameEn.Trim(), ct);
        if (exists) throw new ConflictException("A customer with this name already exists.");
        var c = new Customer
        {
            NameEn = r.NameEn.Trim(),
            NameAr = string.IsNullOrWhiteSpace(r.NameAr) ? r.NameEn.Trim() : r.NameAr.Trim(),
            Sector = r.Sector ?? "",
            IsGovernment = r.IsGovernment,
            Website = r.Website,
            IsQuickCreated = r.IsQuickCreated
        };
        if (r.PrimaryContact is { } pc && !string.IsNullOrWhiteSpace(pc.Name))
        {
            c.Contacts.Add(new CustomerContact
            {
                Name = pc.Name.Trim(),
                Email = pc.Email,
                Phone = pc.Phone,
                Title = pc.Title,
                IsPrimary = true
            });
        }
        _db.Customers.Add(c);
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task<Unit> Handle(UpdateCustomerCommand r, CancellationToken ct)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == r.Id, ct) ?? throw new NotFoundException(nameof(Customer), r.Id);
        c.NameEn = r.NameEn; c.NameAr = r.NameAr; c.Sector = r.Sector; c.IsGovernment = r.IsGovernment; c.Website = r.Website;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Guid> Handle(CreateCustomerContactCommand r, CancellationToken ct)
    {
        _ = await _db.Customers.FirstOrDefaultAsync(c => c.Id == r.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), r.CustomerId);
        if (r.IsPrimary)
        {
            var existing = await _db.CustomerContacts.Where(x => x.CustomerId == r.CustomerId && x.IsPrimary).ToListAsync(ct);
            foreach (var e in existing) e.IsPrimary = false;
        }
        var contact = new CustomerContact
        {
            CustomerId = r.CustomerId,
            Name = r.Name.Trim(),
            Email = r.Email,
            Phone = r.Phone,
            Title = r.Title,
            IsPrimary = r.IsPrimary
        };
        _db.CustomerContacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return contact.Id;
    }

    public async Task<IReadOnlyList<CustomerContactDto>> Handle(GetCustomerContactsQuery r, CancellationToken ct) =>
        await _db.CustomerContacts.AsNoTracking().Where(c => c.CustomerId == r.CustomerId)
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
            .Select(c => new CustomerContactDto(c.Id, c.CustomerId, c.Name, c.Email, c.Phone, c.Title, c.IsPrimary))
            .ToListAsync(ct);

    public async Task<Guid> Handle(CreateServiceLineCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var s = new ServiceLine
        {
            Code = r.Code.Trim(),
            NameEn = r.NameEn.Trim(),
            NameAr = r.NameAr.Trim(),
            LeadUserId = r.LeadUserId,
            SortOrder = r.SortOrder,
            ColourHex = r.ColourHex,
            IsActive = true
        };
        _db.ServiceLines.Add(s);
        await _db.SaveChangesAsync(ct);
        return s.Id;
    }

    public async Task<Unit> Handle(UpdateServiceLineCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var s = await _db.ServiceLines.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(ServiceLine), r.Id);
        s.Code = r.Code.Trim();
        s.NameEn = r.NameEn.Trim();
        s.NameAr = r.NameAr.Trim();
        s.LeadUserId = r.LeadUserId;
        s.SortOrder = r.SortOrder;
        s.ColourHex = r.ColourHex;
        s.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeactivateServiceLineCommand r, CancellationToken ct)
    {
        if (!_user.IsAdmin) throw new ForbiddenException();
        var s = await _db.ServiceLines.FirstOrDefaultAsync(x => x.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(ServiceLine), r.Id);
        s.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
