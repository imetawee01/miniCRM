import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const INBOX_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./inbox.page').then((m) => m.InboxPageComponent),
    canActivate: [authGuard]
  }
];
