import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../shared/material';

@Component({
  selector: 'crm-not-found-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <h1>404</h1>
      <p>{{ 'errors.notFound' | translate }}</p>
      <a mat-flat-button color="primary" routerLink="/dashboard">{{ 'nav.dashboard' | translate }}</a>
    </div>
  `
})
export class NotFoundPageComponent {}
