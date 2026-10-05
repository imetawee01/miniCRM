using Crm.Application.Abstractions;
using Crm.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class DashboardController : ControllerBase
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly ISender _sender;
    private readonly IExcelExporter _excel;

    public DashboardController(ISender sender, IExcelExporter excel)
    {
        _sender = sender;
        _excel = excel;
    }

    [HttpGet("dashboard/kpis")] public Task<KpiDto> Kpis([FromQuery] GetKpisQuery q) => _sender.Send(q);
    [HttpGet("dashboard/my-work")] public Task<MyWorkDto> MyWork() => _sender.Send(new GetMyWorkQuery());
    [HttpGet("dashboard/pipeline")] public Task<PipelineDto> Pipeline() => _sender.Send(new GetPipelineQuery());
    [HttpGet("dashboard/charts")] public Task<DashboardChartsDto> Charts([FromQuery] GetDashboardChartsQuery q) => _sender.Send(q);

    [HttpGet("reports/funnel")] public Task<FunnelDto> Funnel([FromQuery] GetFunnelQuery q) => _sender.Send(q);
    [HttpGet("reports/win-loss")] public Task<IReadOnlyList<WinLossRow>> WinLoss([FromQuery] GetWinLossQuery q) => _sender.Send(q);
    [HttpGet("reports/cycle-time")] public Task<IReadOnlyList<CycleTimeRow>> CycleTime([FromQuery] GetCycleTimeQuery q) => _sender.Send(q);
    [HttpGet("reports/sl-performance")] public Task<IReadOnlyList<SlPerformanceRow>> SlPerformance([FromQuery] GetSlPerformanceQuery q) => _sender.Send(q);
    [HttpGet("reports/deadline-compliance")] public Task<IReadOnlyList<DeadlineComplianceRow>> DeadlineCompliance([FromQuery] GetDeadlineComplianceQuery q) => _sender.Send(q);
    [HttpGet("reports/approval-throughput")] public Task<IReadOnlyList<ApprovalThroughputRow>> ApprovalThroughput([FromQuery] GetApprovalThroughputQuery q) => _sender.Send(q);

    [HttpPost("reports/pivot")] public Task<PivotResultDto> Pivot([FromBody] PivotQuery q) => _sender.Send(q);

    [HttpGet("reports/funnel/export")]
    public async Task<IActionResult> ExportFunnel([FromQuery] GetFunnelQuery q, CancellationToken ct)
    {
        var d = await _sender.Send(q, ct);
        var rows = new (string Step, int Count)[]
        {
            ("Created", d.Created),
            ("GW1 Passed", d.Gw1Passed),
            ("Qualified", d.Qualified),
            ("Submitted", d.Submitted),
            ("Won", d.Won),
            ("Lost", d.Lost)
        };
        return Xlsx("Funnel", [
            ("Step", ((string Step, int Count) r) => r.Step),
            ("Count", r => r.Count)
        ], rows, "funnel");
    }

    [HttpGet("reports/win-loss/export")]
    public async Task<IActionResult> ExportWinLoss([FromQuery] GetWinLossQuery q, CancellationToken ct)
    {
        var rows = await _sender.Send(q, ct);
        return Xlsx("WinLoss", [
            ("Group", (WinLossRow r) => r.GroupLabel),
            ("Group (AR)", r => r.GroupLabelAr),
            ("Won", r => r.Won),
            ("Lost", r => r.Lost),
            ("Win rate %", r => r.WinRate),
            ("Won value", r => r.WonValue),
            ("Lost value", r => r.LostValue),
            ("Awarded value", r => r.AwardedValueSar)
        ], rows, "win-loss");
    }

    [HttpGet("reports/cycle-time/export")]
    public async Task<IActionResult> ExportCycleTime([FromQuery] GetCycleTimeQuery q, CancellationToken ct)
    {
        var rows = await _sender.Send(q, ct);
        return Xlsx("CycleTime", [
            ("Code", (CycleTimeRow r) => r.StageOrGate),
            ("Kind", r => r.Kind),
            ("Name", r => r.NameEn),
            ("Name (AR)", r => r.NameAr),
            ("Avg days", r => r.AvgDays),
            ("Median days", r => r.MedianDays),
            ("Samples", r => r.Count)
        ], rows, "cycle-time");
    }

    [HttpGet("reports/sl-performance/export")]
    public async Task<IActionResult> ExportSl([FromQuery] GetSlPerformanceQuery q, CancellationToken ct)
    {
        var rows = await _sender.Send(q, ct);
        return Xlsx("SlPerformance", [
            ("Service line", (SlPerformanceRow r) => r.NameEn),
            ("Service line (AR)", r => r.NameAr),
            ("Avg turnaround days", r => r.AvgTurnaroundDays),
            ("On-time %", r => r.OnTimePercent),
            ("Submitted", r => r.SubmittedCount),
            ("Returned", r => r.ReturnedCount),
            ("Pending", r => r.PendingCount)
        ], rows, "sl-performance");
    }

    [HttpGet("reports/deadline-compliance/export")]
    public async Task<IActionResult> ExportDeadline([FromQuery] GetDeadlineComplianceQuery q, CancellationToken ct)
    {
        var rows = await _sender.Send(q, ct);
        return Xlsx("DeadlineCompliance", [
            ("Month", (DeadlineComplianceRow r) => r.Month),
            ("Met", r => r.Met),
            ("Missed", r => r.Missed),
            ("Compliance %", r => r.CompliancePercent)
        ], rows, "deadline-compliance");
    }

    [HttpGet("reports/approval-throughput/export")]
    public async Task<IActionResult> ExportApprovals([FromQuery] GetApprovalThroughputQuery q, CancellationToken ct)
    {
        var rows = await _sender.Send(q, ct);
        return Xlsx("ApprovalThroughput", [
            ("Gate", (ApprovalThroughputRow r) => r.GateCode),
            ("Name", r => r.NameEn),
            ("Name (AR)", r => r.NameAr),
            ("Pending", r => r.Pending),
            ("Decided", r => r.Decided),
            ("Avg decision days", r => r.AvgDecisionDays),
            ("Max rounds", r => r.MaxRounds)
        ], rows, "approval-throughput");
    }

    [HttpPost("reports/pivot/export")]
    public async Task<IActionResult> ExportPivot([FromBody] PivotQuery q, CancellationToken ct)
    {
        var pivot = await _sender.Send(q, ct);
        var columns = new List<(string Header, Func<PivotRowDto, object?> Value)>
        {
            ("Row", r => r.RowLabel),
            ("Row (AR)", r => r.RowLabelAr)
        };
        for (var i = 0; i < pivot.ColumnKeys.Count; i++)
        {
            var key = pivot.ColumnKeys[i];
            var label = i < pivot.ColumnLabels.Count ? pivot.ColumnLabels[i] : key;
            columns.Add((label, r => r.Cells.FirstOrDefault(c => c.ColumnKey == key)?.Value ?? 0m));
        }
        columns.Add(("Total", r => r.Total));
        return Xlsx("Pivot", columns, pivot.RowsData, "pivot");
    }

    private FileContentResult Xlsx<T>(string sheet, IReadOnlyList<(string Header, Func<T, object?> Value)> cols, IEnumerable<T> rows, string fileStem)
    {
        var bytes = _excel.Export(sheet, cols, rows);
        return File(bytes, XlsxMime, $"{fileStem}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }
}
