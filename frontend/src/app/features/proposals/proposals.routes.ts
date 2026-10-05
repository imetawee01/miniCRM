import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

export const PROPOSAL_ROUTES: Routes = [
  {
    path: 'my-tasks',
    loadComponent: () => import('./my-tasks.page').then((m) => m.MyTasksPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['PRESALES', 'SL'] }
  },
  {
    path: ':id/workspace',
    loadComponent: () => import('./workspace.page').then((m) => m.BuilderWorkspacePageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['PRESALES', 'SL'] }
  },
  {
    path: ':id/pricing',
    loadComponent: () => import('./pricing.page').then((m) => m.PricingPageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['PRESALES'] }
  },
  {
    path: ':id/review',
    loadComponent: () => import('./review.page').then((m) => m.ProposalReviewPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['PRESALES'] }
  }
];
