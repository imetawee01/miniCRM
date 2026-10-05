import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { humanizeCode } from '../../core/utils/locale';

@Pipe({ name: 'enumLabel', standalone: true, pure: false })
export class EnumLabelPipe implements PipeTransform {
  private readonly i18n = inject(TranslateService);

  transform(value: string | number | null | undefined, prefix = 'enums'): string {
    if (value === null || value === undefined || value === '') {
      return '';
    }
    const key = `${prefix}.${value}`;
    const translated = this.i18n.instant(key);
    if (typeof translated === 'string' && translated && translated !== key) {
      return translated;
    }
    return humanizeCode(String(value));
  }
}
