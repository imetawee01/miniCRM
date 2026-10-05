import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { timeAgo } from '../../core/utils/date-time';

@Pipe({ name: 'timeAgo', standalone: true, pure: false })
export class TimeAgoPipe implements PipeTransform {
  private readonly i18n = inject(TranslateService);

  transform(value: string | Date | null | undefined): string {
    return timeAgo(value, this.i18n.getCurrentLang() ?? 'en');
  }
}
