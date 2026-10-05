import { Injectable } from '@angular/core';
import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import en from '../../../assets/i18n/en.json';
import ar from '../../../assets/i18n/ar.json';

/**
 * Loads EN/AR from the JS bundle so localization works on IIS/subpaths
 * even when /assets/i18n/*.json is missing or blocked.
 */
@Injectable()
export class BundledTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<TranslationObject> {
    const table = lang.toLowerCase().startsWith('ar') ? ar : en;
    return of(table as TranslationObject);
  }
}
