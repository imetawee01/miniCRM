import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ScopeItem, ScopeOfWork, UpsertScopeItemRequest } from '../models/opportunity';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class ScopeService {
  private readonly api = inject(ApiService);

  private normalizeScopeItem(item: unknown): ScopeItem {
    const source = (item ?? {}) as Record<string, unknown>;
    return {
      id: String(source['id'] ?? ''),
      scopeOfWorkId: String(source['scopeOfWorkId'] ?? source['scopeId'] ?? ''),
      title: String(source['title'] ?? ''),
      description: (source['description'] as string | null | undefined) ?? null,
      serviceLineId: String(source['serviceLineId'] ?? ''),
      serviceLineNameEn: (source['serviceLineNameEn'] as string | undefined) ?? (source['serviceLineName'] as string | undefined),
      serviceLineNameAr: (source['serviceLineNameAr'] as string | undefined),
      assignedUserId: (source['assignedUserId'] as string | null | undefined) ?? null,
      assignedUserName: (source['assignedUserName'] as string | null | undefined) ?? null,
      comment: (source['comment'] as string | null | undefined) ?? null,
      sortOrder: Number(source['sortOrder'] ?? 0)
    };
  }

  private normalizeScope(scope: unknown): ScopeOfWork {
    const source = (scope ?? {}) as Record<string, unknown>;
    const rawItems = source['items'] ?? source['scopeItems'] ?? [];
    const items = Array.isArray(rawItems) ? rawItems.map((item) => this.normalizeScopeItem(item)) : [];
    return {
      id: String(source['id'] ?? source['scopeOfWorkId'] ?? ''),
      opportunityId: String(source['opportunityId'] ?? ''),
      brief: String((source['brief'] ?? source['scopeBrief'] ?? '') as string),
      items
    };
  }

  get(opportunityId: string): Observable<ScopeOfWork> {
    return this.api
      .get<unknown>(`/opportunities/${opportunityId}/scope`)
      .pipe(map((response) => this.normalizeScope(response)));
  }

  updateBrief(opportunityId: string, brief: string): Observable<ScopeOfWork> {
    return this.api
      .put<unknown>(`/opportunities/${opportunityId}/scope`, { brief })
      .pipe(map((response) => this.normalizeScope(response)));
  }

  addItem(opportunityId: string, body: UpsertScopeItemRequest): Observable<ScopeItem> {
    return this.api.post<ScopeItem>(`/opportunities/${opportunityId}/scope/items`, body);
  }

  updateItem(opportunityId: string, itemId: string, body: UpsertScopeItemRequest): Observable<ScopeItem> {
    return this.api.put<ScopeItem>(`/opportunities/${opportunityId}/scope/items/${itemId}`, body);
  }

  deleteItem(opportunityId: string, itemId: string): Observable<void> {
    return this.api.delete<void>(`/opportunities/${opportunityId}/scope/items/${itemId}`);
  }

  reorder(opportunityId: string, itemIds: string[]): Observable<void> {
    return this.api.put<void>(`/opportunities/${opportunityId}/scope/items/reorder`, { itemIds });
  }
}
