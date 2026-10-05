import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { LangSwitcherComponent } from './lang-switcher.component';
import { NotificationBellComponent } from './notification-bell.component';
import { UserMenuComponent } from './user-menu.component';

@Component({
  selector: 'crm-topbar',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LangSwitcherComponent, NotificationBellComponent, UserMenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-toolbar class="topbar">
      <button
        class="topbar__toggle"
        mat-icon-button
        type="button"
        (click)="toggle()"
        [attr.aria-label]="'nav.menu' | translate"
      >
        <mat-icon>menu</mat-icon>
      </button>
      <span class="crm-spacer"></span>
      <div class="topbar__actions">
        <crm-lang-switcher />
        <crm-notification-bell />
        <span class="topbar__divider" aria-hidden="true"></span>
        <crm-user-menu />
      </div>
    </mat-toolbar>
  `,
  styles: `
    .topbar {
      position: sticky;
      top: 0;
      z-index: 10;
      background: var(--crm-surface-card);
      color: var(--crm-navy);
      height: var(--crm-topbar-height);
      min-height: var(--crm-topbar-height);
      padding-inline: 0.75rem 1.25rem;
      border-bottom: 1px solid var(--crm-border);
      box-shadow: var(--crm-shadow-sm);
    }

    .topbar__toggle {
      color: var(--crm-muted);
    }

    .topbar__toggle:hover {
      color: var(--crm-teal-dark);
    }

    .topbar__actions {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .topbar__divider {
      width: 1px;
      height: 24px;
      margin-inline: 0.5rem;
      background: var(--crm-border);
    }
  `
})
export class TopbarComponent {
  toggleSidenav?: () => void;
  toggle(): void {
    this.toggleSidenav?.();
  }
}
