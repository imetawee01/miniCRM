import { Injectable, computed, signal } from '@angular/core';

const ACCESS_KEY = 'crm.accessToken';
const REFRESH_KEY = 'crm.refreshToken';
const EXPIRY_KEY = 'crm.expiresAtUtc';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly access = signal<string | null>(sessionStorage.getItem(ACCESS_KEY));
  private readonly refresh = signal<string | null>(localStorage.getItem(REFRESH_KEY));
  private readonly expires = signal<string | null>(sessionStorage.getItem(EXPIRY_KEY));

  readonly accessToken = this.access.asReadonly();
  readonly refreshToken = this.refresh.asReadonly();
  readonly expiresAtUtc = this.expires.asReadonly();
  readonly hasAccessToken = computed(() => !!this.access());

  setTokens(accessToken: string, refreshToken: string, expiresAtUtc: string): void {
    sessionStorage.setItem(ACCESS_KEY, accessToken);
    localStorage.setItem(REFRESH_KEY, refreshToken);
    sessionStorage.setItem(EXPIRY_KEY, expiresAtUtc);
    this.access.set(accessToken);
    this.refresh.set(refreshToken);
    this.expires.set(expiresAtUtc);
  }

  clear(): void {
    sessionStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    sessionStorage.removeItem(EXPIRY_KEY);
    this.access.set(null);
    this.refresh.set(null);
    this.expires.set(null);
  }
}
