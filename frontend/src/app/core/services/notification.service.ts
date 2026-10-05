import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AppNotification } from '../models/collaboration';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);
  private readonly unread = signal(0);
  readonly unreadCount = this.unread.asReadonly();
  readonly hasUnread = computed(() => this.unread() > 0);

  list(query?: PagedQuery): Observable<PagedResult<AppNotification>> {
    return this.api.get<PagedResult<AppNotification>>('/notifications', query);
  }

  unreadCount$(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>('/notifications/unread-count').pipe(
      tap((res) => this.unread.set(res.count))
    );
  }

  markRead(id: string): Observable<void> {
    return this.api.post<void>(`/notifications/${id}/read`).pipe(
      tap(() => this.unread.update((n) => Math.max(0, n - 1)))
    );
  }

  markAllRead(): Observable<void> {
    return this.api.post<void>('/notifications/read-all').pipe(tap(() => this.unread.set(0)));
  }
}
