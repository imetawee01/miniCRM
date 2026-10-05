import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Comment } from '../models/collaboration';
import { CollaborationPath } from '../models/enums';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly api = inject(ApiService);

  list(entityType: CollaborationPath, entityId: string): Observable<Comment[]> {
    return this.api.get<Comment[]>(`/${entityType}/${entityId}/comments`);
  }

  create(
    entityType: CollaborationPath,
    entityId: string,
    body: string,
    parentCommentId?: string | null,
    mentionedUserIds?: string[]
  ): Observable<Comment> {
    return this.api.post<Comment>(`/${entityType}/${entityId}/comments`, {
      body,
      parentCommentId,
      mentionedUserIds
    });
  }

  update(id: string, body: string): Observable<Comment> {
    return this.api.put<Comment>(`/comments/${id}`, { body });
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/comments/${id}`);
  }
}
