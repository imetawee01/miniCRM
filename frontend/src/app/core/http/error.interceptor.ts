import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/paged-result';
import { ToastService } from '../services/toast.service';

const AUTH_ROUTES = ['/auth/login', '/auth/refresh', '/auth/sso'];

function isAuthRequest(url: string): boolean {
  return AUTH_ROUTES.some((path) => url.includes(path));
}

function requestPath(url: string): string {
  return new URL(url, 'http://local').pathname;
}

/** Secondary widgets / follow-ups: fail soft instead of ejecting the whole page. */
function isSecondaryForbiddenPath(path: string): boolean {
  return (
    /\/opportunities\/[^/]+\/builder$/.test(path) ||
    /\/opportunities\/[^/]+\/audit$/.test(path) ||
    /\/audit(\/|$)/.test(path) ||
    // Admin user directory; mention pickers should use /users/pickable instead.
    /\/users$/.test(path)
  );
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  const authRequest = isAuthRequest(req.url);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        return throwError(() => err);
      }

      const path = authRequest ? '' : requestPath(req.url);
      if (err.status === 403 && !authRequest && !isSecondaryForbiddenPath(path)) {
        void router.navigate(['/403']);
      }

      if (!authRequest && !isSecondaryForbiddenPath(path)) {
        const problem = err.error as ProblemDetails | undefined;
        const detail =
          problem?.detail ||
          problem?.title ||
          (typeof err.error === 'string' ? err.error : null) ||
          err.message;
        const trace = problem?.traceId ? ` [${problem.traceId}]` : '';
        toast.error(`${detail}${trace}`);
      }
      return throwError(() => err);
    })
  );
};
