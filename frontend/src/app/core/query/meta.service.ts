import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../services/api.service';
import { MetaField, SavedView } from './domain.models';

@Injectable({ providedIn: 'root' })
export class MetaService {
  private readonly api = inject(ApiService);

  fields(entity: string): Observable<MetaField[]> {
    return this.api.get<MetaField[]>(`/meta/${entity}/fields`);
  }

  savedViews(entityType: string): Observable<SavedView[]> {
    return this.api.get<SavedView[]>('/saved-views', { entityType });
  }

  createSavedView(body: {
    name: string;
    entityType: string;
    definitionJson: string;
    isShared: boolean;
    isDefault: boolean;
  }): Observable<string> {
    return this.api.post<string>('/saved-views', body);
  }

  deleteSavedView(id: string): Observable<void> {
    return this.api.delete<void>(`/saved-views/${id}`);
  }
}
