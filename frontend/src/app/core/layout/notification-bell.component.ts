import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../services/notification.service';
import { MATERIAL_IMPORTS } from '../../shared/material';

@Component({
  selector: 'crm-notification-bell',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="bell"
      mat-icon-button
      routerLink="/notifications"
      [matBadge]="count()"
      [matBadgeHidden]="!count()"
      matBadgeSize="small"
      [attr.aria-label]="'nav.notifications' | translate"
    >
      <mat-icon>notifications</mat-icon>
    </a>
  `,
  styles: `
    .bell {
      color: var(--crm-muted);
      --mat-badge-background-color: var(--crm-danger);
      --mat-badge-text-color: #fff;
    }

    .bell:hover {
      color: var(--crm-teal-dark);
    }
  `
})
export class NotificationBellComponent implements OnInit {
  private readonly notifications = inject(NotificationService);
  readonly count = this.notifications.unreadCount;

  ngOnInit(): void {
    this.notifications.unreadCount$().subscribe();
  }
}
