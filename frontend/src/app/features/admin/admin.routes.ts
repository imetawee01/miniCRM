import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';

const admin = { canActivate: [authGuard, roleGuard], data: { roles: ['ADMIN'] } };

export const ADMIN_ROUTES: Routes = [
  { path: 'users', loadComponent: () => import('./admin-users.page').then((m) => m.AdminUsersPageComponent), ...admin },
  { path: 'roles', loadComponent: () => import('./admin-pages').then((m) => m.AdminRolesPageComponent), ...admin },
  { path: 'customers', loadComponent: () => import('./admin-pages').then((m) => m.AdminCustomersPageComponent), ...admin },
  { path: 'service-lines', loadComponent: () => import('./admin-pages').then((m) => m.AdminServiceLinesPageComponent), ...admin },
  { path: 'lookups', loadComponent: () => import('./admin-pages').then((m) => m.AdminLookupsPageComponent), ...admin },
  { path: 'email-templates', loadComponent: () => import('./admin-pages').then((m) => m.AdminEmailTemplatesPageComponent), ...admin },
  { path: 'workflow', loadComponent: () => import('./admin-pages').then((m) => m.AdminWorkflowPageComponent), ...admin },
  { path: 'audit', loadComponent: () => import('./admin-pages').then((m) => m.AdminAuditPageComponent), ...admin },
  { path: 'permissions', loadComponent: () => import('./admin-permissions.page').then((m) => m.AdminPermissionsPageComponent), ...admin }
];
