import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GateDecisionRequest, GateInstance, PendingApproval, StatusTransitionConfig, WorkflowGate } from '../models/gate';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class GateService {
  private readonly api = inject(ApiService);

  forOpportunity(opportunityId: string): Observable<GateInstance[]> {
    return this.api.get<GateInstance[]>(`/opportunities/${opportunityId}/gates`);
  }

  get(gateInstanceId: string): Observable<GateInstance> {
    return this.api.get<GateInstance>(`/gates/${gateInstanceId}`);
  }

  decide(gateInstanceId: string, body: GateDecisionRequest): Observable<GateInstance> {
    return this.api.post<GateInstance>(`/gates/${gateInstanceId}/decision`, body);
  }

  reassign(gateInstanceId: string, assignedUserId: string): Observable<void> {
    return this.api.post<void>(`/gates/${gateInstanceId}/reassign`, { assignedUserId });
  }

  pending(query?: PagedQuery): Observable<PagedResult<PendingApproval>> {
    return this.api.get<PagedResult<PendingApproval>>('/approvals/pending', query);
  }

  pendingCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>('/approvals/pending/count');
  }

  workflowGates(): Observable<WorkflowGate[]> {
    return this.api.get<WorkflowGate[]>('/workflow/gates');
  }

  updateWorkflowGate(id: string, body: Partial<WorkflowGate>): Observable<WorkflowGate> {
    return this.api.put<WorkflowGate>(`/workflow/gates/${id}`, body);
  }

  transitions(): Observable<StatusTransitionConfig[]> {
    return this.api.get<StatusTransitionConfig[]>('/workflow/transitions');
  }
}
