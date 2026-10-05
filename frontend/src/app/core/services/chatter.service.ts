import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export type ChatterKind = 'note' | 'comment' | 'audit' | 'email' | 'attachment' | 'activity' | 'all';

export interface ChatterItem {
  kind: string;
  id: string;
  occurredAtUtc: string;
  authorName: string;
  title?: string | null;
  body: string;
  meta?: string | null;
}

export interface SmartButtons {
  attachments: number;
  emails: number;
  approvals: number;
  notes: number;
  comments: number;
  scopeItems: number;
  activities: number;
  contractId?: string | null;
}

export type ActivityType = 'Call' | 'Meeting' | 'Todo' | 'FollowUp';

export interface ActivityItem {
  id: string;
  opportunityId: string;
  type: ActivityType | number;
  summary: string;
  dueAtUtc: string;
  assignedUserId: string;
  assignedUserName: string;
  doneAtUtc?: string | null;
  note?: string | null;
  createdAtUtc: string;
  createdByName: string;
}

@Injectable({ providedIn: 'root' })
export class ChatterService {
  private readonly api = inject(ApiService);

  feed(opportunityId: string, kind?: string): Observable<ChatterItem[]> {
    return this.api.get<ChatterItem[]>(`/opportunities/${opportunityId}/chatter`, { kind });
  }

  smartButtons(opportunityId: string): Observable<SmartButtons> {
    return this.api.get<SmartButtons>(`/opportunities/${opportunityId}/smart-buttons`);
  }

  activities(opportunityId: string): Observable<ActivityItem[]> {
    return this.api.get<ActivityItem[]>(`/opportunities/${opportunityId}/activities`);
  }

  myActivities(overdueOnly = false, includeDone = false): Observable<ActivityItem[]> {
    return this.api.get<ActivityItem[]>('/activities/mine', { overdueOnly, includeDone });
  }

  createActivity(
    opportunityId: string,
    body: { type: ActivityType; summary: string; dueAtUtc: string; assignedUserId: string; note?: string }
  ): Observable<ActivityItem> {
    return this.api.post<ActivityItem>(`/opportunities/${opportunityId}/activities`, body);
  }

  completeActivity(id: string, note?: string): Observable<void> {
    return this.api.post<void>(`/activities/${id}/complete`, { note });
  }

  deleteActivity(id: string): Observable<void> {
    return this.api.delete<void>(`/activities/${id}`);
  }
}
