import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { SubmissionService } from '../../core/services/submission.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-outcome-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="outcome.title" />
      <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="save()">
        <mat-radio-group formControlName="result">
          <mat-radio-button value="Won">{{ 'outcome.won' | translate }}</mat-radio-button>
          <mat-radio-button value="Lost">{{ 'outcome.lost' | translate }}</mat-radio-button>
        </mat-radio-group>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'outcome.announcedAt' | translate }}</mat-label>
          <input matInput type="datetime-local" formControlName="announcedAtUtc" />
        </mat-form-field>
        @if (form.controls.result.value === 'Won') {
          <mat-form-field appearance="outline">
            <mat-label>{{ 'outcome.awardedValue' | translate }}</mat-label>
            <input matInput type="number" formControlName="awardedValueSar" />
          </mat-form-field>
        }
        @if (form.controls.result.value === 'Lost') {
          <mat-form-field appearance="outline">
            <mat-label>{{ 'outcome.lossReason' | translate }}</mat-label>
            <textarea matInput formControlName="lossReason"></textarea>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'outcome.competitor' | translate }}</mat-label>
            <input matInput formControlName="competitorName" />
          </mat-form-field>
        }
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">{{ 'common.save' | translate }}</button>
      </form>
    </div>
  `
})
export class OutcomePageComponent implements CanComponentDeactivate {
  private readonly api = inject(SubmissionService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly form = new FormGroup({
    result: new FormControl<'Won' | 'Lost'>('Won', { nonNullable: true }),
    announcedAtUtc: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    awardedValueSar: new FormControl<number | null>(null),
    lossReason: new FormControl('', { nonNullable: true }),
    competitorName: new FormControl('', { nonNullable: true }),
    notes: new FormControl('', { nonNullable: true })
  });

  constructor() {
    this.form.controls.result.valueChanges.subscribe((result) => {
      const ctrl = this.form.controls.lossReason;
      if (result === 'Lost') {
        ctrl.setValidators([Validators.required]);
      } else {
        ctrl.clearValidators();
      }
      ctrl.updateValueAndValidity();
    });
  }

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  save(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || this.form.invalid) return;
    const v = this.form.getRawValue();
    this.api
      .recordOutcome(id, {
        result: v.result,
        announcedAtUtc: new Date(v.announcedAtUtc).toISOString(),
        awardedValueSar: v.awardedValueSar,
        lossReason: v.lossReason || null,
        competitorName: v.competitorName || null,
        notes: v.notes || null
      })
      .subscribe(() => {
        this.form.markAsPristine();
        this.toast.success('outcome.saved');
      });
  }
}
