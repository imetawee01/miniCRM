import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AppNotification } from '../../core/models/collaboration';
import { NotificationService } from '../../core/services/notification.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LocalDateTimePipe } from '../../shared/pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../shared/pipes/time-ago.pipe';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { formatUtcTooltip } from '../../core/utils/date-time';

@Component({
  selector: 'crm-notifications-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, PageHeaderComponent, LocalDateTimePipe, TimeAgoPipe, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="notifications.title">
        <button mat-stroked-button type="button" (click)="readAll()">{{ 'notifications.readAll' | translate }}</button>
      </crm-page-header>
      @if (!items().length) {
        <crm-empty-state titleKey="notifications.emptyTitle" messageKey="notifications.emptyMessage" />
      } @else {
        <mat-list>
          @for (n of items(); track n.id) {
            <a mat-list-item [routerLink]="n.linkUrl || '/dashboard'" (click)="read(n)">
              <span matListItemTitle>{{ titleOf(n) }}</span>
              <span matListItemLine>{{ bodyOf(n) }}</span>
              <span matListItemMeta [matTooltip]="utc(n.createdAtUtc) + ' · ' + (n.createdAtUtc | timeAgo)">
                {{ n.createdAtUtc | localDateTime }}
              </span>
            </a>
          }
        </mat-list>
      }
    </div>
  `
})
export class NotificationsPageComponent implements OnInit {
  private readonly api = inject(NotificationService);
  private readonly i18n = inject(TranslateService);
  readonly items = signal<AppNotification[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.list({ page: 1, pageSize: 50 }).subscribe((r) => this.items.set(r.items));
  }

  utc(v: string): string {
    return formatUtcTooltip(v);
  }

  titleOf(n: AppNotification): string {
    if (!n.messageKey) return n.title;
    const key = `notif.${n.messageKey}.title`;
    const translated = this.i18n.instant(key, this.paramsOf(n));
    return translated === key ? n.title : translated;
  }

  bodyOf(n: AppNotification): string {
    if (!n.messageKey) return n.body;
    const key = `notif.${n.messageKey}.body`;
    const translated = this.i18n.instant(key, this.paramsOf(n));
    return translated === key ? n.body : translated;
  }

  private paramsOf(n: AppNotification): Record<string, string> {
    if (!n.paramsJson) return {};
    try {
      return JSON.parse(n.paramsJson) as Record<string, string>;
    } catch {
      return {};
    }
  }

  read(n: AppNotification): void {
    if (!n.isRead) this.api.markRead(n.id).subscribe();
  }

  readAll(): void {
    this.api.markAllRead().subscribe(() => this.reload());
  }
}
