import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { distinctUntilChanged, filter, map, tap } from 'rxjs';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { OpportunityService } from '../../../core/services/opportunity.service';
import { WorkflowActionService } from '../../../core/services/workflow-action.service';
import { ChatterService, SmartButtons } from '../../../core/services/chatter.service';
import { NextAction } from '../../../core/models/opportunity';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { StageStatusBadgeComponent } from '../../../shared/components/stage-status-badge/stage-status-badge.component';
import { StatusBarComponent } from '../../../shared/components/status-bar/status-bar.component';
import { JourneyPanelComponent } from '../../../shared/components/journey-panel/journey-panel.component';
import { ActionCenterComponent } from '../../../shared/components/action-center/action-center.component';
import { SmartButtonsComponent } from '../../../shared/components/smart-buttons/smart-buttons.component';
import { RecordPagerComponent } from '../../../shared/components/record-pager/record-pager.component';
import { SarPipe } from '../../../shared/pipes/sar.pipe';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { OpportunityRecordStore } from './opportunity-record.store';

export const OPP_LIST_IDS_KEY = 'crm.opportunityListIds';

@Component({
  selector: 'crm-opportunity-detail-page',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    StageStatusBadgeComponent,
    StatusBarComponent,
    JourneyPanelComponent,
    ActionCenterComponent,
    SmartButtonsComponent,
    RecordPagerComponent,
    SarPipe,
    HasRoleDirective
  ],
  providers: [OpportunityRecordStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      @if (store.detail(); as o) {
        <header class="crm-card">
          <div class="crm-row">
            <h1>{{ o.opportunityNumber }} · {{ o.name }}</h1>
            <crm-stage-status-badge
              [stageCode]="o.stageCode || ''"
              [statusCode]="o.statusCode || ''"
              [stageNameEn]="o.stageNameEn || ''"
              [stageNameAr]="o.stageNameAr || ''"
              [statusNameEn]="o.statusNameEn || ''"
              [statusNameAr]="o.statusNameAr || ''"
            />
            <span class="crm-spacer"></span>
            <crm-record-pager [currentId]="o.id" [ids]="listIds()" />
            <a mat-stroked-button [routerLink]="['/opportunities', o.id, 'edit']">{{ 'common.edit' | translate }}</a>
            <ng-container *crmHasRole="['BIDS_PRESALES', 'BIDS_MGMT', 'ADMIN']">
              @if (o.statusCode === 'HOLD' || o.isOnHold) {
                <button mat-stroked-button type="button" (click)="resume()">{{ 'opp.resume' | translate }}</button>
              } @else {
                <button mat-button type="button" (click)="hold()">{{ 'opp.hold' | translate }}</button>
              }
              <button mat-button color="warn" type="button" (click)="cancel()">{{ 'opp.cancel' | translate }}</button>
            </ng-container>
          </div>
          <p>{{ o.customerName }} · {{ o.expectedValueSar | sar }}</p>
          <crm-status-bar [currentStage]="o.stageCode || ''" [currentStatus]="o.statusCode || ''" />
          <crm-action-center [actions]="store.nextActions()" (act)="onAction($event)" />
          <crm-smart-buttons [opportunityId]="o.id" [buttons]="smart()" />
          <crm-journey-panel [journey]="store.journey()" />
        </header>
        <nav mat-tab-nav-bar [tabPanel]="panel">
          @for (tab of tabs(); track tab.path) {
            <a
              mat-tab-link
              [routerLink]="tab.path"
              routerLinkActive
              #rla="routerLinkActive"
              [active]="rla.isActive"
            >
              {{ tab.key | translate }}
            </a>
          }
        </nav>
        <mat-tab-nav-panel #panel>
          <router-outlet />
        </mat-tab-nav-panel>
      } @else if (store.loading()) {
        <div class="crm-card skeleton">{{ 'common.loading' | translate }}</div>
      }
    </div>
  `,
  styles: `
    .skeleton { color: var(--crm-muted); padding: 2rem; }
  `
})
export class OpportunityDetailPageComponent implements OnInit {
  readonly store = inject(OpportunityRecordStore);
  private readonly api = inject(OpportunityService);
  private readonly chatter = inject(ChatterService);
  private readonly workflow = inject(WorkflowActionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  readonly smart = signal<SmartButtons | null>(null);
  readonly listIds = signal<string[]>([]);

  /** Reloads the record when the pager changes `:id` without destroying the shell. */
  private readonly routeId = toSignal(
    this.route.paramMap.pipe(
      map((p) => p.get('id') ?? ''),
      filter((id) => !!id),
      distinctUntilChanged(),
      tap((id) => {
        this.store.load(id);
        this.chatter.smartButtons(id).subscribe((b) => this.smart.set(b));
      })
    ),
    { initialValue: this.route.snapshot.paramMap.get('id') ?? '' }
  );

  tabs(): { path: string; key: string }[] {
    const o = this.store.detail();
    const id = o?.id ?? this.routeId() ?? this.route.snapshot.paramMap.get('id');
    const base = `/opportunities/${id}`;
    const all = [
      { path: `${base}/overview`, key: 'opp.tabs.overview' },
      { path: `${base}/scope`, key: 'opp.tabs.scope' },
      { path: `${base}/qualification`, key: 'opp.tabs.qualification' },
      { path: `${base}/proposal`, key: 'opp.tabs.proposal' },
      { path: `${base}/approvals`, key: 'opp.tabs.approvals' },
      { path: `${base}/attachments`, key: 'opp.tabs.attachments' },
      { path: `${base}/discussion`, key: 'opp.tabs.discussion' },
      { path: `${base}/emails`, key: 'opp.tabs.emails' },
      { path: `${base}/activity`, key: 'opp.tabs.activity' }
    ];
    if (o?.statusCode === 'WON' || o?.stageCode === 'CONTRACTING') {
      all.push({ path: `${base}/contract`, key: 'opp.tabs.contract' });
    }
    return all;
  }

  ngOnInit(): void {
    try {
      const raw = sessionStorage.getItem(OPP_LIST_IDS_KEY);
      if (raw) this.listIds.set(JSON.parse(raw) as string[]);
    } catch {
      /* ignore */
    }
    // Keep paramMap subscription live so the pager reloads on `:id` change.
    void this.routeId();
  }

  onAction(action: NextAction): void {
    const id = this.store.detail()?.id;
    if (!id) return;

    if (action.gateInstanceId) {
      this.workflow.openGateDecide(action.gateInstanceId, id).subscribe((ok) => {
        if (ok) this.reloadAll();
      });
      return;
    }

    switch (action.code) {
      case 'QUAL_ROUTE':
        this.workflow.openQualRoute(id).subscribe((ok) => {
          if (ok) this.reloadAll();
        });
        break;
      case 'ASSIGN_BUILDER':
        this.workflow.openAssignBuilder(id).subscribe((ok) => {
          if (ok) this.reloadAll();
        });
        break;
      default:
        if (action.primaryRoute) void this.router.navigateByUrl(action.primaryRoute);
        break;
    }
  }

  hold(): void {
    const id = this.store.detail()?.id;
    if (!id) return;
    this.confirm
      .reason({ titleKey: 'opp.holdTitle', messageKey: 'opp.holdMessage', confirmKey: 'opp.hold', warn: true })
      .subscribe((reason) => {
        if (!reason) return;
        this.api.hold(id, reason).subscribe(() => {
          this.toast.success('opp.held');
          this.reloadAll();
        });
      });
  }

  resume(): void {
    const id = this.store.detail()?.id;
    if (!id) return;
    this.api.resume(id).subscribe(() => {
      this.toast.success('opp.resumed');
      this.reloadAll();
    });
  }

  cancel(): void {
    const id = this.store.detail()?.id;
    if (!id) return;
    this.confirm
      .reason({ titleKey: 'opp.cancelTitle', messageKey: 'opp.cancelMessage', confirmKey: 'opp.cancel', warn: true })
      .subscribe((reason) => {
        if (!reason) return;
        this.api.cancel(id, reason).subscribe(() => {
          this.toast.success('opp.canceled');
          this.reloadAll();
        });
      });
  }

  private reloadAll(): void {
    this.store.reload();
    const id = this.store.detail()?.id ?? this.route.snapshot.paramMap.get('id');
    if (id) this.chatter.smartButtons(id).subscribe((b) => this.smart.set(b));
  }
}
