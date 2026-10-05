import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { formatLocalDateTime, formatUtcTooltip } from '../../core/utils/date-time';

@Pipe({ name: 'localDateTime', standalone: true })
export class LocalDateTimePipe implements PipeTransform {
  private readonly i18n = inject(TranslateService);

  transform(value: string | Date | null | undefined, mode: 'local' | 'utc' = 'local'): string {
    const lang = this.i18n.getCurrentLang() ?? 'en';
    return mode === 'utc' ? formatUtcTooltip(value) : formatLocalDateTime(value, lang);
  }
}
