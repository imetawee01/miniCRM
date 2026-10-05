import { Injectable } from '@angular/core';
import { MissingTranslationHandler, MissingTranslationHandlerParams } from '@ngx-translate/core';
import { humanizeCode } from '../utils/locale';

@Injectable()
export class HumanizeMissingTranslationHandler implements MissingTranslationHandler {
  handle(params: MissingTranslationHandlerParams): string {
    return humanizeCode(params.key);
  }
}
