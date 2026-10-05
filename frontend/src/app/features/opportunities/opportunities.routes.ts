import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/auth/unsaved-changes.guard';

export const OPPORTUNITY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./list/opportunity-list.page').then((m) => m.OpportunityListPageComponent), canActivate: [authGuard] },
  {
    path: 'new',
    loadComponent: () => import('./create/opportunity-create.page').then((m) => m.OpportunityCreatePageComponent),
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: ['AM', 'BIDS_PRESALES', 'ADMIN'] }
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./edit/opportunity-edit.page').then((m) => m.OpportunityEditPageComponent),
    canActivate: [authGuard],
    canDeactivate: [unsavedChangesGuard]
  },
  {
    path: ':id',
    loadComponent: () => import('./detail/opportunity-detail.page').then((m) => m.OpportunityDetailPageComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', loadComponent: () => import('./detail/tabs/overview.tab').then((m) => m.OverviewTabComponent) },
      { path: 'scope', loadComponent: () => import('./detail/tabs/scope.tab').then((m) => m.ScopeTabComponent) },
      { path: 'qualification', loadComponent: () => import('./detail/tabs/qualification.tab').then((m) => m.QualificationTabComponent) },
      { path: 'proposal', loadComponent: () => import('./detail/tabs/proposal.tab').then((m) => m.ProposalTabComponent) },
      { path: 'approvals', loadComponent: () => import('./detail/tabs/approvals.tab').then((m) => m.ApprovalsTabComponent) },
      { path: 'attachments', loadComponent: () => import('./detail/tabs/attachments.tab').then((m) => m.AttachmentsTabComponent) },
      { path: 'discussion', loadComponent: () => import('./detail/tabs/discussion.tab').then((m) => m.DiscussionTabComponent) },
      { path: 'emails', loadComponent: () => import('./detail/tabs/emails.tab').then((m) => m.EmailsTabComponent) },
      { path: 'activity', loadComponent: () => import('./detail/tabs/activity.tab').then((m) => m.ActivityTabComponent) },
      { path: 'contract', loadComponent: () => import('./detail/tabs/contract.tab').then((m) => m.ContractTabComponent) }
    ]
  }
];
