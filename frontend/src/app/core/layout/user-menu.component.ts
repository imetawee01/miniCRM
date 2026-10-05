import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { AuthStore } from '../auth/auth.store';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';

@Component({
  selector: 'crm-user-menu',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, UserAvatarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      class="account"
      mat-button
      type="button"
      [matMenuTriggerFor]="menu"
      [attr.aria-label]="'nav.account' | translate"
    >
      <crm-user-avatar [name]="store.displayName()" />
      <span class="account__name crm-hide-sm">{{ store.displayName() }}</span>
      <mat-icon class="account__caret crm-hide-sm">expand_more</mat-icon>
    </button>
    <mat-menu #menu="matMenu">
      <button mat-menu-item type="button" (click)="go('/auth/change-password')">
        <mat-icon>lock_reset</mat-icon>
        {{ 'auth.changePassword' | translate }}
      </button>
      <button mat-menu-item type="button" (click)="logout()">
        <mat-icon>logout</mat-icon>
        {{ 'auth.logout' | translate }}
      </button>
    </mat-menu>
  `,
  styles: `
    .account {
      display: inline-flex;
      align-items: center;
      color: var(--crm-navy);
      font-weight: 600;
    }

    :host ::ng-deep .account .mdc-button__label {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
    }

    .account:hover {
      background: var(--crm-blue-soft);
    }

    .account__name {
      max-width: 160px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      font-size: 0.85rem;
    }

    .account__caret {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: var(--crm-muted);
    }
  `
})
export class UserMenuComponent {
  readonly store = inject(AuthStore);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  go(path: string): void {
    void this.router.navigateByUrl(path);
  }

  logout(): void {
    this.auth.logout().subscribe(() => this.auth.navigateAfterLogout());
  }
}
