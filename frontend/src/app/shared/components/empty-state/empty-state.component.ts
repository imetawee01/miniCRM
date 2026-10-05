import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-empty-state',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-empty" role="status">
      <span class="crm-empty__chip" aria-hidden="true">
        <mat-icon>{{ icon() }}</mat-icon>
      </span>
      <h2>{{ titleKey() | translate }}</h2>
      <p>{{ messageKey() | translate }}</p>
      @if (ctaKey() && ctaLink()) {
        <a mat-flat-button color="primary" [routerLink]="ctaLink()">{{ ctaKey() | translate }}</a>
      }
      <ng-content />
    </div>
  `,
  styles: `
    .crm-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 3rem 1.25rem;
      color: var(--crm-muted);
    }

    .crm-empty__chip {
      display: inline-grid;
      place-items: center;
      width: 64px;
      height: 64px;
      border-radius: 50%;
      background: var(--crm-blue-soft);
      margin-bottom: 1rem;
    }

    mat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: var(--crm-teal-dark);
    }

    h2 {
      color: var(--crm-navy);
      font-size: 1.05rem;
      font-weight: 700;
      margin: 0 0 0.35rem;
    }

    p {
      margin: 0 0 1.25rem;
      font-size: 0.875rem;
      max-width: 32ch;
    }
  `
})
export class EmptyStateComponent {
  readonly titleKey = input.required<string>();
  readonly messageKey = input.required<string>();
  readonly icon = input('inbox');
  readonly ctaKey = input('');
  readonly ctaLink = input<string | string[]>('');
}
