import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BidBond } from '../models/contract';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class BidBondService {
  private readonly api = inject(ApiService);

  get(opportunityId: string): Observable<BidBond> {
    return this.api.get<BidBond>(`/opportunities/${opportunityId}/bid-bond`);
  }

  request(opportunityId: string): Observable<BidBond> {
    return this.api.post<BidBond>(`/opportunities/${opportunityId}/bid-bond/request`);
  }

  issue(
    opportunityId: string,
    body: { amount: number; validUntil: string; issuingBank: string; attachmentId?: string }
  ): Observable<BidBond> {
    return this.api.post<BidBond>(`/opportunities/${opportunityId}/bid-bond/issue`, body);
  }

  reject(opportunityId: string, reason: string): Observable<BidBond> {
    return this.api.post<BidBond>(`/opportunities/${opportunityId}/bid-bond/reject`, { reason });
  }
}
