import { ChangeDetectionStrategy, Component, OnInit, inject, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import {
  ApprovalThroughputRow,
  CycleTimeRow,
  DeadlineComplianceRow,
  FunnelReport,
  SlPerformanceRow,
  WinLossRow
} from '../../core/models/contract';
import { ReportRangeParams, ReportsService } from '../../core/services/reports.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { triggerDownload } from '../../core/utils/browser';

@Component({
  selector: 'crm-report-range-bar',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="range" [formGroup]="form" (ngSubmit)="applied.emit(params())">
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>{{ 'reports.from' | translate }}</mat-label>
        <input matInput type="date" formControlName="from" />
      </mat-form-field>
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>{{ 'reports.to' | translate }}</mat-label>
        <input matInput type="date" formControlName="to" />
      </mat-form-field>
      <ng-content />
      <button mat-stroked-button type="submit">{{ 'common.apply' | translate }}</button>
    </form>
  `,
  styles: `.range { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; margin-bottom: 1rem; }`
})
export class ReportRangeBarComponent {
  readonly form = new FormGroup({
    from: new FormControl(''),
    to: new FormControl('')
  });
  readonly applied = output<ReportRangeParams>();

  params(): ReportRangeParams {
    const from = this.form.value.from;
    const to = this.form.value.to;
    return {
      fromUtc: from ? new Date(from).toISOString() : undefined,
      toUtc: to ? new Date(`${to}T23:59:59`).toISOString() : undefined
    };
  }
}

const tableStyles = `
  .crm-table { width: 100%; border-collapse: collapse; }
  .crm-table th, .crm-table td { text-align: start; padding: 0.65rem 0.75rem; border-bottom: 1px solid color-mix(in srgb, var(--crm-border, #d0d7de) 80%, transparent); }
  .crm-table th { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--crm-muted); }
`;

@Component({
  selector: 'crm-funnel-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.funnel">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="load($event)" />
      @if (data(); as d) {
        <div class="crm-grid crm-grid-4">
          <article class="crm-card crm-kpi"><span>{{ 'reports.created' | translate }}</span><strong>{{ d.created }}</strong></article>
          <article class="crm-card crm-kpi"><span>{{ 'reports.gw1' | translate }}</span><strong>{{ d.gw1Passed ?? 0 }}</strong></article>
          <article class="crm-card crm-kpi"><span>{{ 'reports.qualified' | translate }}</span><strong>{{ d.qualified }}</strong></article>
          <article class="crm-card crm-kpi"><span>{{ 'reports.submitted' | translate }}</span><strong>{{ d.submitted }}</strong></article>
          <article class="crm-card crm-kpi"><span>{{ 'reports.won' | translate }}</span><strong>{{ d.won }}</strong></article>
          <article class="crm-card crm-kpi"><span>{{ 'outcome.lost' | translate }}</span><strong>{{ d.lost ?? 0 }}</strong></article>
        </div>
        <ul class="funnel">
          @for (step of funnelSteps(d); track step.label) {
            <li>
              <span>{{ step.label | translate }}</span>
              <div class="crm-bar"><div class="crm-bar__fill" [style.width.%]="step.pct"></div></div>
              <strong>{{ step.value }}</strong>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: `
    .funnel { list-style: none; margin: 1.5rem 0 0; padding: 0; display: flex; flex-direction: column; gap: 0.75rem; }
    .funnel li { display: grid; grid-template-columns: 8rem 1fr 3rem; gap: 0.75rem; align-items: center; }
  `
})
export class FunnelReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly data = signal<FunnelReport | null>(null);
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.load({}); }
  load(r: ReportRangeParams): void {
    this.range = r;
    this.api.funnel(r).subscribe((d) => this.data.set(d));
  }
  export(): void {
    this.api.export('funnel', this.range).subscribe((b) => triggerDownload(b, 'funnel.xlsx'));
  }
  funnelSteps(d: FunnelReport): { label: string; value: number; pct: number }[] {
    const steps = [
      { label: 'reports.created', value: d.created },
      { label: 'reports.gw1', value: d.gw1Passed ?? 0 },
      { label: 'reports.qualified', value: d.qualified },
      { label: 'reports.submitted', value: d.submitted },
      { label: 'reports.won', value: d.won }
    ];
    const max = Math.max(...steps.map((s) => s.value), 1);
    return steps.map((s) => ({ ...s, pct: (s.value / max) * 100 }));
  }
}

@Component({
  selector: 'crm-win-loss-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.winLoss">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="onRange($event)">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>{{ 'reports.groupBy' | translate }}</mat-label>
          <mat-select [formControl]="groupBy" (selectionChange)="reload()">
            <mat-option value="theme">{{ 'opp.submissionTheme' | translate }}</mat-option>
            <mat-option value="source">{{ 'opp.sourceChannel' | translate }}</mat-option>
            <mat-option value="customer">{{ 'opp.customer' | translate }}</mat-option>
            <mat-option value="owner">Owner</mat-option>
            <mat-option value="serviceline">{{ 'admin.serviceLines' | translate }}</mat-option>
          </mat-select>
        </mat-form-field>
      </crm-report-range-bar>
      <table class="crm-table">
        <thead>
          <tr>
            <th>{{ 'reports.group' | translate }}</th>
            <th>{{ 'outcome.won' | translate }}</th>
            <th>{{ 'outcome.lost' | translate }}</th>
            <th>{{ 'dashboard.winRate' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.groupKey) {
            <tr>
              <td>{{ r.groupLabel }}</td>
              <td>{{ r.won }}</td>
              <td>{{ r.lost }}</td>
              <td>{{ r.winRate }}%</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [tableStyles]
})
export class WinLossReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly rows = signal<WinLossRow[]>([]);
  readonly groupBy = new FormControl('theme', { nonNullable: true });
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.reload(); }
  onRange(r: ReportRangeParams): void { this.range = r; this.reload(); }
  reload(): void {
    this.api.winLoss({ ...this.range, groupBy: this.groupBy.value }).subscribe((r) => this.rows.set(r));
  }
  export(): void {
    this.api.export('win-loss', { ...this.range, groupBy: this.groupBy.value }).subscribe((b) => triggerDownload(b, 'win-loss.xlsx'));
  }
}

@Component({
  selector: 'crm-cycle-time-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.cycleTime">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="load($event)" />
      <table class="crm-table">
        <thead>
          <tr>
            <th>{{ 'reports.group' | translate }}</th>
            <th>{{ 'reports.avgDays' | translate }}</th>
            <th>{{ 'reports.medianDays' | translate }}</th>
            <th>{{ 'reports.samples' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.stageOrGate + (r.kind ?? '')) {
            <tr>
              <td>{{ r.nameEn }} <small>({{ r.kind }})</small></td>
              <td>{{ r.avgDays }}</td>
              <td>{{ r.medianDays ?? '—' }}</td>
              <td>{{ r.count ?? 0 }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [tableStyles]
})
export class CycleTimeReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly rows = signal<CycleTimeRow[]>([]);
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.load({}); }
  load(r: ReportRangeParams): void {
    this.range = r;
    this.api.cycleTime(r).subscribe((rows) => this.rows.set(rows));
  }
  export(): void {
    this.api.export('cycle-time', this.range).subscribe((b) => triggerDownload(b, 'cycle-time.xlsx'));
  }
}

@Component({
  selector: 'crm-sl-performance-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.slPerformance">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="load($event)" />
      <table class="crm-table">
        <thead>
          <tr>
            <th>{{ 'admin.serviceLines' | translate }}</th>
            <th>{{ 'reports.avgDays' | translate }}</th>
            <th>{{ 'reports.onTime' | translate }}</th>
            <th>{{ 'reports.submitted' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.serviceLineId) {
            <tr>
              <td>{{ r.nameEn }}</td>
              <td>{{ r.avgTurnaroundDays }}</td>
              <td>{{ r.onTimePercent ?? 0 }}%</td>
              <td>{{ r.submittedCount }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [tableStyles]
})
export class SlPerformanceReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly rows = signal<SlPerformanceRow[]>([]);
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.load({}); }
  load(r: ReportRangeParams): void {
    this.range = r;
    this.api.slPerformance(r).subscribe((rows) => this.rows.set(rows));
  }
  export(): void {
    this.api.export('sl-performance', this.range).subscribe((b) => triggerDownload(b, 'sl-performance.xlsx'));
  }
}

@Component({
  selector: 'crm-deadline-compliance-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.deadlineCompliance">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="load($event)" />
      <table class="crm-table">
        <thead>
          <tr>
            <th>{{ 'reports.month' | translate }}</th>
            <th>{{ 'reports.met' | translate }}</th>
            <th>{{ 'reports.missed' | translate }}</th>
            <th>{{ 'reports.compliance' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.month) {
            <tr>
              <td>{{ r.month }}</td>
              <td>{{ r.met }}</td>
              <td>{{ r.missed }}</td>
              <td>{{ r.compliancePercent }}%</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [tableStyles]
})
export class DeadlineComplianceReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly rows = signal<DeadlineComplianceRow[]>([]);
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.load({}); }
  load(r: ReportRangeParams): void {
    this.range = r;
    this.api.deadlineCompliance(r).subscribe((rows) => this.rows.set(rows));
  }
  export(): void {
    this.api.export('deadline-compliance', this.range).subscribe((b) => triggerDownload(b, 'deadline-compliance.xlsx'));
  }
}

@Component({
  selector: 'crm-approval-throughput-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ReportRangeBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.approvalThroughput">
        <button mat-stroked-button type="button" (click)="export()">{{ 'common.export' | translate }}</button>
      </crm-page-header>
      <crm-report-range-bar (applied)="load($event)" />
      <table class="crm-table">
        <thead>
          <tr>
            <th>{{ 'reports.gate' | translate }}</th>
            <th>{{ 'reports.pending' | translate }}</th>
            <th>{{ 'reports.decided' | translate }}</th>
            <th>{{ 'reports.avgDays' | translate }}</th>
            <th>{{ 'reports.maxRounds' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.gateCode) {
            <tr>
              <td>{{ r.nameEn }}</td>
              <td>{{ r.pending }}</td>
              <td>{{ r.decided }}</td>
              <td>{{ r.avgDecisionDays }}</td>
              <td>{{ r.maxRounds }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [tableStyles]
})
export class ApprovalThroughputReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly rows = signal<ApprovalThroughputRow[]>([]);
  private range: ReportRangeParams = {};
  ngOnInit(): void { this.load({}); }
  load(r: ReportRangeParams): void {
    this.range = r;
    this.api.approvalThroughput(r).subscribe((rows) => this.rows.set(rows));
  }
  export(): void {
    this.api.export('approval-throughput', this.range).subscribe((b) => triggerDownload(b, 'approval-throughput.xlsx'));
  }
}
