import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EstimatedCost, SlResponse } from '../../core/models/contract';
import { OpportunityDeadlines } from '../../core/models/opportunity';
import { ProposalService } from '../../core/services/proposal.service';
import { OpportunityService } from '../../core/services/opportunity.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AttachmentListComponent } from '../../shared/components/attachment-list/attachment-list.component';
import { NotesPanelComponent } from '../../shared/components/notes-panel/notes-panel.component';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';
import { deadlineOrder } from '../../core/utils/validators';

@Component({
  selector: 'crm-builder-workspace-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, AttachmentListComponent, NotesPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="proposal.workspace" />
      <form class="crm-card crm-grid crm-grid-2" [formGroup]="deadlines" (ngSubmit)="saveDeadlines()">
        <h3 class="crm-section-title">{{ 'deadlines.title' | translate }}</h3>
        @for (f of deadlineFields; track f) {
          <mat-form-field appearance="outline">
            <mat-label>{{ ('deadlines.' + f) | translate }}</mat-label>
            <input #deadlineInput matInput type="datetime-local" [formControlName]="f" />
            <button mat-icon-button matSuffix type="button" (click)="openDateTimePicker(deadlineInput)">
              <mat-icon>calendar_today</mat-icon>
            </button>
          </mat-form-field>
        }
        <button mat-stroked-button type="submit">{{ 'common.save' | translate }}</button>
      </form>
      <form class="crm-card" [formGroup]="costForm" (ngSubmit)="submitCost()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'proposal.estimatedCost' | translate }}</mat-label>
          <input matInput type="number" formControlName="amount" />
        </mat-form-field>
        <button mat-flat-button type="submit">{{ 'proposal.submitCost' | translate }}</button>
      </form>
      <button mat-flat-button color="primary" type="button" (click)="ready()">{{ 'proposal.markReady' | translate }}</button>
      <crm-attachment-list entityType="opportunities" [entityId]="id()" />
      <crm-notes-panel entityType="opportunities" [entityId]="id()" />
    </div>
  `
})
export class BuilderWorkspacePageComponent implements OnInit, CanComponentDeactivate {
  private readonly proposals = inject(ProposalService);
  private readonly opps = inject(OpportunityService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly cost = signal<EstimatedCost | null>(null);
  readonly sl = signal<SlResponse[]>([]);
  readonly deadlineFields = [
    'qualificationDeadline',
    'inquiriesDeadline',
    'estimatedCostDeadline',
    'internalDeadline',
    'submissionDeadline'
  ] as const;
  readonly deadlines = new FormGroup(
    {
      qualificationDeadline: new FormControl('', { nonNullable: true }),
      inquiriesDeadline: new FormControl('', { nonNullable: true }),
      estimatedCostDeadline: new FormControl('', { nonNullable: true }),
      internalDeadline: new FormControl('', { nonNullable: true }),
      submissionDeadline: new FormControl('', { nonNullable: true })
    },
    { validators: deadlineOrder() }
  );
  readonly costForm = new FormGroup({
    amount: new FormControl<number | null>(null, Validators.required)
  });

  canDeactivate(): boolean {
    return !this.deadlines.dirty && !this.costForm.dirty;
  }

  id(): string {
    return this.route.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    this.proposals.estimatedCost(this.id()).subscribe({ next: (c) => this.cost.set(c), error: () => null });
    this.proposals.slResponses(this.id()).subscribe({ next: (r) => this.sl.set(r), error: () => null });
    this.opps.get(this.id()).subscribe((o) => {
      const d = o.deadlines;
      this.patchDeadline(d);
    });
  }

  private patchDeadline(d: OpportunityDeadlines): void {
    this.deadlines.patchValue({
      qualificationDeadline: d.qualificationDeadline?.slice(0, 16) ?? '',
      inquiriesDeadline: d.inquiriesDeadline?.slice(0, 16) ?? '',
      estimatedCostDeadline: d.estimatedCostDeadline?.slice(0, 16) ?? '',
      internalDeadline: d.internalDeadline?.slice(0, 16) ?? '',
      submissionDeadline: d.submissionDeadline?.slice(0, 16) ?? ''
    });
  }

  openDateTimePicker(input: HTMLInputElement): void {
    input.focus();

    if (typeof input.showPicker === 'function') {
      input.showPicker();
    }
  }

  saveDeadlines(): void {
    const raw = this.deadlines.getRawValue();
    const body: Record<string, string | null> = {};
    for (const [k, v] of Object.entries(raw)) {
      body[k] = v ? new Date(v).toISOString() : null;
    }
    this.opps.setDeadlines(this.id(), body).subscribe(() => {
      this.deadlines.markAsPristine();
      this.toast.success('deadlines.saved');
    });
  }

  submitCost(): void {
    const amount = this.costForm.controls.amount.value;
    if (amount == null) return;
    this.proposals.submitEstimatedCost(this.id(), amount).subscribe(() => {
      this.costForm.markAsPristine();
      this.toast.success('proposal.costSubmitted');
    });
  }

  ready(): void {
    this.proposals.markReady(this.id()).subscribe(() => this.toast.success('proposal.ready'));
  }
}
