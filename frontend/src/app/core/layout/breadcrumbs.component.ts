import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../shared/material';

@Component({
  selector: 'crm-breadcrumbs',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="crm-breadcrumb" [attr.aria-label]="'nav.breadcrumb' | translate">
      <a routerLink="/dashboard">{{ 'nav.dashboard' | translate }}</a>
      <ng-content />
    </nav>
  `,
  styles: `
    .crm-breadcrumb { display: flex; gap: 0.5rem; font-size: 0.85rem; color: var(--crm-muted); }
    a { color: inherit; }
  `
})
export class BreadcrumbsComponent {}
