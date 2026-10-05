import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardChartsDto, KpiDto, MyWorkDto, PipelineDto } from '../../core/models/contract';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SarPipe } from '../../shared/pipes/sar.pipe';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'crm-dashboard-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, ReactiveFormsModule, PageHeaderComponent, SarPipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="dashboard.title" subtitleKey="dashboard.subtitle">
        <form class="range" [formGroup]="rangeForm" (ngSubmit)="reload()">
          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>{{ 'reports.from' | translate }}</mat-label>
            <input matInput type="date" formControlName="from" />
          </mat-form-field>
          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>{{ 'reports.to' | translate }}</mat-label>
            <input matInput type="date" formControlName="to" />
          </mat-form-field>
          <button mat-stroked-button type="submit">{{ 'common.apply' | translate }}</button>
        </form>
      </crm-page-header>

      @if (kpis(); as k) {
        <div class="crm-grid crm-grid-4">
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip" aria-hidden="true"><mat-icon>work_outline</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.openCount' | translate }}</span>
              <span class="crm-kpi__value">{{ k.openCount }}</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip crm-icon-chip--green" aria-hidden="true"><mat-icon>payments</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.openValue' | translate }}</span>
              <span class="crm-kpi__value">{{ k.openValueSar | sar }}</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip crm-icon-chip--purple" aria-hidden="true"><mat-icon>trending_up</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.winRate' | translate }}</span>
              <span class="crm-kpi__value">{{ k.winRate | number: '1.0-1' }}%</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip" [class.crm-icon-chip--danger]="k.overdueDeadlines > 0" aria-hidden="true">
              <mat-icon>{{ k.overdueDeadlines > 0 ? 'warning_amber' : 'schedule' }}</mat-icon>
            </span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.overdue' | translate }}</span>
              <span class="crm-kpi__value" [class.value--danger]="k.overdueDeadlines > 0">{{ k.overdueDeadlines }}</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip" aria-hidden="true"><mat-icon>emoji_events</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.wonYtd' | translate }}</span>
              <span class="crm-kpi__value">{{ k.wonCount ?? 0 }} · {{ (k.wonValueSar ?? 0) | sar }}</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip" aria-hidden="true"><mat-icon>timelapse</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.avgCycle' | translate }}</span>
              <span class="crm-kpi__value">{{ k.avgCycleDays | number: '1.0-1' }} {{ 'reports.days' | translate }}</span>
            </span>
          </article>
          <article class="crm-card crm-card--hover crm-kpi">
            <span class="crm-icon-chip" aria-hidden="true"><mat-icon>how_to_reg</mat-icon></span>
            <span class="crm-kpi__body">
              <span class="crm-kpi__label">{{ 'dashboard.pendingApprovals' | translate }}</span>
              <span class="crm-kpi__value">{{ k.pendingApprovals ?? 0 }}</span>
            </span>
          </article>
        </div>
      }

      <div class="dash-split">
        @if (pipeline(); as p) {
          <section class="crm-card">
            <h2 class="crm-section-title">
              <mat-icon class="section-icon">bar_chart</mat-icon>
              {{ 'dashboard.pipeline' | translate }}
            </h2>
            @if (p.items.length) {
              <ul class="pipe">
                @for (item of p.items; track item.stageCode) {
                  <li class="pipe-row">
                    <span class="pipe-row__stage">{{ lang() === 'ar' ? item.stageNameAr : item.stageNameEn }}</span>
                    <div class="pipe-row__bar">
                      <div class="crm-bar"><div class="crm-bar__fill" [style.width.%]="pct(item.valueSar, pipelineMax())"></div></div>
                    </div>
                    <span class="pipe-row__value">{{ item.valueSar | sar }} <span class="pipe-row__count">{{ item.count }}</span></span>
                  </li>
                }
              </ul>
            } @else {
              <p class="dash-empty">{{ 'common.emptyMessage' | translate }}</p>
            }
          </section>
        }

        @if (work(); as w) {
          <section class="crm-card">
            <h2 class="crm-section-title">
              <mat-icon class="section-icon">assignment_turned_in</mat-icon>
              {{ 'dashboard.myWork' | translate }}
            </h2>
            <h3 class="crm-subsection-title">{{ 'dashboard.pendingGates' | translate }}</h3>
            @if (w.pendingGates.length) {
              <ul class="tasks">
                @for (g of w.pendingGates; track g.id) {
                  <li>
                    <a class="task" [routerLink]="['/approvals', g.id]">
                      <mat-icon class="task__icon">how_to_reg</mat-icon>
                      <span class="task__text">
                        <span class="task__id">{{ g.opportunityNumber }}</span>
                        <span class="task__name">{{ lang() === 'ar' ? g.gateNameAr : g.gateNameEn }}</span>
                      </span>
                      <mat-icon class="task__go">chevron_right</mat-icon>
                    </a>
                  </li>
                }
              </ul>
            } @else {
              <p class="dash-empty">{{ 'common.emptyMessage' | translate }}</p>
            }
            <h3 class="crm-subsection-title">{{ 'dashboard.builderTasks' | translate }}</h3>
            @if (w.builderTasks.length) {
              <ul class="tasks">
                @for (t of w.builderTasks; track t.opportunityId) {
                  <li>
                    <a class="task" [routerLink]="['/proposals', t.opportunityId, 'workspace']">
                      <mat-icon class="task__icon">description</mat-icon>
                      <span class="task__text">
                        <span class="task__id">{{ t.opportunityNumber }}</span>
                        <span class="task__name">{{ t.opportunityName }}</span>
                      </span>
                      <mat-icon class="task__go">chevron_right</mat-icon>
                    </a>
                  </li>
                }
              </ul>
            } @else {
              <p class="dash-empty">{{ 'common.emptyMessage' | translate }}</p>
            }
          </section>
        }
      </div>

      @if (charts(); as c) {
        <div class="dash-split">
          <section class="crm-card">
            <h2 class="crm-section-title">{{ 'dashboard.byTheme' | translate }}</h2>
            <ul class="pipe">
              @for (item of c.byTheme; track item.key) {
                <li class="pipe-row">
                  <span class="pipe-row__stage">{{ item.labelEn }}</span>
                  <div class="pipe-row__bar">
                    <div class="crm-bar"><div class="crm-bar__fill" [style.width.%]="pct(item.valueSar, themeMax())"></div></div>
                  </div>
                  <span class="pipe-row__value">{{ item.valueSar | sar }}</span>
                </li>
              }
            </ul>
          </section>
          <section class="crm-card">
            <h2 class="crm-section-title">{{ 'dashboard.aging' | translate }}</h2>
            <ul class="pipe">
              @for (item of c.aging; track item.bucket) {
                <li class="pipe-row">
                  <span class="pipe-row__stage">{{ item.bucket }} {{ 'reports.days' | translate }}</span>
                  <div class="pipe-row__bar">
                    <div class="crm-bar"><div class="crm-bar__fill crm-bar__fill--amber" [style.width.%]="pct(item.count, agingMax())"></div></div>
                  </div>
                  <span class="pipe-row__value">{{ item.count }}</span>
                </li>
              }
            </ul>
          </section>
        </div>
        <div class="dash-split">
          <section class="crm-card">
            <h2 class="crm-section-title">{{ 'dashboard.byServiceLine' | translate }}</h2>
            <ul class="pipe">
              @for (item of c.byServiceLine; track item.key) {
                <li class="pipe-row">
                  <span class="pipe-row__stage">{{ lang() === 'ar' ? item.labelAr : item.labelEn }}</span>
                  <div class="pipe-row__bar">
                    <div class="crm-bar"><div class="crm-bar__fill" [style.width.%]="pct(item.valueSar, serviceLineMax())"></div></div>
                  </div>
                  <span class="pipe-row__value">{{ item.valueSar | sar }}</span>
                </li>
              }
            </ul>
          </section>
          <section class="crm-card">
            <h2 class="crm-section-title">{{ 'dashboard.topCustomers' | translate }}</h2>
            <ul class="pipe">
              @for (item of c.topCustomers; track item.key) {
                <li class="pipe-row">
                  <span class="pipe-row__stage">{{ lang() === 'ar' ? item.labelAr : item.labelEn }}</span>
                  <div class="pipe-row__bar">
                    <div class="crm-bar"><div class="crm-bar__fill" [style.width.%]="pct(item.valueSar, customerMax())"></div></div>
                  </div>
                  <span class="pipe-row__value">{{ item.valueSar | sar }}</span>
                </li>
              }
            </ul>
          </section>
        </div>
      }
    </div>
  `,
  styles: `
    .range { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
    .value--danger { color: var(--crm-danger); }
    .section-icon { font-size: 20px; width: 20px; height: 20px; color: var(--crm-teal-dark); }
    .dash-split { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(0, 1fr); gap: 1.25rem; align-items: start; }
    .dash-empty { margin: 0; padding: 0.75rem 0; color: var(--crm-muted); font-size: 0.875rem; }
    .pipe { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.875rem; }
    .pipe-row { display: grid; grid-template-columns: minmax(7rem, 10rem) minmax(0, 1fr) auto; gap: 0.75rem; align-items: center; }
    .pipe-row__stage { font-size: 0.8125rem; font-weight: 600; color: var(--crm-navy); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .pipe-row__value { font-size: 0.8125rem; font-weight: 600; white-space: nowrap; display: flex; gap: 0.5rem; align-items: baseline; }
    .pipe-row__count { color: var(--crm-muted); font-weight: 500; }
    .crm-bar__fill--amber { background: linear-gradient(90deg, #f59e0b, #fbbf24); }
    .tasks { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.35rem; }
    .task { display: flex; align-items: center; gap: 0.65rem; padding: 0.55rem 0.65rem; border-radius: 10px; text-decoration: none; color: inherit; transition: background 0.15s ease; }
    .task:hover { background: color-mix(in srgb, var(--crm-teal) 8%, transparent); }
    .task__icon { font-size: 18px; width: 18px; height: 18px; color: var(--crm-teal-dark); }
    .task__text { flex: 1 1 auto; min-width: 0; display: flex; flex-direction: column; line-height: 1.3; }
    .task__id { font-size: 0.72rem; font-weight: 700; letter-spacing: 0.03em; color: var(--crm-muted); }
    .task__name { font-size: 0.875rem; font-weight: 500; color: var(--crm-navy); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .task__go { font-size: 18px; width: 18px; height: 18px; color: var(--crm-muted); opacity: 0; }
    .task:hover .task__go { opacity: 1; }
    :host-context([dir='rtl']) .task__go { transform: scaleX(-1); }
    @media (max-width: 1100px) { .dash-split { grid-template-columns: minmax(0, 1fr); } }
    @media (max-width: 768px) { .pipe-row { grid-template-columns: minmax(0, 1fr); gap: 0.375rem; } }
  `
})
export class DashboardPageComponent implements OnInit {
  private readonly api = inject(DashboardService);
  private readonly i18n = inject(TranslateService);
  readonly kpis = signal<KpiDto | null>(null);
  readonly work = signal<MyWorkDto | null>(null);
  readonly pipeline = signal<PipelineDto | null>(null);
  readonly charts = signal<DashboardChartsDto | null>(null);
  readonly lang = () => this.i18n.getCurrentLang() ?? 'en';
  readonly rangeForm = new FormGroup({
    from: new FormControl<string>(''),
    to: new FormControl<string>('')
  });

  ngOnInit(): void {
    this.api.myWork().subscribe((v) => this.work.set(v));
    this.api.pipeline().subscribe((v) => this.pipeline.set(v));
    this.reload();
  }

  reload(): void {
    const range = this.toRange();
    this.api.kpis(range).subscribe((v) => this.kpis.set(v));
    this.api.charts(range).subscribe((v) => this.charts.set(v));
  }

  pct(value: number, max: number): number {
    return (value / Math.max(max, 1)) * 100;
  }

  pipelineMax(): number {
    return Math.max(...(this.pipeline()?.items.map((i) => i.valueSar) ?? [0]), 1);
  }

  themeMax(): number {
    return Math.max(...(this.charts()?.byTheme.map((i) => i.valueSar) ?? [0]), 1);
  }

  agingMax(): number {
    return Math.max(...(this.charts()?.aging.map((i) => i.count) ?? [0]), 1);
  }

  serviceLineMax(): number {
    return Math.max(...(this.charts()?.byServiceLine.map((i) => i.valueSar) ?? [0]), 1);
  }

  customerMax(): number {
    return Math.max(...(this.charts()?.topCustomers.map((i) => i.valueSar) ?? [0]), 1);
  }

  private toRange(): { fromUtc?: string; toUtc?: string } {
    const from = this.rangeForm.value.from;
    const to = this.rangeForm.value.to;
    return {
      fromUtc: from ? new Date(from).toISOString() : undefined,
      toUtc: to ? new Date(`${to}T23:59:59`).toISOString() : undefined
    };
  }
}
