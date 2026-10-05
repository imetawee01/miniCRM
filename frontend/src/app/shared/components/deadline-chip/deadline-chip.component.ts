import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { deadlineTone, formatUtcTooltip } from '../../../core/utils/date-time';

@Component({
  selector: 'crm-deadline-chip',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LocalDateTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="chip"
      [class]="'chip--' + tone()"
      [matTooltip]="tooltip()"
    >
      {{ labelKey() | translate }}:
      @if (date()) {
        {{ date() | localDateTime }}
      } @else {
        {{ 'common.noDate' | translate }}
      }
    </span>
  `,
  styles: `
    .chip {
      display: inline-block; padding: 0.2rem 0.6rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600;
    }
    .chip--none { background: #eceff1; color: #546e7a; }
    .chip--ok { background: #e8f5e9; color: #1b5e20; }
    .chip--soon { background: #fff8e1; color: #e65100; }
    .chip--overdue { background: #ffebee; color: #b71c1c; }
  `
})
export class DeadlineChipComponent {
  private readonly i18n = inject(TranslateService);
  readonly date = input<string | null | undefined>(null);
  readonly labelKey = input('deadlines.generic');
  readonly tone = computed(() => deadlineTone(this.date()));
  readonly tooltip = computed(() => {
    const d = this.date();
    if (!d) {
      return this.i18n.instant('common.noDate');
    }
    return `${formatUtcTooltip(d)} · ${this.i18n.instant('common.relative')}`;
  });
}
