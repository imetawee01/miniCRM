import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { SUBMISSION_CHANNELS } from '../../core/models/enums';
import { SubmissionRecord } from '../../core/models/contract';
import { SubmissionService } from '../../core/services/submission.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EnumLabelPipe } from '../../shared/pipes/enum-label.pipe';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-submission-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="submission.title" />
      <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'submission.channel' | translate }}</mat-label>
          <mat-select formControlName="channel">
            @for (c of channels; track c) {
              <mat-option [value]="c">{{ c | enumLabel }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'submission.reference' | translate }}</mat-label>
          <input matInput formControlName="reference" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'submission.submittedAt' | translate }}</mat-label>
          <input matInput type="datetime-local" formControlName="submittedAtUtc" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">{{ 'submission.submit' | translate }}</button>
      </form>
    </div>
  `
})
export class SubmissionPageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(SubmissionService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly existing = signal<SubmissionRecord | null>(null);
  readonly channels = SUBMISSION_CHANNELS;
  readonly form = new FormGroup({
    channel: new FormControl<(typeof SUBMISSION_CHANNELS)[number]>('Etimad', { nonNullable: true }),
    reference: new FormControl('', { nonNullable: true }),
    submittedAtUtc: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.api.get(id).subscribe({ next: (s) => this.existing.set(s), error: () => null });
    }
  }

  submit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || this.form.invalid) return;
    const v = this.form.getRawValue();
    this.api
      .submit(id, { channel: v.channel, reference: v.reference, submittedAtUtc: new Date(v.submittedAtUtc).toISOString() })
      .subscribe(() => {
        this.form.markAsPristine();
        this.toast.success('submission.saved');
      });
  }
}
