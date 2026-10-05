import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignBuilderRequest, EstimatedCost, ProposalPricing, ProposalTask, SlResponse } from '../models/contract';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class ProposalService {
  private readonly api = inject(ApiService);

  assignBuilder(opportunityId: string, body: AssignBuilderRequest): Observable<void> {
    return this.api.post<void>(`/opportunities/${opportunityId}/builder`, body);
  }

  estimatedCost(opportunityId: string): Observable<EstimatedCost> {
    return this.api.get<EstimatedCost>(`/opportunities/${opportunityId}/estimated-cost`);
  }

  submitEstimatedCost(opportunityId: string, amount: number, notes?: string): Observable<EstimatedCost> {
    return this.api.post<EstimatedCost>(`/opportunities/${opportunityId}/estimated-cost`, { amount, notes });
  }

  slResponses(opportunityId: string): Observable<SlResponse[]> {
    return this.api.get<SlResponse[]>(`/opportunities/${opportunityId}/sl-responses`);
  }

  createSlResponse(opportunityId: string, body: Partial<SlResponse>): Observable<SlResponse> {
    return this.api.post<SlResponse>(`/opportunities/${opportunityId}/sl-responses`, body);
  }

  updateSlResponse(id: string, body: Partial<SlResponse>): Observable<SlResponse> {
    return this.api.put<SlResponse>(`/sl-responses/${id}`, body);
  }

  submitSlResponse(id: string): Observable<void> {
    return this.api.post<void>(`/sl-responses/${id}/submit`);
  }

  returnSlResponse(id: string, reason: string): Observable<void> {
    return this.api.post<void>(`/sl-responses/${id}/return`, { reason });
  }

  pricing(opportunityId: string): Observable<ProposalPricing[]> {
    return this.api.get<ProposalPricing[]>(`/opportunities/${opportunityId}/pricing`);
  }

  addPricing(opportunityId: string, priceSar: number, costSar: number): Observable<ProposalPricing> {
    return this.api.post<ProposalPricing>(`/opportunities/${opportunityId}/pricing`, { priceSar, costSar });
  }

  markReady(opportunityId: string): Observable<void> {
    return this.api.post<void>(`/opportunities/${opportunityId}/proposal/ready`);
  }

  myTasks(query?: PagedQuery): Observable<PagedResult<ProposalTask>> {
    return this.api.get<PagedResult<ProposalTask>>('/proposals/my-tasks', query);
  }
}
