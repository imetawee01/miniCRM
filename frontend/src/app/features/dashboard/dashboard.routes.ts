import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const DASHBOARD_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./dashboard.page').then((m) => m.DashboardPageComponent), canActivate: [authGuard] }
];
