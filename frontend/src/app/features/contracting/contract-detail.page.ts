import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Contract, ContractMilestone, ContractNegotiationRound } from '../../core/models/contract';
import { GateInstance } from '../../core/models/gate';
import { ContractService } from '../../core/services/contract.service';
import { GateService } from '../../core/services/gate.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ApprovalPanelComponent } from '../../shared/components/approval-panel/approval-panel.component';
import { NotesPanelComponent } from '../../shared/components/notes-panel/notes-panel.component';
import { CommentsThreadComponent } from '../../shared/components/comments-thread/comments-thread.component';
import { AttachmentListComponent } from '../../shared/components/attachment-list/attachment-list.component';
import { SarPipe } from '../../shared/pipes/sar.pipe';
import { EnumLabelPipe } from '../../shared/pipes/enum-label.pipe';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-contract-detail-page',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    ReactiveFormsModule,
    PageHeaderComponent,
    ApprovalPanelComponent,
    NotesPanelComponent,
    CommentsThreadComponent,
    AttachmentListComponent,
    SarPipe,
    EnumLabelPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="contract.detail" />
      @if (contract(); as c) {
        <section class="crm-card">
          <h2>{{ c.contractNumber }}</h2>
          <p>{{ c.contractStatus | enumLabel }} · {{ c.contractValueSar | sar }}</p>
        </section>
        <form class="crm-card crm-stack" [formGroup]="roundForm" (ngSubmit)="addRound()">
          <h3>{{ 'contract.negotiation' | translate }}</h3>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'contract.requestedChanges' | translate }}</mat-label>
            <textarea matInput formControlName="requestedChanges"></textarea>
          </mat-form-field>
          <button mat-stroked-button type="submit">{{ 'contract.addRound' | translate }}</button>
        </form>
        @for (r of rounds(); track r.id) {
          <article class="crm-card">
            <strong>{{ 'gates.round' | translate }} {{ r.roundNumber }}</strong>
            <p>{{ r.requestedChanges }}</p>
            <button mat-button type="button" (click)="close(r.id, 'Agreed')">{{ 'contract.agreed' | translate }}</button>
          </article>
        }
        <form class="crm-card crm-stack" [formGroup]="msForm" (ngSubmit)="addMs()">
          <h3>{{ 'contract.milestones' | translate }}</h3>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'scope.title' | translate }}</mat-label>
            <input matInput formControlName="title" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'contract.dueDate' | translate }}</mat-label>
            <input matInput type="date" formControlName="dueDate" />
          </mat-form-field>
          <button mat-stroked-button type="submit">{{ 'common.add' | translate }}</button>
        </form>
        @for (m of milestones(); track m.id) {
          <p>{{ m.title }} · {{ m.dueDate }} · {{ m.amountSar | sar }}</p>
        }
        <form class="crm-card" [formGroup]="signForm" (ngSubmit)="sign()">
          <h3>{{ 'contract.sign' | translate }}</h3>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'contract.signatory' | translate }}</mat-label>
            <input matInput formControlName="signatory" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ 'contract.signedAt' | translate }}</mat-label>
            <input matInput type="datetime-local" formControlName="signedAtUtc" />
          </mat-form-field>
          <button mat-flat-button color="primary" type="submit">{{ 'contract.sign' | translate }}</button>
        </form>
        @if (gate(); as g) {
          <crm-approval-panel [gate]="g" />
        }
        <crm-notes-panel entityType="contracts" [entityId]="c.id" />
        <crm-comments-thread entityType="contracts" [entityId]="c.id" />
        <crm-attachment-list entityType="contracts" [entityId]="c.id" />
      }
    </div>
  `
})
export class ContractDetailPageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(ContractService);
  private readonly gates = inject(GateService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly contract = signal<Contract | null>(null);
  readonly rounds = signal<ContractNegotiationRound[]>([]);
  readonly milestones = signal<ContractMilestone[]>([]);
  readonly gate = signal<GateInstance | null>(null);
  readonly roundForm = new FormGroup({
    requestedChanges: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });
  readonly msForm = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    dueDate: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });
  readonly signForm = new FormGroup({
    signatory: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    signedAtUtc: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  canDeactivate(): boolean {
    return !this.roundForm.dirty && !this.msForm.dirty && !this.signForm.dirty;
  }

  private id(): string {
    return this.route.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    this.api.get(this.id()).subscribe((c) => {
      this.contract.set(c);
      this.gates.forOpportunity(c.opportunityId).subscribe((rows) => {
        this.gate.set(rows.find((g) => (g.gateCode || g.gate?.code) === 'CONTRACT_SIGNOFF') ?? null);
      });
    });
    this.api.rounds(this.id()).subscribe((r) => this.rounds.set(r));
    this.api.milestones(this.id()).subscribe((m) => this.milestones.set(m));
  }

  addRound(): void {
    this.api.addRound(this.id(), this.roundForm.getRawValue()).subscribe(() => {
      this.roundForm.reset();
      this.toast.success('contract.roundAdded');
      this.api.rounds(this.id()).subscribe((r) => this.rounds.set(r));
    });
  }

  close(roundId: string, status: 'Agreed' | 'Rejected'): void {
    this.api.closeRound(roundId, status).subscribe(() => this.toast.success('contract.roundClosed'));
  }

  addMs(): void {
    this.api.addMilestone(this.id(), this.msForm.getRawValue()).subscribe(() => {
      this.msForm.reset();
      this.toast.success('contract.milestoneAdded');
      this.api.milestones(this.id()).subscribe((m) => this.milestones.set(m));
    });
  }

  sign(): void {
    const v = this.signForm.getRawValue();
    this.api.sign(this.id(), { signatory: v.signatory, signedAtUtc: new Date(v.signedAtUtc).toISOString() }).subscribe(() => {
      this.signForm.markAsPristine();
      this.toast.success('contract.signed');
    });
  }
}
