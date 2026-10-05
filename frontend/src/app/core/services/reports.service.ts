import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ApprovalThroughputRow,
  CycleTimeRow,
  DashboardChartsDto,
  DeadlineComplianceRow,
  FunnelReport,
  PivotQuery,
  PivotResult,
  SlPerformanceRow,
  WinLossRow
} from '../models/contract';
import { ApiService } from './api.service';

export interface ReportRangeParams {
  fromUtc?: string | null;
  toUtc?: string | null;
  groupBy?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly api = inject(ApiService);

  private range(p?: ReportRangeParams): Record<string, string | number | boolean | undefined | null> {
    return {
      fromUtc: p?.fromUtc || undefined,
      toUtc: p?.toUtc || undefined,
      groupBy: p?.groupBy || undefined
    };
  }

  funnel(p?: ReportRangeParams): Observable<FunnelReport> {
    return this.api.get<FunnelReport>('/reports/funnel', this.range(p));
  }

  winLoss(p?: ReportRangeParams): Observable<WinLossRow[]> {
    return this.api.get<WinLossRow[]>('/reports/win-loss', this.range(p));
  }

  cycleTime(p?: ReportRangeParams): Observable<CycleTimeRow[]> {
    return this.api.get<CycleTimeRow[]>('/reports/cycle-time', this.range(p));
  }

  slPerformance(p?: ReportRangeParams): Observable<SlPerformanceRow[]> {
    return this.api.get<SlPerformanceRow[]>('/reports/sl-performance', this.range(p));
  }

  deadlineCompliance(p?: ReportRangeParams): Observable<DeadlineComplianceRow[]> {
    return this.api.get<DeadlineComplianceRow[]>('/reports/deadline-compliance', this.range(p));
  }

  approvalThroughput(p?: ReportRangeParams): Observable<ApprovalThroughputRow[]> {
    return this.api.get<ApprovalThroughputRow[]>('/reports/approval-throughput', this.range(p));
  }

  pivot(body: PivotQuery): Observable<PivotResult> {
    return this.api.post<PivotResult>('/reports/pivot', body);
  }

  export(
    report:
      | 'funnel'
      | 'win-loss'
      | 'cycle-time'
      | 'sl-performance'
      | 'deadline-compliance'
      | 'approval-throughput',
    p?: ReportRangeParams
  ): Observable<Blob> {
    return this.api.getBlob(`/reports/${report}/export`, this.range(p));
  }

  exportPivot(body: PivotQuery): Observable<Blob> {
    return this.api.postBlob('/reports/pivot/export', body);
  }
}
