import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RoleCode } from '../models/enums';
import { AuthStore } from './auth.store';

export const roleGuard: CanActivateFn = (route) => {
  const store = inject(AuthStore);
  const router = inject(Router);
  const roles = (route.data['roles'] as RoleCode[] | undefined) ?? [];
  if (roles.length === 0 || store.hasRole(roles)) {
    return true;
  }
  return router.createUrlTree(['/403']);
};
