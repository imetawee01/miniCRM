import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { GateInstance } from '../../../../core/models/gate';
import { GateService } from '../../../../core/services/gate.service';
import { WorkflowActionService } from '../../../../core/services/workflow-action.service';
import { OpportunityRecordStore } from '../opportunity-record.store';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { EnumLabelPipe } from '../../../../shared/pipes/enum-label.pipe';
import { LocalDateTimePipe } from '../../../../shared/pipes/local-date-time.pipe';
import { LocalizedPipe } from '../../../../shared/pipes/localized.pipe';
import { ApprovalPanelComponent } from '../../../../shared/components/approval-panel/approval-panel.component';

@Component({
  selector: 'crm-approvals-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, EnumLabelPipe, LocalDateTimePipe, LocalizedPipe, ApprovalPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      @for (g of gates(); track g.id) {
        <details class="crm-card" [open]="g.state === 'Pending'">
          <summary>
            {{ g.gateNameEn | localized: g.gateNameAr : g.gateCode }} · {{ 'gates.round' | translate }} {{ g.round }} · {{ g.state | enumLabel }}
            · {{ g.openedAtUtc | localDateTime }}
            <a [routerLink]="['/approvals', g.id]">{{ 'common.open' | translate }}</a>
          </summary>
          @if (g.state === 'Pending') {
            <div class="crm-actions" style="margin-bottom: 0.75rem">
              <button mat-flat-button color="primary" type="button" (click)="act(g)">{{ 'opp.actNow' | translate }}</button>
            </div>
            <crm-approval-panel [gate]="g" />
          }
        </details>
      }
    </div>
  `
})
export class ApprovalsTabComponent implements OnInit {
  private readonly api = inject(GateService);
  private readonly workflow = inject(WorkflowActionService);
  private readonly store = inject(OpportunityRecordStore, { optional: true });
  private readonly route = inject(ActivatedRoute);
  readonly gates = signal<GateInstance[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const id = this.route.parent?.snapshot.paramMap.get('id') ?? this.store?.id() ?? null;
    if (id) this.api.forOpportunity(id).subscribe((rows) => this.gates.set(rows));
  }

  act(g: GateInstance): void {
    this.workflow.openGateDecide(g, g.opportunityId).subscribe((ok) => {
      if (ok) {
        this.reload();
        this.store?.reload();
      }
    });
  }
}
