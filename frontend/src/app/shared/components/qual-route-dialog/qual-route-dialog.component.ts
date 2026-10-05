import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MATERIAL_IMPORTS } from '../../material';

export interface QualRouteDialogResult {
  requiresQualificationMeeting: boolean;
}

@Component({
  selector: 'crm-qual-route-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'qual.routeTitle' | translate }}</h2>
    <mat-dialog-content>
      <p>{{ 'qual.routeMessage' | translate }}</p>
      <mat-radio-group [formControl]="route" class="opts">
        <mat-radio-button [value]="true">{{ 'qual.requiresMeeting' | translate }}</mat-radio-button>
        <mat-radio-button [value]="false">{{ 'qual.directDecision' | translate }}</mat-radio-button>
      </mat-radio-group>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="ref.close(null)">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" [disabled]="route.invalid" (click)="submit()">
        {{ 'common.confirm' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    p { margin: 0 0 0.75rem; color: var(--crm-muted); }
    .opts { display: flex; flex-direction: column; gap: 0.5rem; }
  `
})
export class QualRouteDialogComponent {
  readonly ref = inject(MatDialogRef<QualRouteDialogComponent, QualRouteDialogResult | null>);
  readonly route = new FormControl<boolean | null>(null, { validators: [Validators.required] });

  submit(): void {
    if (this.route.value === null) {
      this.route.markAsTouched();
      return;
    }
    this.ref.close({ requiresQualificationMeeting: this.route.value });
  }
}
