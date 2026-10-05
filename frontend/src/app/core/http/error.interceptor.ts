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

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  const authRequest = isAuthRequest(req.url);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        return throwError(() => err);
      }
      if (err.status === 403 && !authRequest) {
        const path = new URL(req.url, 'http://local').pathname;
        // Workflow follow-up calls (assign builder) must not eject the user from the page they just completed.
        const stayOnPage = /\/opportunities\/[^/]+\/builder$/.test(path);
        if (!stayOnPage) {
          void router.navigate(['/403']);
        }
      }
      if (!authRequest) {
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
