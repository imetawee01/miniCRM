import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { EmailTemplate, GeneratedEmail } from '../models/contract';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class EmailsService {
  private readonly api = inject(ApiService);

  forOpportunity(opportunityId: string): Observable<GeneratedEmail[]> {
    return this.api.get<GeneratedEmail[]>(`/opportunities/${opportunityId}/emails`);
  }

  get(id: string): Observable<GeneratedEmail> {
    return this.api.get<GeneratedEmail>(`/emails/${id}`);
  }

  regenerate(id: string): Observable<GeneratedEmail> {
    return this.api.post<GeneratedEmail>(`/emails/${id}/regenerate`);
  }

  markSent(id: string): Observable<void> {
    return this.api.post<void>(`/emails/${id}/mark-sent`);
  }

  downloadEml(id: string): Observable<Blob> {
    return this.api.getBlob(`/emails/${id}/eml`);
  }

  templates(): Observable<EmailTemplate[]> {
    return this.api.get<EmailTemplate[]>('/email-templates');
  }

  updateTemplate(code: string, body: Partial<EmailTemplate>): Observable<EmailTemplate> {
    return this.api.put<EmailTemplate>(`/email-templates/${code}`, body);
  }

  preview(code: string, opportunityId: string): Observable<GeneratedEmail> {
    return this.api.post<GeneratedEmail>(`/email-templates/${code}/preview`, { opportunityId });
  }
}
