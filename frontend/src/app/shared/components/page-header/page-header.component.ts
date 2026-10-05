import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-page-header',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="crm-page-header">
      <div>
        <h1>{{ titleKey() | translate }}</h1>
        @if (subtitleKey()) {
          <p class="crm-muted">{{ subtitleKey() | translate }}</p>
        }
      </div>
      <div class="crm-actions">
        @if (actionKey()) {
          <a mat-flat-button color="primary" [routerLink]="actionLink()">
            <mat-icon>add</mat-icon>
            {{ actionKey() | translate }}
          </a>
        }
        <ng-content />
      </div>
    </header>
  `,
  styles: `
    .crm-page-header {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
      flex-wrap: wrap;
      padding-inline-start: 0.875rem;
      border-inline-start: 3px solid var(--crm-teal);
    }

    h1 {
      margin: 0;
      font-size: 1.5rem;
      line-height: 1.25;
      font-weight: 700;
      letter-spacing: -0.01em;
      color: var(--crm-navy);
    }

    p {
      margin: 0.25rem 0 0;
      font-size: 0.875rem;
    }

    .crm-actions { padding-top: 0.25rem; }
  `
})
export class PageHeaderComponent {
  readonly titleKey = input.required<string>();
  readonly subtitleKey = input<string>('');
  readonly actionKey = input<string>('');
  readonly actionLink = input<string | string[]>('');
  readonly action = output<void>();
}
