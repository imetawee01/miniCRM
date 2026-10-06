import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { GateService } from '../services/gate.service';
import { AuthStore } from './auth.store';

export const gateRoleGuard: CanActivateFn = (route) => {
  const store = inject(AuthStore);
  const router = inject(Router);
  const gates = inject(GateService);
  const id = route.paramMap.get('gateInstanceId');
  if (!id) {
    return router.createUrlTree(['/403']);
  }
  if (store.isAdmin()) {
    return true;
  }
  return gates.get(id).pipe(
    map((gate) => {
      const raw = [gate.assignedRoleCode, gate.gate?.responsibleRoleCode].filter(Boolean).join('|');
      const allowed = raw
        .split(/[|,]/)
        .map((s) => s.trim())
        .filter(Boolean);
      return store.hasRole(allowed) ? true : router.createUrlTree(['/403']);
    }),
    catchError(() => of(router.createUrlTree(['/403'])))
  );
};
