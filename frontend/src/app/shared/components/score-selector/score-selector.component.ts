import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-score-selector',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <fieldset class="score">
      <legend>{{ labelKey() | translate }}</legend>
      <mat-radio-group [formControl]="control()" [attr.aria-label]="labelKey() | translate">
        @for (n of scores; track n) {
          <mat-radio-button [value]="n">{{ n }}</mat-radio-button>
        }
      </mat-radio-group>
      <p class="hint">{{ 'opp.scoreHint' | translate }}</p>
    </fieldset>
  `,
  styles: `
    .score { border: 1px solid var(--crm-border); border-radius: 8px; padding: 0.5rem 0.75rem; margin: 0 0 1rem; }
    legend { padding: 0 0.35rem; color: var(--crm-navy); font-size: 0.85rem; }
    .hint { margin: 0.35rem 0 0; color: var(--crm-muted); font-size: 0.75rem; }
  `
})
export class ScoreSelectorComponent {
  readonly control = input.required<FormControl<number>>();
  readonly labelKey = input.required<string>();
  readonly scores = [1, 2, 3, 4, 5];
}
