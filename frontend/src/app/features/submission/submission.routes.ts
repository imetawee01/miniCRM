import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

export const SUBMISSION_ROUTES: Routes = [
  {
    path: ':id/outcome',
    loadComponent: () => import('./outcome.page').then((m) => m.OutcomePageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['BIDS_MGMT', 'AM'] }
  },
  {
    path: ':id',
    loadComponent: () => import('./submission.page').then((m) => m.SubmissionPageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['BIDS_MGMT'] }
  }
];
