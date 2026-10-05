import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

export const QUALIFICATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./qualification-queue.page').then((m) => m.QualificationQueuePageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['BIDS_PRESALES', 'BIDS_MGMT', 'PRESALES', 'SL'] }
  },
  {
    path: ':id/gw1-review',
    loadComponent: () => import('./gw1-review.page').then((m) => m.Gw1ReviewPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['BIDS_PRESALES', 'BIDS_MGMT', 'ADMIN'] }
  },
  {
    path: ':id/decision',
    loadComponent: () => import('./sl-decision.page').then((m) => m.SlDecisionPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SL', 'PRESALES', 'BIDS_PRESALES', 'BIDS_MGMT', 'ADMIN'] }
  },
  {
    path: ':id/meeting',
    loadComponent: () => import('./meeting.page').then((m) => m.MeetingPageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['BIDS_MGMT'] }
  }
];
