import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { NextAction, OpportunityNextActions } from '../../../core/models/opportunity';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-action-center',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (actions()?.primary; as p) {
      <section class="action-center crm-card" [class.mine]="p.canCurrentUserAct">
        <div class="text">
          <span class="eyebrow">{{ 'opp.nextAction' | translate }}</span>
          <h3>{{ headline(p) }}</h3>
          <p>{{ description(p) }}</p>
          @if (p.blockers.length) {
            <ul class="blockers">
              @for (b of p.blockers; track b) {
                <li><mat-icon inline>warning_amber</mat-icon> {{ b }}</li>
              }
            </ul>
          }
        </div>
        <div class="cta">
          @if (p.canCurrentUserAct) {
            <button mat-flat-button color="primary" type="button" (click)="act.emit(p)">
              {{ 'opp.actNow' | translate }}
            </button>
          } @else if (p.primaryRoute) {
            <a mat-stroked-button [routerLink]="p.primaryRoute">{{ 'common.open' | translate }}</a>
          } @else if (p.responsibleUserName || p.responsibleRole) {
            <span class="waiting">
              <mat-icon inline>hourglass_top</mat-icon>
              {{ p.responsibleUserName || p.responsibleRole }}
            </span>
          }
        </div>
      </section>
    }
  `,
  styles: `
    .action-center {
      display: flex; gap: 1rem; align-items: center; justify-content: space-between; flex-wrap: wrap;
      border-inline-start: 4px solid var(--crm-border);
    }
    .action-center.mine { border-inline-start-color: var(--crm-teal); background: linear-gradient(90deg, #f0fdfa, transparent); }
    .eyebrow { font-size: 0.7rem; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; color: var(--crm-muted); }
    h3 { margin: 0.15rem 0; font-size: 1.05rem; color: var(--crm-navy); }
    p { margin: 0; color: var(--crm-muted); font-size: 0.875rem; }
    .blockers { list-style: none; margin: 0.5rem 0 0; padding: 0; color: #b45309; font-size: 0.8rem; }
    .blockers li { display: flex; align-items: center; gap: 0.25rem; }
    .waiting { display: inline-flex; align-items: center; gap: 0.35rem; color: var(--crm-muted); font-weight: 600; }
  `
})
export class ActionCenterComponent {
  private readonly i18n = inject(TranslateService);
  readonly actions = input<OpportunityNextActions | null>(null);
  readonly act = output<NextAction>();

  headline(a: NextAction): string {
    return (this.i18n.getCurrentLang() ?? 'en') === 'ar' ? a.headlineAr : a.headlineEn;
  }

  description(a: NextAction): string {
    return (this.i18n.getCurrentLang() ?? 'en') === 'ar' ? a.descriptionAr : a.descriptionEn;
  }
}
