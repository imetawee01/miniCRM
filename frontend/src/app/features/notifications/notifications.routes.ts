import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const NOTIFICATION_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./notifications.page').then((m) => m.NotificationsPageComponent), canActivate: [authGuard] }
];
