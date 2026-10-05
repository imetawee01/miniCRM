import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Attachment } from '../models/collaboration';
import { AttachmentCategory, CollaborationPath } from '../models/enums';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AttachmentsService {
  private readonly api = inject(ApiService);

  list(entityType: CollaborationPath, entityId: string): Observable<Attachment[]> {
    return this.api.get<Attachment[]>(`/${entityType}/${entityId}/attachments`);
  }

  upload(
    entityType: CollaborationPath,
    entityId: string,
    file: File,
    description: string,
    category: AttachmentCategory
  ): Observable<Attachment> {
    const form = new FormData();
    form.append('file', file);
    form.append('description', description);
    form.append('category', category);
    return this.api.postForm<Attachment>(`/${entityType}/${entityId}/attachments`, form);
  }

  metadata(id: string): Observable<Attachment> {
    return this.api.get<Attachment>(`/attachments/${id}`);
  }

  download(id: string): Observable<Blob> {
    return this.api.getBlob(`/attachments/${id}/download`);
  }

  update(id: string, body: { description?: string; category?: AttachmentCategory }): Observable<Attachment> {
    return this.api.put<Attachment>(`/attachments/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/attachments/${id}`);
  }
}
