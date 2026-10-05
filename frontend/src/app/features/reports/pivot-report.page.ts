import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { PivotResult } from '../../core/models/contract';
import { ReportsService } from '../../core/services/reports.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { triggerDownload } from '../../core/utils/browser';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'crm-pivot-report-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="reports.pivot">
        <button mat-stroked-button type="button" (click)="export()" [disabled]="!result()">{{ 'common.export' | translate }}</button>
      </crm-page-header>

      <form class="crm-card filters" [formGroup]="form" (ngSubmit)="run()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'reports.rows' | translate }}</mat-label>
          <mat-select formControlName="rows">
            @for (d of dims; track d.value) {
              <mat-option [value]="d.value">{{ d.labelKey | translate }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'reports.columns' | translate }}</mat-label>
          <mat-select formControlName="columns">
            <mat-option value="">{{ 'reports.none' | translate }}</mat-option>
            @for (d of dims; track d.value) {
              <mat-option [value]="d.value">{{ d.labelKey | translate }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'reports.measure' | translate }}</mat-label>
          <mat-select formControlName="measure">
            <mat-option value="count">{{ 'reports.measureCount' | translate }}</mat-option>
            <mat-option value="expectedValue">{{ 'reports.measureValue' | translate }}</mat-option>
            <mat-option value="awardedValue">{{ 'reports.measureAwarded' | translate }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'reports.from' | translate }}</mat-label>
          <input matInput type="date" formControlName="from" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'reports.to' | translate }}</mat-label>
          <input matInput type="date" formControlName="to" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit">{{ 'reports.run' | translate }}</button>
      </form>

      @if (result(); as p) {
        <div class="crm-card table-wrap">
          <table class="crm-table">
            <thead>
              <tr>
                <th>{{ 'reports.group' | translate }}</th>
                @for (label of p.columnLabels; track $index) {
                  <th>{{ label }}</th>
                }
                <th>{{ 'reports.total' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (row of p.rowsData; track row.rowKey) {
                <tr>
                  <td>{{ row.rowLabel }}</td>
                  @for (key of p.columnKeys; track key) {
                    <td>{{ cell(row, key) | number: '1.0-2' }}</td>
                  }
                  <td><strong>{{ row.total | number: '1.0-2' }}</strong></td>
                </tr>
              }
            </tbody>
            <tfoot>
              <tr>
                <td><strong>{{ 'reports.total' | translate }}</strong></td>
                @for (_ of p.columnKeys; track $index) {
                  <td></td>
                }
                <td><strong>{{ p.grandTotal | number: '1.0-2' }}</strong></td>
              </tr>
            </tfoot>
          </table>
        </div>
      }
    </div>
  `,
  styles: `
    .filters { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; }
    .table-wrap { overflow: auto; }
    .crm-table { width: 100%; border-collapse: collapse; min-width: 480px; }
    .crm-table th, .crm-table td { text-align: start; padding: 0.65rem 0.75rem; border-bottom: 1px solid color-mix(in srgb, var(--crm-border, #d0d7de) 80%, transparent); white-space: nowrap; }
    .crm-table th { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--crm-muted); }
    tfoot td { border-top: 2px solid var(--crm-border, #d0d7de); }
  `
})
export class PivotReportPageComponent implements OnInit {
  private readonly api = inject(ReportsService);
  readonly result = signal<PivotResult | null>(null);
  readonly dims = [
    { value: 'stage', labelKey: 'opp.stage' },
    { value: 'status', labelKey: 'opp.status' },
    { value: 'customer', labelKey: 'opp.customer' },
    { value: 'theme', labelKey: 'opp.submissionTheme' },
    { value: 'source', labelKey: 'opp.sourceChannel' },
    { value: 'owner', labelKey: 'reports.dimOwner' },
    { value: 'builder', labelKey: 'reports.dimBuilder' },
    { value: 'serviceline', labelKey: 'admin.serviceLines' },
    { value: 'monthCreated', labelKey: 'reports.monthCreated' },
    { value: 'monthSubmitted', labelKey: 'reports.monthSubmitted' }
  ];
  readonly form = new FormGroup({
    rows: new FormControl('stage', { nonNullable: true }),
    columns: new FormControl(''),
    measure: new FormControl('count', { nonNullable: true }),
    from: new FormControl(''),
    to: new FormControl('')
  });

  ngOnInit(): void {
    this.run();
  }

  run(): void {
    this.api.pivot(this.body()).subscribe((r) => this.result.set(r));
  }

  export(): void {
    this.api.exportPivot(this.body()).subscribe((b) => triggerDownload(b, 'pivot.xlsx'));
  }

  cell(row: PivotResult['rowsData'][number], key: string): number {
    return row.cells.find((c) => c.columnKey === key)?.value ?? 0;
  }

  private body() {
    const v = this.form.getRawValue();
    return {
      rows: v.rows,
      columns: v.columns || null,
      measure: v.measure,
      fromUtc: v.from ? new Date(v.from).toISOString() : null,
      toUtc: v.to ? new Date(`${v.to}T23:59:59`).toISOString() : null
    };
  }
}
