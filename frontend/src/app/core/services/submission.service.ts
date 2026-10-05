import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { OpportunityOutcome, SubmissionRecord } from '../models/contract';
import { OutcomeResult, SubmissionChannel } from '../models/enums';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly api = inject(ApiService);

  submit(
    opportunityId: string,
    body: { channel: SubmissionChannel; reference?: string; submittedAtUtc: string }
  ): Observable<SubmissionRecord> {
    return this.api.post<SubmissionRecord>(`/opportunities/${opportunityId}/submit`, body);
  }

  get(opportunityId: string): Observable<SubmissionRecord> {
    return this.api.get<SubmissionRecord>(`/opportunities/${opportunityId}/submission`);
  }

  recordOutcome(
    opportunityId: string,
    body: {
      result: OutcomeResult;
      announcedAtUtc: string;
      awardedValueSar?: number | null;
      competitorName?: string | null;
      lossReason?: string | null;
      notes?: string | null;
    }
  ): Observable<OpportunityOutcome> {
    return this.api.post<OpportunityOutcome>(`/opportunities/${opportunityId}/outcome`, body);
  }

  outcome(opportunityId: string): Observable<OpportunityOutcome> {
    return this.api.get<OpportunityOutcome>(`/opportunities/${opportunityId}/outcome`);
  }
}
