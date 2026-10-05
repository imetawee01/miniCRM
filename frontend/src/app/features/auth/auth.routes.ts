import { Routes } from '@angular/router';
import { anonymousGuard } from '../../core/auth/anonymous.guard';
import { authGuard } from '../../core/auth/auth.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

export const AUTH_ROUTES: Routes = [
  { path: 'login', loadComponent: () => import('./login.page').then((m) => m.LoginPageComponent), canActivate: [anonymousGuard] },
  {
    path: 'change-password',
    loadComponent: () => import('./change-password.page').then((m) => m.ChangePasswordPageComponent),
    canActivate: [authGuard],
    canDeactivate: [unsavedChangesGuard]
  }
];
