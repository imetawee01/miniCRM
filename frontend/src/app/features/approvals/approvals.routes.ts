import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { gateRoleGuard } from '../../core/auth/gate-role.guard';

export const APPROVAL_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pending-approvals.page').then((m) => m.PendingApprovalsPageComponent), canActivate: [authGuard] },
  {
    path: ':gateInstanceId',
    loadComponent: () => import('./approval-detail.page').then((m) => m.ApprovalDetailPageComponent),
    canActivate: [authGuard, gateRoleGuard]
  }
];
