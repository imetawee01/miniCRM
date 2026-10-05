import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  { path: 'login', pathMatch: 'full', redirectTo: 'auth/login' },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES)
  },
  { path: '403', loadComponent: () => import('./features/pages/forbidden.page').then((m) => m.ForbiddenPageComponent) },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES) },
      { path: 'inbox', loadChildren: () => import('./features/inbox/inbox.routes').then((m) => m.INBOX_ROUTES) },
      { path: 'opportunities', loadChildren: () => import('./features/opportunities/opportunities.routes').then((m) => m.OPPORTUNITY_ROUTES) },
      { path: 'qualification', loadChildren: () => import('./features/qualification/qualification.routes').then((m) => m.QUALIFICATION_ROUTES) },
      { path: 'proposals', loadChildren: () => import('./features/proposals/proposals.routes').then((m) => m.PROPOSAL_ROUTES) },
      { path: 'approvals', loadChildren: () => import('./features/approvals/approvals.routes').then((m) => m.APPROVAL_ROUTES) },
      { path: 'submission', loadChildren: () => import('./features/submission/submission.routes').then((m) => m.SUBMISSION_ROUTES) },
      { path: 'contracting', loadChildren: () => import('./features/contracting/contracting.routes').then((m) => m.CONTRACTING_ROUTES) },
      { path: 'notifications', loadChildren: () => import('./features/notifications/notifications.routes').then((m) => m.NOTIFICATION_ROUTES) },
      { path: 'reports', loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORT_ROUTES) },
      { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES) }
    ]
  },
  { path: '**', loadComponent: () => import('./features/pages/not-found.page').then((m) => m.NotFoundPageComponent) }
];
