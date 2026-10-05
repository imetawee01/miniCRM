import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuditLogEntry } from '../models/collaboration';
import { AuditAction } from '../models/enums';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

export interface AuditQuery extends PagedQuery {
  entityType?: string;
  entityId?: string;
  opportunityId?: string;
  actorUserId?: string;
  action?: AuditAction | string;
  fromUtc?: string;
  toUtc?: string;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly api = inject(ApiService);

  list(query: AuditQuery): Observable<PagedResult<AuditLogEntry>> {
    return this.api.get<PagedResult<AuditLogEntry>>('/audit', query);
  }

  forOpportunity(opportunityId: string, query?: PagedQuery): Observable<AuditLogEntry[]> {
    return this.api.get<AuditLogEntry[]>(`/opportunities/${opportunityId}/audit`, query);
  }

  export(query: AuditQuery): Observable<Blob> {
    return this.api.getBlob('/audit/export', query);
  }
}
