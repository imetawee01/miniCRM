import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { GateInstance } from '../../../core/models/gate';
import { GateService } from '../../../core/services/gate.service';
import { ToastService } from '../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../material';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';
import { pickLocalized } from '../../../core/utils/locale';
import { TranslateService } from '@ngx-translate/core';

const DECISIONS_BY_GATE_CODE: Record<string, string[]> = {
  GW1_REVIEW: ['Approve', 'Reject'],
  QUAL_DECISION: ['Qualified', 'NotQualified'],
  QUAL_MEETING: ['Passed', 'NotPassed'],
  PROPOSAL_REVIEW: ['Approve', 'Return'],
  MGMT_APPROVAL: ['Approve', 'Reject'],
  CONTRACT_SIGNOFF: ['Approve', 'Reject']
};

export interface GateDecideDialogData {
  gate: GateInstance;
}

export interface GateDecideDialogResult {
  submitted: true;
  decision: string;
  reason?: string | null;
}

@Component({
  selector: 'crm-gate-decide-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ gateName }}</h2>
    <mat-dialog-content>
      <p class="meta">
        <strong>{{ data.gate.opportunityNumber || data.gate.opportunity?.opportunityNumber }}</strong>
        · {{ data.gate.opportunityName || data.gate.opportunity?.name }}
      </p>
      <p class="meta muted">{{ data.gate.assignedRoleCode | enumLabel: 'roles' }} · {{ 'gates.round' | translate }} {{ data.gate.round }}</p>

      <form [formGroup]="form" class="crm-stack">
        <mat-radio-group formControlName="decision" class="decisions" [attr.aria-label]="'gates.decision' | translate">
          @for (d of decisions; track d) {
            <mat-radio-button [value]="d">{{ ('gates.decisions.' + d) | translate }}</mat-radio-button>
          }
        </mat-radio-group>
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'gates.reason' | translate }}</mat-label>
          <textarea matInput rows="3" formControlName="reason"></textarea>
          @if (form.controls.reason.invalid && form.controls.reason.touched) {
            <mat-error>{{ 'common.reasonRequired' | translate }}</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="busy()" (click)="ref.close(null)">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid || busy()" (click)="submit()">
        {{ 'gates.submitDecision' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .meta { margin: 0 0 0.35rem; }
    .muted { color: var(--crm-muted); font-size: 0.85rem; }
    .decisions { display: flex; flex-direction: column; gap: 0.35rem; margin: 0.75rem 0; }
    .full { width: 100%; }
  `
})
export class GateDecideDialogComponent {
  readonly data = inject<GateDecideDialogData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<GateDecideDialogComponent, GateDecideDialogResult | null>);
  private readonly gates = inject(GateService);
  private readonly toast = inject(ToastService);
  private readonly i18n = inject(TranslateService);
  readonly busy = signal(false);

  readonly decisions: string[] = (() => {
    const fromGate = this.data.gate.gate?.allowedDecisions || this.data.gate.allowedDecisions;
    if (fromGate) return fromGate.split(',').map((s) => s.trim()).filter(Boolean);
    const code = (this.data.gate.gateCode || this.data.gate.gate?.code || '').toUpperCase();
    return DECISIONS_BY_GATE_CODE[code] ?? ['Approve', 'Reject'];
  })();

  readonly gateName = pickLocalized(
    this.i18n.getCurrentLang(),
    this.data.gate.gate?.nameEn || this.data.gate.gateNameEn,
    this.data.gate.gate?.nameAr || this.data.gate.gateNameAr,
    this.data.gate.gateCode || ''
  );

  readonly form = new FormGroup({
    decision: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    reason: new FormControl('', { nonNullable: true })
  });

  constructor() {
    this.form.controls.decision.valueChanges.subscribe((decision) => {
      const rejectLike = ['Reject', 'Return', 'NotQualified', 'NotPassed'].includes(decision);
      const reason = this.form.controls.reason;
      if (rejectLike) reason.setValidators([Validators.required]);
      else reason.clearValidators();
      reason.updateValueAndValidity();
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { decision, reason } = this.form.getRawValue();
    this.busy.set(true);
    this.gates.decide(this.data.gate.id, { decision, reason: reason || null }).subscribe({
      next: () => {
        this.toast.success('gates.decided');
        this.busy.set(false);
        this.ref.close({ submitted: true, decision, reason });
      },
      error: () => this.busy.set(false)
    });
  }
}
