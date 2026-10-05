import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { PendingApproval } from '../../core/models/gate';
import { ProposalTask } from '../../core/models/contract';
import { GateService } from '../../core/services/gate.service';
import { ProposalService } from '../../core/services/proposal.service';
import { WorkflowActionService } from '../../core/services/workflow-action.service';
import { pickLocalized } from '../../core/utils/locale';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LocalDateTimePipe } from '../../shared/pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../shared/pipes/time-ago.pipe';

type InboxTab = 'todo' | 'proposals' | 'overdue';

@Component({
  selector: 'crm-inbox-page',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    RouterLink,
    PageHeaderComponent,
    EmptyStateComponent,
    LocalDateTimePipe,
    TimeAgoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="inbox.title" subtitleKey="inbox.subtitle" />

      <mat-button-toggle-group [value]="tab()" (change)="tab.set($event.value)" aria-label="Inbox tabs">
        <mat-button-toggle value="todo">
          {{ 'inbox.todo' | translate }}
          @if (gates().length) {
            <span class="chip">{{ gates().length }}</span>
          }
        </mat-button-toggle>
        <mat-button-toggle value="proposals">
          {{ 'inbox.proposals' | translate }}
          @if (tasks().length) {
            <span class="chip">{{ tasks().length }}</span>
          }
        </mat-button-toggle>
      </mat-button-toggle-group>

      @if (tab() === 'todo') {
        @if (!gates().length) {
          <crm-empty-state titleKey="inbox.emptyTodoTitle" messageKey="inbox.emptyTodoMessage" />
        } @else {
          <ul class="list">
            @for (g of gates(); track g.id) {
              <li class="row crm-card">
                <div class="text">
                  <a class="id" [routerLink]="['/opportunities', g.opportunityId]">{{ g.opportunityNumber }}</a>
                  <strong>{{ g.opportunityName }}</strong>
                  <span class="meta">
                    {{ gateName(g) }} · {{ g.openedAtUtc | timeAgo }}
                    <span class="muted">({{ g.openedAtUtc | localDateTime }})</span>
                  </span>
                </div>
                <div class="actions">
                  <button mat-flat-button color="primary" type="button" (click)="decide(g)">
                    {{ 'opp.actNow' | translate }}
                  </button>
                  <a mat-stroked-button [routerLink]="['/opportunities', g.opportunityId]">{{ 'common.open' | translate }}</a>
                </div>
              </li>
            }
          </ul>
        }
      }

      @if (tab() === 'proposals') {
        @if (!tasks().length) {
          <crm-empty-state titleKey="inbox.emptyProposalsTitle" messageKey="inbox.emptyProposalsMessage" />
        } @else {
          <ul class="list">
            @for (t of tasks(); track t.opportunityId) {
              <li class="row crm-card">
                <div class="text">
                  <a class="id" [routerLink]="['/opportunities', t.opportunityId]">{{ t.opportunityNumber }}</a>
                  <strong>{{ t.opportunityName }}</strong>
                  <span class="meta">{{ t.customerName }} · {{ t.statusNameEn }}</span>
                </div>
                <div class="actions">
                  <a mat-flat-button color="primary" [routerLink]="['/opportunities', t.opportunityId, 'proposal']">
                    {{ 'proposal.workspace' | translate }}
                  </a>
                </div>
              </li>
            }
          </ul>
        }
      }
    </div>
  `,
  styles: `
    .chip {
      margin-inline-start: 0.35rem;
      padding: 0 0.4rem;
      border-radius: 999px;
      background: var(--crm-blue-soft);
      color: var(--crm-blue-strong);
      font-size: 0.7rem;
      font-weight: 700;
    }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.65rem; }
    .row {
      display: flex; gap: 1rem; align-items: center; justify-content: space-between; flex-wrap: wrap;
      padding: 0.9rem 1rem;
    }
    .text { display: flex; flex-direction: column; gap: 0.15rem; min-width: 0; }
    .id { font-size: 0.75rem; font-weight: 700; color: var(--crm-muted); text-decoration: none; }
    .meta { font-size: 0.8rem; color: var(--crm-ink); }
    .muted { color: var(--crm-muted); }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  `
})
export class InboxPageComponent implements OnInit {
  private readonly gatesApi = inject(GateService);
  private readonly proposalsApi = inject(ProposalService);
  private readonly workflow = inject(WorkflowActionService);
  private readonly i18n = inject(TranslateService);
  private readonly router = inject(Router);

  readonly tab = signal<InboxTab>('todo');
  readonly gates = signal<PendingApproval[]>([]);
  readonly tasks = signal<ProposalTask[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.gatesApi.pending({ page: 1, pageSize: 50 }).subscribe({
      next: (r) => this.gates.set(r.items),
      error: () => this.gates.set([])
    });
    this.proposalsApi.myTasks({ page: 1, pageSize: 50 }).subscribe({
      next: (r) => this.tasks.set(r.items),
      error: () => this.tasks.set([])
    });
  }

  gateName(g: PendingApproval): string {
    return pickLocalized(this.i18n.getCurrentLang(), g.gateNameEn, g.gateNameAr, g.gateCode || '');
  }

  decide(g: PendingApproval): void {
    this.workflow.openGateDecide(g, g.opportunityId).subscribe((ok) => {
      if (ok) {
        this.reload();
        void this.router.navigate(['/opportunities', g.opportunityId]);
      }
    });
  }
}
