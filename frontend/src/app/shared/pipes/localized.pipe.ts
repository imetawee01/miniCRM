import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { pickLocalized } from '../../core/utils/locale';

@Pipe({ name: 'localized', standalone: true, pure: false })
export class LocalizedPipe implements PipeTransform {
  private readonly i18n = inject(TranslateService);

  transform(en?: string | null, ar?: string | null, fallback = ''): string {
    return pickLocalized(this.i18n.getCurrentLang(), en, ar, fallback);
  }
}
