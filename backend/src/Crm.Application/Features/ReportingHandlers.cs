using Crm.Application.Abstractions;
using Crm.Application.Common;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Features;

// ---------------------------------------------------------------------------
// Phase 5 — charts, extra reports, pivot
// ---------------------------------------------------------------------------

public record NamedBucketDto(string Key, string LabelEn, string LabelAr, int Count, decimal ValueSar);
public record AgingBucketDto(string Bucket, int Count, decimal ValueSar);
public record MonthTrendDto(string Month, int Won, int Lost, decimal WonValueSar, decimal LostValueSar);
public record DashboardChartsDto(
    IReadOnlyList<NamedBucketDto> ByTheme,
    IReadOnlyList<NamedBucketDto> ByServiceLine,
    IReadOnlyList<NamedBucketDto> TopCustomers,
    IReadOnlyList<AgingBucketDto> Aging,
    IReadOnlyList<MonthTrendDto> WinLossTrend);
public record GetDashboardChartsQuery : ReportRange, IRequest<DashboardChartsDto>;

public record DeadlineComplianceRow(string Month, int Met, int Missed, double CompliancePercent);
public record GetDeadlineComplianceQuery : ReportRange, IRequest<IReadOnlyList<DeadlineComplianceRow>>;

public record ApprovalThroughputRow(
    string GateCode, string NameEn, string NameAr,
    int Pending, int Decided, double AvgDecisionDays, int MaxRounds);
public record GetApprovalThroughputQuery : ReportRange, IRequest<IReadOnlyList<ApprovalThroughputRow>>;

public record PivotQuery : ReportRange, IRequest<PivotResultDto>
{
    /// <summary>stage|status|customer|theme|source|owner|builder|serviceline|monthCreated|monthSubmitted</summary>
    public string Rows { get; set; } = "stage";
    /// <summary>Same whitelist as Rows, or empty for single total column.</summary>
    public string? Columns { get; set; }
    /// <summary>count|expectedValue|awardedValue</summary>
    public string Measure { get; set; } = "count";
}

public record PivotCellDto(string ColumnKey, decimal Value);
public record PivotRowDto(string RowKey, string RowLabel, string RowLabelAr, IReadOnlyList<PivotCellDto> Cells, decimal Total);
public record PivotResultDto(
    string Rows, string? Columns, string Measure,
    IReadOnlyList<string> ColumnKeys,
    IReadOnlyList<string> ColumnLabels,
    IReadOnlyList<PivotRowDto> RowsData,
    decimal GrandTotal);

public sealed class ReportingHandlers :
    IRequestHandler<GetDashboardChartsQuery, DashboardChartsDto>,
    IRequestHandler<GetDeadlineComplianceQuery, IReadOnlyList<DeadlineComplianceRow>>,
    IRequestHandler<GetApprovalThroughputQuery, IReadOnlyList<ApprovalThroughputRow>>,
    IRequestHandler<PivotQuery, PivotResultDto>
{
    private static readonly string[] WonCodes =
        [StatusCodes.Won, StatusCodes.ContractNegotiation, StatusCodes.ContractSigned];

    private readonly IApplicationDbContext _db;
    private readonly IDateTime _clock;
    private readonly IOpportunityVisibility _visibility;

    public ReportingHandlers(IApplicationDbContext db, IDateTime clock, IOpportunityVisibility visibility)
    {
        _db = db; _clock = clock; _visibility = visibility;
    }

    private IQueryable<Opportunity> InRange(IQueryable<Opportunity> q, ReportRange r)
    {
        q = _visibility.Apply(q);
        if (r.FromUtc is DateTime f) q = q.Where(o => o.CreatedAtUtc >= f);
        if (r.ToUtc is DateTime t) q = q.Where(o => o.CreatedAtUtc <= t);
        return q;
    }

    public async Task<DashboardChartsDto> Handle(GetDashboardChartsQuery r, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var open = await InRange(_db.Opportunities.AsNoTracking(), r)
            .Where(o => !o.IsClosed)
            .Select(o => new
            {
                o.Id,
                o.ExpectedValueSar,
                o.CreatedAtUtc,
                Theme = o.SubmissionTheme.ToString(),
                CustomerEn = o.Customer.NameEn,
                CustomerAr = o.Customer.NameAr,
                CustomerId = o.CustomerId
            }).ToListAsync(ct);

        var byTheme = open.GroupBy(o => o.Theme)
            .Select(g => new NamedBucketDto(g.Key, g.Key, g.Key, g.Count(), g.Sum(x => x.ExpectedValueSar)))
            .OrderByDescending(x => x.ValueSar).ToList();

        var topCustomers = open.GroupBy(o => new { o.CustomerId, o.CustomerEn, o.CustomerAr })
            .Select(g => new NamedBucketDto(g.Key.CustomerId.ToString(), g.Key.CustomerEn, g.Key.CustomerAr, g.Count(), g.Sum(x => x.ExpectedValueSar)))
            .OrderByDescending(x => x.ValueSar).Take(10).ToList();

        var scope = await _db.ScopeItems.AsNoTracking()
            .Where(si => open.Select(o => o.Id).Contains(si.ScopeOfWork.OpportunityId))
            .Select(si => new { OppId = si.ScopeOfWork.OpportunityId, si.ServiceLineId, si.ServiceLine.NameEn, si.ServiceLine.NameAr })
            .ToListAsync(ct);
        var oppValue = open.ToDictionary(o => o.Id, o => o.ExpectedValueSar);
        var bySl = scope.GroupBy(s => new { s.ServiceLineId, s.NameEn, s.NameAr })
            .Select(g =>
            {
                var oppIds = g.Select(x => x.OppId).Distinct().ToList();
                return new NamedBucketDto(g.Key.ServiceLineId.ToString(), g.Key.NameEn, g.Key.NameAr,
                    oppIds.Count, oppIds.Sum(id => oppValue.GetValueOrDefault(id)));
            })
            .OrderByDescending(x => x.ValueSar).ToList();

        AgingBucketDto Bucket(string name, Func<DateTime, bool> pred)
        {
            var items = open.Where(o => pred(o.CreatedAtUtc)).ToList();
            return new AgingBucketDto(name, items.Count, items.Sum(x => x.ExpectedValueSar));
        }
        var aging = new List<AgingBucketDto>
        {
            Bucket("0-7", d => (now - d).TotalDays <= 7),
            Bucket("8-30", d => { var days = (now - d).TotalDays; return days > 7 && days <= 30; }),
            Bucket("31-90", d => { var days = (now - d).TotalDays; return days > 30 && days <= 90; }),
            Bucket("90+", d => (now - d).TotalDays > 90)
        };

        var decided = await InRange(_db.Opportunities.AsNoTracking(), r)
            .Where(o => WonCodes.Contains(o.Status.Code) || o.Status.Code == StatusCodes.Lost)
            .Select(o => new { o.CreatedAtUtc, IsWon = WonCodes.Contains(o.Status.Code), o.ExpectedValueSar })
            .ToListAsync(ct);
        var trend = decided
            .GroupBy(o => o.CreatedAtUtc.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new MonthTrendDto(
                g.Key,
                g.Count(x => x.IsWon),
                g.Count(x => !x.IsWon),
                g.Where(x => x.IsWon).Sum(x => x.ExpectedValueSar),
                g.Where(x => !x.IsWon).Sum(x => x.ExpectedValueSar)))
            .ToList();

        return new DashboardChartsDto(byTheme, bySl, topCustomers, aging, trend);
    }

    public async Task<IReadOnlyList<DeadlineComplianceRow>> Handle(GetDeadlineComplianceQuery r, CancellationToken ct)
    {
        var opps = InRange(_db.Opportunities.AsNoTracking(), r);
        var rows = await (from o in opps
                          join s in _db.Submissions.AsNoTracking() on o.Id equals s.OpportunityId
                          where o.Deadlines.SubmissionDeadline != null
                          select new { Deadline = o.Deadlines.SubmissionDeadline!.Value, s.SubmittedAtUtc })
            .ToListAsync(ct);

        return rows
            .Select(x => new { Month = x.SubmittedAtUtc.ToString("yyyy-MM"), Met = x.SubmittedAtUtc <= x.Deadline })
            .GroupBy(x => x.Month)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var met = g.Count(x => x.Met);
                var missed = g.Count() - met;
                var total = met + missed;
                return new DeadlineComplianceRow(g.Key, met, missed,
                    total == 0 ? 0 : Math.Round(met * 100.0 / total, 1));
            }).ToList();
    }

    public async Task<IReadOnlyList<ApprovalThroughputRow>> Handle(GetApprovalThroughputQuery r, CancellationToken ct)
    {
        var visibleOppIds = InRange(_db.Opportunities.AsNoTracking(), r).Select(o => o.Id);
        var gates = await _db.GateInstances.AsNoTracking()
            .Where(g => visibleOppIds.Contains(g.OpportunityId))
            .Select(g => new
            {
                g.Gate.Code, g.Gate.NameEn, g.Gate.NameAr, g.Gate.SortOrder,
                g.State, g.OpenedAtUtc, g.DecidedAtUtc, g.Round
            }).ToListAsync(ct);

        return gates.GroupBy(g => new { g.Code, g.NameEn, g.NameAr, g.SortOrder })
            .OrderBy(g => g.Key.SortOrder)
            .Select(g =>
            {
                var decided = g.Where(x => x.DecidedAtUtc != null).ToList();
                var days = decided.Select(x => (x.DecidedAtUtc!.Value - x.OpenedAtUtc).TotalDays).ToList();
                return new ApprovalThroughputRow(
                    g.Key.Code, g.Key.NameEn, g.Key.NameAr,
                    g.Count(x => x.State == GateState.Pending),
                    decided.Count,
                    days.Count == 0 ? 0 : Math.Round(days.Average(), 1),
                    g.Max(x => x.Round));
            }).ToList();
    }

    public async Task<PivotResultDto> Handle(PivotQuery r, CancellationToken ct)
    {
        var measure = (r.Measure ?? "count").ToLowerInvariant();
        var rowDim = (r.Rows ?? "stage").ToLowerInvariant();
        var colDim = string.IsNullOrWhiteSpace(r.Columns) ? null : r.Columns!.ToLowerInvariant();

        var raw = await InRange(_db.Opportunities.AsNoTracking(), r)
            .Select(o => new PivotRaw(
                o.Id,
                o.Stage.Code, o.Stage.NameEn, o.Stage.NameAr,
                o.Status.Code, o.Status.NameEn, o.Status.NameAr,
                o.Customer.NameEn, o.Customer.NameAr,
                o.SubmissionTheme.ToString(),
                o.SourceChannel.ToString(),
                o.OwnerUser != null ? o.OwnerUser.DisplayName : "—",
                o.BuilderUser != null ? o.BuilderUser.DisplayName : "—",
                o.CreatedAtUtc,
                o.ExpectedValueSar,
                _db.OpportunityOutcomes.Where(x => x.OpportunityId == o.Id).Select(x => (decimal?)x.AwardedValueSar).FirstOrDefault(),
                _db.Submissions.Where(s => s.OpportunityId == o.Id).Select(s => (DateTime?)s.SubmittedAtUtc).FirstOrDefault(),
                _db.ScopeItems.Where(si => si.ScopeOfWork.OpportunityId == o.Id)
                    .Select(si => new Dim(si.ServiceLineId.ToString(), si.ServiceLine.NameEn, si.ServiceLine.NameAr))
                    .Distinct().ToList()
            )).ToListAsync(ct);

        // Expand service-line dimension into one row per SL when requested
        IEnumerable<(Dim Row, Dim Col, PivotRaw Item)> expanded = Expand(raw, rowDim, colDim);

        var grouped = expanded.GroupBy(x => (Row: x.Row.Key, Col: x.Col.Key)).ToList();
        var rowMeta = expanded.GroupBy(x => x.Row.Key).ToDictionary(g => g.Key, g => g.First().Row);
        var colMeta = expanded.GroupBy(x => x.Col.Key).ToDictionary(g => g.Key, g => g.First().Col);

        decimal Agg(IEnumerable<PivotRaw> items) => measure switch
        {
            "expectedvalue" or "expected_value" or "value" => items.Sum(i => i.ExpectedValueSar),
            "awardedvalue" or "awarded_value" or "awarded" => items.Sum(i => i.Awarded ?? 0m),
            _ => items.Count()
        };

        var colKeys = colMeta.Keys.OrderBy(k => k).ToList();
        if (colKeys.Count == 0) colKeys = [""];

        var rowsData = rowMeta.Keys.OrderBy(k => k).Select(rk =>
        {
            var cells = colKeys.Select(ck =>
            {
                var items = grouped.FirstOrDefault(g => g.Key.Row == rk && g.Key.Col == ck)?.Select(x => x.Item) ?? Enumerable.Empty<PivotRaw>();
                return new PivotCellDto(ck, Agg(items));
            }).ToList();
            var total = cells.Sum(c => c.Value);
            var meta = rowMeta[rk];
            return new PivotRowDto(rk, meta.LabelEn, meta.LabelAr, cells, total);
        }).ToList();

        var grand = rowsData.Sum(x => x.Total);
        return new PivotResultDto(rowDim, colDim, measure, colKeys,
            colKeys.Select(k => colMeta.TryGetValue(k, out var m) ? m.LabelEn : (string.IsNullOrEmpty(k) ? "Total" : k)).ToList(),
            rowsData, grand);
    }

    private static IEnumerable<(Dim Row, Dim Col, PivotRaw Item)> Expand(List<PivotRaw> raw, string rowDim, string? colDim)
    {
        foreach (var item in raw)
        {
            foreach (var row in Dims(item, rowDim))
            {
                if (colDim is null)
                {
                    yield return (row, new Dim("", "Total", "الإجمالي"), item);
                    continue;
                }
                foreach (var col in Dims(item, colDim))
                    yield return (row, col, item);
            }
        }
    }

    private static IEnumerable<Dim> Dims(PivotRaw o, string dim) => dim switch
    {
        "status" => [new Dim(o.StatusCode, o.StatusEn, o.StatusAr)],
        "customer" => [new Dim(o.CustomerEn, o.CustomerEn, o.CustomerAr)],
        "theme" => [new Dim(o.Theme, o.Theme, o.Theme)],
        "source" => [new Dim(o.Source, o.Source, o.Source)],
        "owner" => [new Dim(o.Owner, o.Owner, o.Owner)],
        "builder" => [new Dim(o.Builder, o.Builder, o.Builder)],
        "monthcreated" or "month_created" => [Month(o.CreatedAtUtc)],
        "monthsubmitted" or "month_submitted" => o.SubmittedAt is DateTime s ? [Month(s)] : [new Dim("", "—", "—")],
        "serviceline" or "service-line" => o.ServiceLines.Count == 0 ? [new Dim("", "—", "—")] : o.ServiceLines,
        _ => [new Dim(o.StageCode, o.StageEn, o.StageAr)]
    };

    private static Dim Month(DateTime d) => new(d.ToString("yyyy-MM"), d.ToString("yyyy-MM"), d.ToString("yyyy-MM"));

    private sealed record Dim(string Key, string LabelEn, string LabelAr);
    private sealed record PivotRaw(
        Guid Id,
        string StageCode, string StageEn, string StageAr,
        string StatusCode, string StatusEn, string StatusAr,
        string CustomerEn, string CustomerAr,
        string Theme, string Source, string Owner, string Builder,
        DateTime CreatedAtUtc, decimal ExpectedValueSar, decimal? Awarded, DateTime? SubmittedAt,
        List<Dim> ServiceLines);
}
