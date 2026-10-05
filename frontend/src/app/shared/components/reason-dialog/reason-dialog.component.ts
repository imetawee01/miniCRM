import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MATERIAL_IMPORTS } from '../../material';

export interface ReasonDialogData {
  titleKey: string;
  messageKey?: string;
  confirmKey?: string;
  cancelKey?: string;
  reasonLabelKey?: string;
  warn?: boolean;
  minLength?: number;
}

@Component({
  selector: 'crm-reason-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title id="crm-reason-title">{{ data.titleKey | translate }}</h2>
    <mat-dialog-content>
      @if (data.messageKey) {
        <p>{{ data.messageKey | translate }}</p>
      }
      <mat-form-field appearance="outline" class="full">
        <mat-label>{{ (data.reasonLabelKey || 'common.reason') | translate }}</mat-label>
        <textarea matInput rows="3" [formControl]="reason" required></textarea>
        @if (reason.invalid && reason.touched) {
          <mat-error>{{ 'common.reasonRequired' | translate }}</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="ref.close(null)">
        {{ (data.cancelKey || 'common.cancel') | translate }}
      </button>
      <button
        mat-flat-button
        type="button"
        [color]="data.warn ? 'warn' : 'primary'"
        [disabled]="reason.invalid"
        (click)="submit()"
      >
        {{ (data.confirmKey || 'common.confirm') | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .full { width: 100%; display: block; margin-top: 0.5rem; }
    p { margin: 0 0 0.75rem; color: var(--crm-muted); }
  `
})
export class ReasonDialogComponent {
  readonly data = inject<ReasonDialogData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<ReasonDialogComponent, string | null>);
  readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(this.data.minLength ?? 3)]
  });

  submit(): void {
    if (this.reason.invalid) {
      this.reason.markAsTouched();
      return;
    }
    this.ref.close(this.reason.value.trim());
  }
}
