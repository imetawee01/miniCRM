import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { QualificationMeeting } from '../../core/models/contract';
import { GateInstance } from '../../core/models/gate';
import { QualificationService } from '../../core/services/qualification.service';
import { GateService } from '../../core/services/gate.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ApprovalPanelComponent } from '../../shared/components/approval-panel/approval-panel.component';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-meeting-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, ApprovalPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="qual.meeting" />
      <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'qual.scheduledAt' | translate }}</mat-label>
          <input matInput type="datetime-local" formControlName="scheduledAtUtc" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'qual.location' | translate }}</mat-label>
          <input matInput formControlName="location" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'qual.agenda' | translate }}</mat-label>
          <textarea matInput rows="3" formControlName="agenda"></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'qual.minutes' | translate }}</mat-label>
          <textarea matInput rows="4" formControlName="minutesOfMeeting"></textarea>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit">{{ 'common.save' | translate }}</button>
      </form>
      @if (gate(); as g) {
        <crm-approval-panel [gate]="g" />
      }
    </div>
  `
})
export class MeetingPageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(QualificationService);
  private readonly gates = inject(GateService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly meeting = signal<QualificationMeeting | null>(null);
  readonly gate = signal<GateInstance | null>(null);
  readonly form = new FormGroup({
    scheduledAtUtc: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    location: new FormControl('', { nonNullable: true }),
    agenda: new FormControl('', { nonNullable: true }),
    minutesOfMeeting: new FormControl('', { nonNullable: true })
  });

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  private id(): string {
    return this.route.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    this.api.getMeeting(this.id()).subscribe({
      next: (m) => {
        this.meeting.set(m);
        this.form.patchValue({
          scheduledAtUtc: m.scheduledAtUtc?.slice(0, 16) ?? '',
          location: m.location ?? '',
          agenda: m.agenda ?? '',
          minutesOfMeeting: m.minutesOfMeeting ?? ''
        });
      },
      error: () => null
    });
    this.gates.forOpportunity(this.id()).subscribe((rows) => {
      this.gate.set(rows.find((g) => (g.gateCode || g.gate?.code) === 'QUAL_MEETING' && g.state === 'Pending') ?? null);
    });
  }

  save(): void {
    const v = this.form.getRawValue();
    const body = {
      scheduledAtUtc: new Date(v.scheduledAtUtc).toISOString(),
      location: v.location,
      agenda: v.agenda
    };
    const req$ = this.meeting()
      ? this.api.updateMeeting(this.id(), body)
      : this.api.createMeeting(this.id(), body);
    req$.subscribe(() => {
      if (v.minutesOfMeeting) {
        this.api.saveMinutes(this.id(), v.minutesOfMeeting).subscribe();
      }
      this.form.markAsPristine();
      this.toast.success('qual.meetingSaved');
    });
  }
}
