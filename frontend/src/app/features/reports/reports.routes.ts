import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';

const roles = ['BIDS_MGMT', 'MGMT', 'ADMIN'];

export const REPORT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./reports-home.page').then((m) => m.ReportsHomePageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'funnel',
    loadComponent: () => import('./report-pages').then((m) => m.FunnelReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'win-loss',
    loadComponent: () => import('./report-pages').then((m) => m.WinLossReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'cycle-time',
    loadComponent: () => import('./report-pages').then((m) => m.CycleTimeReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'sl-performance',
    loadComponent: () => import('./report-pages').then((m) => m.SlPerformanceReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'deadline-compliance',
    loadComponent: () => import('./report-pages').then((m) => m.DeadlineComplianceReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'approval-throughput',
    loadComponent: () => import('./report-pages').then((m) => m.ApprovalThroughputReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  },
  {
    path: 'pivot',
    loadComponent: () => import('./pivot-report.page').then((m) => m.PivotReportPageComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles }
  }
];
