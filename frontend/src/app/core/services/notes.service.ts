import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Note } from '../models/collaboration';
import { CollaborationPath, NoteVisibility } from '../models/enums';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotesService {
  private readonly api = inject(ApiService);

  list(entityType: CollaborationPath, entityId: string): Observable<Note[]> {
    return this.api.get<Note[]>(`/${entityType}/${entityId}/notes`);
  }

  create(entityType: CollaborationPath, entityId: string, body: string, visibility: NoteVisibility): Observable<Note> {
    return this.api.post<Note>(`/${entityType}/${entityId}/notes`, { body, visibility });
  }

  update(id: string, body: string, visibility?: NoteVisibility): Observable<Note> {
    return this.api.put<Note>(`/notes/${id}`, { body, visibility });
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/notes/${id}`);
  }
}
