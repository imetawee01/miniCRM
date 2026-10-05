import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { JourneyStep, OpportunityJourney } from '../../../core/models/opportunity';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';

@Component({
  selector: 'crm-journey-panel',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LocalDateTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="journey crm-card">
      <button type="button" class="journey__toggle" (click)="open.set(!open())" [attr.aria-expanded]="open()">
        <mat-icon>{{ open() ? 'expand_less' : 'expand_more' }}</mat-icon>
        <span>{{ 'opp.journey' | translate }}</span>
      </button>
      @if (open()) {
        <ol class="journey__list">
          @for (step of journey()?.steps ?? []; track step.code) {
            <li [attr.data-state]="step.state">
              <span class="mark" aria-hidden="true">
                @switch (step.state) {
                  @case ('done') { <mat-icon>check_circle</mat-icon> }
                  @case ('current') { <mat-icon>radio_button_checked</mat-icon> }
                  @case ('skipped') { <mat-icon>redo</mat-icon> }
                  @default { <mat-icon>radio_button_unchecked</mat-icon> }
                }
              </span>
              <div class="body">
                <strong>{{ name(step) }}</strong>
                <span class="meta">
                  @if (step.actorName) { {{ step.actorName }} · }
                  @if (step.decision) { {{ step.decision }} · }
                  @if (step.atUtc) { {{ step.atUtc | localDateTime }} }
                  @if (!step.actorName && !step.decision && !step.atUtc) { {{ stateLabel(step.state) }} }
                </span>
              </div>
            </li>
          }
        </ol>
      }
    </section>
  `,
  styles: `
    .journey { padding: 0.5rem 0.85rem; }
    .journey__toggle {
      display: flex; align-items: center; gap: 0.35rem; width: 100%;
      border: 0; background: transparent; cursor: pointer; padding: 0.35rem 0;
      font: inherit; font-weight: 700; color: var(--crm-navy);
    }
    .journey__list { list-style: none; margin: 0.35rem 0 0.5rem; padding: 0; display: flex; flex-direction: column; gap: 0.45rem; }
    li { display: flex; gap: 0.6rem; align-items: flex-start; }
    .mark mat-icon { font-size: 20px; width: 20px; height: 20px; }
    li[data-state='done'] .mark { color: var(--crm-teal); }
    li[data-state='current'] .mark { color: var(--crm-blue-strong); }
    li[data-state='skipped'] .mark { color: var(--crm-muted); }
    li[data-state='notStarted'] .mark, li[data-state='pending'] .mark { color: #cbd5e1; }
    .body { display: flex; flex-direction: column; gap: 0.1rem; min-width: 0; }
    .body strong { font-size: 0.875rem; color: var(--crm-navy); }
    .meta { font-size: 0.75rem; color: var(--crm-muted); }
  `
})
export class JourneyPanelComponent {
  private readonly i18n = inject(TranslateService);
  readonly journey = input<OpportunityJourney | null>(null);
  readonly open = signal(true);

  name(step: JourneyStep): string {
    return (this.i18n.getCurrentLang() ?? 'en') === 'ar' ? step.nameAr : step.nameEn;
  }

  stateLabel(state: string): string {
    return this.i18n.instant(`opp.journeyState.${state}`);
  }
}
