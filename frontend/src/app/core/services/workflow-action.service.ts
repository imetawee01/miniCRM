import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { catchError, Observable, of, switchMap } from 'rxjs';
import { GateInstance } from '../models/gate';
import { AuthStore } from '../auth/auth.store';
import { GateService } from './gate.service';
import { QualificationService } from './qualification.service';
import { ProposalService } from './proposal.service';
import { ToastService } from './toast.service';
import {
  GateDecideDialogComponent,
  GateDecideDialogData,
  GateDecideDialogResult
} from '../../shared/components/gate-decide-dialog/gate-decide-dialog.component';
import {
  QualRouteDialogComponent,
  QualRouteDialogResult
} from '../../shared/components/qual-route-dialog/qual-route-dialog.component';
import {
  AssignBuilderDialogComponent,
  AssignBuilderDialogResult
} from '../../shared/components/assign-builder-dialog/assign-builder-dialog.component';

/** Opens inline workflow dialogs and runs chained follow-ups after a successful gate decision. */
@Injectable({ providedIn: 'root' })
export class WorkflowActionService {
  private readonly dialog = inject(MatDialog);
  private readonly gates = inject(GateService);
  private readonly qualification = inject(QualificationService);
  private readonly proposals = inject(ProposalService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);

  /** Load gate (if needed) and open the decide dialog. Emits true when a decision was submitted. */
  openGateDecide(gateOrId: GateInstance | string, opportunityId?: string): Observable<boolean> {
    const load$ =
      typeof gateOrId === 'string'
        ? this.gates.get(gateOrId)
        : of(gateOrId);

    return load$.pipe(
      switchMap((gate) =>
        this.dialog
          .open(GateDecideDialogComponent, {
            data: { gate } satisfies GateDecideDialogData,
            width: '560px',
            maxHeight: '90vh',
            autoFocus: 'dialog'
          })
          .afterClosed()
          .pipe(
            switchMap((result: GateDecideDialogResult | null | undefined) => {
              if (!result?.submitted) return of(false);
              return this.chainAfterDecision(gate, result.decision, opportunityId ?? gate.opportunityId);
            })
          )
      )
    );
  }

  openQualRoute(opportunityId: string): Observable<boolean> {
    return this.dialog
      .open(QualRouteDialogComponent, { width: '480px', autoFocus: 'dialog' })
      .afterClosed()
      .pipe(
        switchMap((result: QualRouteDialogResult | null | undefined) => {
          if (!result) return of(false);
          return this.qualification
            .setRoute(opportunityId, { requiresQualificationMeeting: result.requiresQualificationMeeting })
            .pipe(
              switchMap(() => {
                this.toast.success('qual.routeSaved');
                return of(true);
              })
            );
        })
      );
  }

  openAssignBuilder(opportunityId: string): Observable<boolean> {
    if (!this.canAssignBuilder()) {
      this.toast.info('proposal.assignPendingBids');
      return of(true);
    }
    return this.dialog
      .open(AssignBuilderDialogComponent, { width: '520px', autoFocus: 'dialog' })
      .afterClosed()
      .pipe(
        switchMap((result: AssignBuilderDialogResult | null | undefined) => {
          if (!result) return of(true);
          return this.proposals.assignBuilder(opportunityId, result).pipe(
            switchMap(() => {
              this.toast.success('proposal.assigned');
              return of(true);
            }),
            catchError(() => of(true))
          );
        })
      );
  }

  private canAssignBuilder(): boolean {
    return this.auth.hasRole(['BIDS_PRESALES', 'BIDS_MGMT', 'ADMIN']);
  }

  /** Follow-up dialogs after a successful gate decision (route, assign builder). */
  chainAfterDecision(gate: GateInstance, decision: string, opportunityId: string): Observable<boolean> {
    const code = (gate.gateCode || gate.gate?.code || '').toUpperCase();
    const d = decision.toLowerCase();

    if (code === 'GW1_REVIEW' && d === 'approve') {
      return this.openQualRoute(opportunityId);
    }
    if (
      (code === 'QUAL_DECISION' && d === 'qualified') ||
      (code === 'QUAL_MEETING' && (d === 'passed' || d === 'approve'))
    ) {
      return this.openAssignBuilder(opportunityId);
    }
    return of(true);
  }
}
