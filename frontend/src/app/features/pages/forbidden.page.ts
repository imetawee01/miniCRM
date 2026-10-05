import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../shared/material';

@Component({
  selector: 'crm-forbidden-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <h1>403</h1>
      <p>{{ 'errors.forbidden' | translate }}</p>
      <a mat-flat-button color="primary" routerLink="/dashboard">{{ 'nav.dashboard' | translate }}</a>
    </div>
  `
})
export class ForbiddenPageComponent {}
