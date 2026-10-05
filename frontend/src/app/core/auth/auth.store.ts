import { Injectable, computed, signal } from '@angular/core';
import { RoleCode } from '../models/enums';
import { UserProfile } from '../models/user';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly userSignal = signal<UserProfile | null>(null);
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.userSignal());
  readonly roles = computed(() => this.userSignal()?.roles ?? []);
  readonly displayName = computed(() => this.userSignal()?.displayName ?? '');
  readonly isAdmin = computed(() => this.roles().includes('ADMIN'));

  setUser(user: UserProfile | null): void {
    this.userSignal.set(user);
  }

  hasRole(role: RoleCode | RoleCode[] | string | string[]): boolean {
    const current = this.roles();
    if (current.includes('ADMIN')) {
      return true;
    }
    const needed = Array.isArray(role) ? role : [role];
    return needed.some((r) => current.includes(r as RoleCode));
  }

  hasPermission(permission: string | string[]): boolean {
    const user = this.userSignal();
    if (!user) {
      return false;
    }
    if (user.roles.includes('ADMIN')) {
      return true;
    }
    const perms = user.permissions ?? [];
    const needed = Array.isArray(permission) ? permission : [permission];
    if (perms.length === 0) {
      return this.hasRole(needed);
    }
    return needed.some((p) => perms.includes(p));
  }
}
