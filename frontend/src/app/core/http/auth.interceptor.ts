import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { TokenService } from '../auth/token.service';

const SKIP = ['/auth/login', '/auth/refresh', '/auth/sso'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenService);
  const auth = inject(AuthService);
  const router = inject(Router);
  const skip = SKIP.some((p) => req.url.includes(p)) || req.headers.has('X-Skip-Auth');
  const token = tokens.accessToken();
  const authReq =
    !skip && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || skip) {
        return throwError(() => err);
      }
      return auth.refresh().pipe(
        switchMap(() => {
          const retryToken = tokens.accessToken();
          const retry = retryToken
            ? req.clone({ setHeaders: { Authorization: `Bearer ${retryToken}` } })
            : req;
          return next(retry);
        }),
        catchError((refreshErr) => {
          auth.clearSession();
          void router.navigate(['/auth/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
