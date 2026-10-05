import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  CreateOpportunityRequest,
  OpportunityDetail,
  OpportunityJourney,
  OpportunityListItem,
  OpportunityListQuery,
  OpportunityNextActions,
  OpportunitySummary,
  StatusTransitionDto,
  UpdateOpportunityRequest
} from '../models/opportunity';
import { PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class OpportunityService {
  private readonly api = inject(ApiService);

  private normalizeDetail(detail: OpportunityDetail & {
    qualificationDeadline?: string | null;
    inquiriesDeadline?: string | null;
    estimatedCostDeadline?: string | null;
    internalDeadline?: string | null;
    submissionDeadline?: string | null;
  }): OpportunityDetail {
    const deadlines = detail.deadlines ?? {
      qualificationDeadline: detail.qualificationDeadline ?? null,
      inquiriesDeadline: detail.inquiriesDeadline ?? null,
      estimatedCostDeadline: detail.estimatedCostDeadline ?? null,
      internalDeadline: detail.internalDeadline ?? null,
      submissionDeadline: detail.submissionDeadline ?? null
    };

    return {
      ...detail,
      deadlines
    };
  }

  list(query: OpportunityListQuery): Observable<PagedResult<OpportunityListItem>> {
    return this.api.get<PagedResult<OpportunityListItem>>('/opportunities', query);
  }

  get(id: string): Observable<OpportunityDetail> {
    return this.api
      .get<OpportunityDetail & {
        qualificationDeadline?: string | null;
        inquiriesDeadline?: string | null;
        estimatedCostDeadline?: string | null;
        internalDeadline?: string | null;
        submissionDeadline?: string | null;
      }>(`/opportunities/${id}`)
      .pipe(map((detail) => this.normalizeDetail(detail)));
  }

  summary(id: string): Observable<OpportunitySummary> {
    return this.api.get<OpportunitySummary>(`/opportunities/${id}/summary`);
  }

  journey(id: string): Observable<OpportunityJourney> {
    return this.api.get<OpportunityJourney>(`/opportunities/${id}/journey`);
  }

  nextActions(id: string): Observable<OpportunityNextActions> {
    return this.api.get<OpportunityNextActions>(`/opportunities/${id}/next-actions`);
  }

  create(body: CreateOpportunityRequest): Observable<OpportunityDetail> {
    return this.api
      .post<OpportunityDetail & {
        qualificationDeadline?: string | null;
        inquiriesDeadline?: string | null;
        estimatedCostDeadline?: string | null;
        internalDeadline?: string | null;
        submissionDeadline?: string | null;
      }>('/opportunities', body)
      .pipe(map((detail) => this.normalizeDetail(detail)));
  }

  update(id: string, body: UpdateOpportunityRequest): Observable<OpportunityDetail> {
    return this.api
      .put<OpportunityDetail & {
        qualificationDeadline?: string | null;
        inquiriesDeadline?: string | null;
        estimatedCostDeadline?: string | null;
        internalDeadline?: string | null;
        submissionDeadline?: string | null;
      }>(`/opportunities/${id}`, body)
      .pipe(map((detail) => this.normalizeDetail(detail)));
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/opportunities/${id}`);
  }

  setDeadlines(id: string, body: Record<string, string | null>): Observable<void> {
    return this.api.put<void>(`/opportunities/${id}/deadlines`, body);
  }

  setOwner(id: string, ownerUserId: string): Observable<void> {
    return this.api.put<void>(`/opportunities/${id}/owner`, { ownerUserId });
  }

  timeline(id: string): Observable<unknown[]> {
    return this.api.get<unknown[]>(`/opportunities/${id}/timeline`);
  }

  availableTransitions(id: string): Observable<StatusTransitionDto[]> {
    return this.api.get<StatusTransitionDto[]>(`/opportunities/${id}/available-transitions`);
  }

  changeStatus(id: string, toStatusId: string, reason?: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${id}/status`, { toStatusId, reason });
  }

  hold(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${id}/hold`, { reason });
  }

  resume(id: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${id}/resume`);
  }

  cancel(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${id}/cancel`, { reason });
  }

  export(query: OpportunityListQuery): Observable<Blob> {
    return this.api.getBlob('/opportunities/export', query);
  }
}
