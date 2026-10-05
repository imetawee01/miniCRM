import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({ name: 'sar', standalone: true })
export class SarPipe implements PipeTransform {
  private readonly i18n = inject(TranslateService);

  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '';
    }
    const formatted = new Intl.NumberFormat(this.i18n.getCurrentLang() === 'ar' ? 'ar-SA' : 'en-SA', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
    return `${formatted} ${this.i18n.instant('common.sar')}`;
  }
}
