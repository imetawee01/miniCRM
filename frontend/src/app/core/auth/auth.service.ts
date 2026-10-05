import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, of, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { AuthTokens, ChangePasswordRequest, LoginRequest, SsoConfig, UserProfile } from '../models/user';
import { ApiService } from '../services/api.service';
import { AuthStore } from './auth.store';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly tokens = inject(TokenService);
  private readonly store = inject(AuthStore);
  private readonly router = inject(Router);
  private refreshInFlight$: Observable<AuthTokens> | null = null;

  login(payload: LoginRequest): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/auth/login', payload).pipe(tap((res) => this.applySession(res)));
  }

  logout(): Observable<void> {
    const refreshToken = this.tokens.refreshToken();
    const request$ = refreshToken
      ? this.api.post<void>('/auth/logout', { refreshToken }).pipe(catchError(() => of(void 0)))
      : of(void 0);
    return request$.pipe(
      tap(() => this.clearSession()),
      map(() => void 0)
    );
  }

  me(): Observable<UserProfile> {
    return this.api.get<UserProfile>('/auth/me').pipe(tap((user) => this.store.setUser(user)));
  }

  changePassword(payload: ChangePasswordRequest): Observable<void> {
    return this.api.post<void>('/auth/change-password', payload);
  }

  ssoConfig(): Observable<SsoConfig> {
    return this.api.get<SsoConfig>('/auth/sso/config').pipe(
      catchError(() => of({ ssoEnabled: false }))
    );
  }

  refresh(): Observable<AuthTokens> {
    const refreshToken = this.tokens.refreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.api.post<AuthTokens>('/auth/refresh', { refreshToken }).pipe(
        tap((res) => this.applySession(res)),
        shareReplay(1)
      );
      this.refreshInFlight$.subscribe({
        complete: () => {
          this.refreshInFlight$ = null;
        },
        error: () => {
          this.refreshInFlight$ = null;
        }
      });
    }
    return this.refreshInFlight$;
  }

  restoreSession(): Observable<UserProfile | null> {
    if (!this.tokens.accessToken() && !this.tokens.refreshToken()) {
      return of(null);
    }
    const load$ = this.tokens.accessToken()
      ? this.me()
      : this.refresh().pipe(switchMap(() => this.me()));
    return load$.pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401 && this.tokens.refreshToken()) {
          return this.refresh().pipe(
            switchMap(() => this.me()),
            catchError(() => {
              this.clearSession();
              return of(null);
            })
          );
        }
        this.clearSession();
        return of(null);
      })
    );
  }

  clearSession(): void {
    this.tokens.clear();
    this.store.setUser(null);
  }

  navigateAfterLogout(): void {
    void this.router.navigate(['/auth/login']);
  }

  private applySession(res: AuthTokens): void {
    this.tokens.setTokens(res.accessToken, res.refreshToken, res.expiresAtUtc);
    this.store.setUser(res.user);
  }
}
