import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GateInstance } from '../../../core/models/gate';
import { GateService } from '../../../core/services/gate.service';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowActionService } from '../../../core/services/workflow-action.service';
import { MATERIAL_IMPORTS } from '../../material';
import { StageStatusBadgeComponent } from '../stage-status-badge/stage-status-badge.component';
import { NotesPanelComponent } from '../notes-panel/notes-panel.component';
import { CommentsThreadComponent } from '../comments-thread/comments-thread.component';
import { AttachmentListComponent } from '../attachment-list/attachment-list.component';
import { ActivityLogComponent } from '../activity-log/activity-log.component';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';
import { formatUtcTooltip } from '../../../core/utils/date-time';
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

const LEGACY_DECISION_MAP: Record<string, string> = {
  Approve: 'Passed',
  Reject: 'NotPassed'
};

@Component({
  selector: 'crm-approval-panel',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    ReactiveFormsModule,
    StageStatusBadgeComponent,
    NotesPanelComponent,
    CommentsThreadComponent,
    AttachmentListComponent,
    ActivityLogComponent,
    LocalDateTimePipe,
    TimeAgoPipe,
    EnumLabelPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (gate(); as g) {
      <article class="crm-stack">
        <header class="crm-card">
          <div class="crm-row">
            <strong>{{ g.opportunityNumber || g.opportunity?.opportunityNumber }}</strong>
            <span>{{ g.opportunityName || g.opportunity?.name }}</span>
            <crm-stage-status-badge
              [stageNameEn]="g.opportunity?.stageNameEn || ''"
              [stageNameAr]="g.opportunity?.stageNameAr || ''"
              [statusNameEn]="g.opportunity?.statusNameEn || ''"
              [statusNameAr]="g.opportunity?.statusNameAr || ''"
              [stageCode]="g.opportunity?.stageCode || g.gateCode || ''"
              [statusCode]="g.opportunity?.statusCode || g.state || ''"
            />
          </div>
          <p>
            {{ g.opportunity?.customerName || g.customerName }} · {{ gateName() }} ·
            {{ 'gates.round' | translate }} {{ g.round }} · {{ g.assignedRoleCode | enumLabel: 'roles' }}
          </p>
          <p class="crm-muted" [matTooltip]="utc(g.openedAtUtc) + ' · ' + (g.openedAtUtc | timeAgo)">
            {{ 'gates.opened' | translate }}: {{ g.openedAtUtc | localDateTime }}
            · {{ 'gates.age' | translate }}: {{ g.openedAtUtc | timeAgo }}
          </p>
        </header>

        <section class="crm-card crm-stack">
          <h3 class="crm-section-title">{{ 'gates.decision' | translate }}</h3>
          <form [formGroup]="form">
            <mat-radio-group formControlName="decision" [attr.aria-label]="'gates.decision' | translate">
              @for (d of decisions(); track d) {
                <mat-radio-button [value]="d">{{ ('gates.decisions.' + d) | translate }}</mat-radio-button>
              }
            </mat-radio-group>
            <mat-form-field appearance="outline" class="full">
              <mat-label>{{ 'gates.reason' | translate }}</mat-label>
              <textarea matInput rows="3" formControlName="reason"></textarea>
            </mat-form-field>
          </form>
        </section>

        <crm-notes-panel entityType="gates" [entityId]="g.id" />
        <crm-comments-thread entityType="gates" [entityId]="g.id" />
        <crm-attachment-list entityType="gates" [entityId]="g.id" />
        <crm-activity-log entityType="GateInstance" [entityId]="g.id" [opportunityId]="g.opportunityId" />

        <div class="crm-actions">
          <button mat-flat-button color="primary" type="button" [disabled]="form.invalid || g.state !== 'Pending'" (click)="submit()">
            {{ 'gates.submitDecision' | translate }}
          </button>
        </div>
      </article>
    }
  `,
  styles: `.full { width: 100%; display: block; margin-top: 0.75rem; }`
})
export class ApprovalPanelComponent {
  private readonly gates = inject(GateService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly i18n = inject(TranslateService);
  private readonly workflow = inject(WorkflowActionService);
  readonly gate = input<GateInstance | null>(null);
  readonly decided = signal(false);
  readonly decidedChange = output<void>();
  readonly form = new FormGroup({
    decision: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    reason: new FormControl('', { nonNullable: true })
  });

  readonly decisions = computed(() => {
    const fromGate = this.gate()?.gate?.allowedDecisions;
    if (fromGate) {
      return fromGate.split(',').map((s) => s.trim()).filter(Boolean);
    }
    const gateCode = (this.gate()?.gateCode || '').toUpperCase();
    return DECISIONS_BY_GATE_CODE[gateCode] ?? ['Approve', 'Reject'];
  });

  readonly gateName = computed(() => {
    const g = this.gate();
    return pickLocalized(
      this.i18n.getCurrentLang(),
      g?.gate?.nameEn || g?.gateNameEn,
      g?.gate?.nameAr || g?.gateNameAr,
      g?.gateCode || ''
    );
  });

  constructor() {
    this.form.controls.decision.valueChanges.subscribe((decision) => {
      const rejectLike = ['Reject', 'Return', 'NotQualified', 'NotPassed'].includes(decision);
      const reason = this.form.controls.reason;
      if (rejectLike) {
        reason.setValidators([Validators.required]);
      } else {
        reason.clearValidators();
      }
      reason.updateValueAndValidity();
    });
  }

  utc(value: string): string {
    return formatUtcTooltip(value);
  }

  submit(): void {
    const g = this.gate();
    if (!g || this.form.invalid) {
      return;
    }
    const { decision, reason } = this.form.getRawValue();
    const allowed = this.decisions();
    const mapped = LEGACY_DECISION_MAP[decision];
    const hasDecision = allowed.some((d) => d.toLowerCase() === decision.toLowerCase());
    const canMap = mapped ? allowed.some((d) => d.toLowerCase() === mapped.toLowerCase()) : false;
    const normalizedDecision = !hasDecision && canMap ? mapped! : decision;
    this.confirm
      .confirm({
        titleKey: 'gates.confirmTitle',
        messageKey: 'gates.confirmMessage',
        messageParams: { decision: normalizedDecision, gate: this.gateName() }
      })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.gates.decide(g.id, { decision: normalizedDecision, reason: reason || null }).subscribe(() => {
          this.toast.success('gates.decided');
          this.decided.set(true);
          this.workflow.chainAfterDecision(g, normalizedDecision, g.opportunityId).subscribe(() => {
            this.decidedChange.emit();
          });
        });
      });
  }
}
