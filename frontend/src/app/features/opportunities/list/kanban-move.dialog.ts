import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateService } from '@ngx-translate/core';
import { StatusTransitionDto } from '../../../core/models/opportunity';
import { MATERIAL_IMPORTS } from '../../../shared/material';

export interface KanbanMoveDialogData {
  opportunityNumber: string;
  opportunityName: string;
  targetStageName: string;
  /** Candidate statuses in the target stage (already filtered as legal for the user). */
  options: StatusTransitionDto[];
}

export interface KanbanMoveDialogResult {
  toStatusId: string;
  reason?: string;
}

@Component({
  selector: 'crm-kanban-move-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'opp.kanbanMoveTitle' | translate }}</h2>
    <mat-dialog-content>
      <p class="hint">
        {{
          'opp.kanbanMoveHint'
            | translate
              : {
                  number: data.opportunityNumber,
                  name: data.opportunityName,
                  stage: data.targetStageName
                }
        }}
      </p>

      @if (data.options.length > 1) {
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'opp.status' | translate }}</mat-label>
          <mat-select [formControl]="form.controls.toStatusId" required>
            @for (opt of data.options; track opt.toStatusId) {
              <mat-option [value]="opt.toStatusId">{{ statusLabel(opt) }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      } @else if (data.options.length === 1) {
        <p class="target">
          <strong>{{ 'opp.status' | translate }}:</strong>
          {{ statusLabel(data.options[0]) }}
        </p>
      }

      @if (needsReason()) {
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'common.reason' | translate }}</mat-label>
          <textarea matInput rows="3" [formControl]="form.controls.reason" required></textarea>
          @if (form.controls.reason.invalid && form.controls.reason.touched) {
            <mat-error>{{ 'common.reasonRequired' | translate }}</mat-error>
          }
        </mat-form-field>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="ref.close(null)">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid" (click)="submit()">
        {{ 'opp.kanbanMoveConfirm' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .hint,
    .target {
      margin: 0 0 0.85rem;
      color: var(--crm-muted, #6b7280);
      font-size: 0.9rem;
    }
    .full {
      width: 100%;
      display: block;
    }
  `
})
export class KanbanMoveDialogComponent {
  readonly data = inject<KanbanMoveDialogData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<KanbanMoveDialogComponent, KanbanMoveDialogResult | null>);
  private readonly i18n = inject(TranslateService);

  readonly form = new FormGroup({
    toStatusId: new FormControl(this.data.options[0]?.toStatusId ?? '', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    reason: new FormControl('', { nonNullable: true })
  });

  constructor() {
    this.form.controls.toStatusId.valueChanges.subscribe(() => this.syncReasonValidators());
    this.syncReasonValidators();
  }

  needsReason(): boolean {
    const id = this.form.controls.toStatusId.value;
    return !!this.data.options.find((o) => o.toStatusId === id)?.requiresReason;
  }

  statusLabel(opt: StatusTransitionDto): string {
    return this.i18n.getCurrentLang() === 'ar' ? opt.toStatusNameAr || opt.toStatusNameEn : opt.toStatusNameEn;
  }

  submit(): void {
    this.syncReasonValidators();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.ref.close({
      toStatusId: v.toStatusId,
      reason: this.needsReason() ? v.reason.trim() : undefined
    });
  }

  private syncReasonValidators(): void {
    const ctrl = this.form.controls.reason;
    if (this.needsReason()) {
      ctrl.setValidators([Validators.required, Validators.minLength(3)]);
    } else {
      ctrl.clearValidators();
      ctrl.setValue('');
    }
    ctrl.updateValueAndValidity({ emitEvent: false });
  }
}
