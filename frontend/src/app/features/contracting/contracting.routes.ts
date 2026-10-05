import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

const roles = ['BIDS_MGMT', 'MGMT', 'ADMIN'];

export const CONTRACTING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./contract-list.page').then((m) => m.ContractListPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: ':id',
    loadComponent: () => import('./contract-detail.page').then((m) => m.ContractDetailPageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles }
  }
];
