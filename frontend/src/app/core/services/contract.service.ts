import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Contract, ContractMilestone, ContractNegotiationRound } from '../models/contract';
import { ContractStatus, NegotiationRoundStatus } from '../models/enums';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

export interface ContractListQuery extends PagedQuery {
  status?: ContractStatus | '';
  customerId?: string;
  valueFrom?: number | null;
  valueTo?: number | null;
  signedFrom?: string;
  signedTo?: string;
  expiringWithinDays?: number | null;
}

@Injectable({ providedIn: 'root' })
export class ContractService {
  private readonly api = inject(ApiService);

  list(query: ContractListQuery): Observable<PagedResult<Contract>> {
    return this.api.get<PagedResult<Contract>>('/contracts', query);
  }

  get(id: string): Observable<Contract> {
    return this.api.get<Contract>(`/contracts/${id}`);
  }

  createFromOpportunity(opportunityId: string, body: Partial<Contract> = {}): Observable<Contract> {
    return this.api.post<Contract>(`/opportunities/${opportunityId}/contract`, body);
  }

  update(id: string, body: Partial<Contract>): Observable<Contract> {
    return this.api.put<Contract>(`/contracts/${id}`, body);
  }

  changeStatus(id: string, toStatus: ContractStatus, reason?: string): Observable<void> {
    return this.api.post<void>(`/contracts/${id}/status`, { toStatus, reason });
  }

  rounds(id: string): Observable<ContractNegotiationRound[]> {
    return this.api.get<ContractNegotiationRound[]>(`/contracts/${id}/negotiation-rounds`);
  }

  addRound(id: string, body: Partial<ContractNegotiationRound>): Observable<ContractNegotiationRound> {
    return this.api.post<ContractNegotiationRound>(`/contracts/${id}/negotiation-rounds`, body);
  }

  updateRound(roundId: string, body: Partial<ContractNegotiationRound>): Observable<ContractNegotiationRound> {
    return this.api.put<ContractNegotiationRound>(`/contracts/negotiation-rounds/${roundId}`, body);
  }

  closeRound(roundId: string, status: Extract<NegotiationRoundStatus, 'Agreed' | 'Rejected'>): Observable<void> {
    return this.api.post<void>(`/contracts/negotiation-rounds/${roundId}/close`, { status });
  }

  milestones(id: string): Observable<ContractMilestone[]> {
    return this.api.get<ContractMilestone[]>(`/contracts/${id}/milestones`);
  }

  addMilestone(id: string, body: Partial<ContractMilestone>): Observable<ContractMilestone> {
    return this.api.post<ContractMilestone>(`/contracts/${id}/milestones`, body);
  }

  updateMilestone(milestoneId: string, body: Partial<ContractMilestone>): Observable<ContractMilestone> {
    return this.api.put<ContractMilestone>(`/contracts/milestones/${milestoneId}`, body);
  }

  sign(id: string, body: { signedAtUtc: string; signatory: string; attachmentId?: string }): Observable<Contract> {
    return this.api.post<Contract>(`/contracts/${id}/sign`, body);
  }
}
