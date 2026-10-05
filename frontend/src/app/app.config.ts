import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { catchError, firstValueFrom, of, switchMap } from 'rxjs';
import { routes } from './app.routes';
import { authInterceptor } from './core/http/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { loadingInterceptor } from './core/http/loading.interceptor';
import { retryInterceptor } from './core/http/retry.interceptor';
import { AuthService } from './core/auth/auth.service';
import { LookupService } from './core/services/lookup.service';
import { initStoredLanguage } from './core/layout/lang-switcher.component';
import { BundledTranslateLoader } from './core/i18n/bundled-translate.loader';
import { HumanizeMissingTranslationHandler } from './core/i18n/humanize-missing-translation.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideAnimationsAsync(),
    provideHttpClient(
      withInterceptors([loadingInterceptor, authInterceptor, retryInterceptor, errorInterceptor])
    ),
    provideNativeDateAdapter(),
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
    provideTranslateService({
      fallbackLang: 'en',
      loader: () => new BundledTranslateLoader(),
      missingTranslationHandler: () => new HumanizeMissingTranslationHandler()
    }),
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      const lookups = inject(LookupService);
      const i18n = inject(TranslateService);
      return firstValueFrom(
        initStoredLanguage(i18n).pipe(
          switchMap(() => auth.restoreSession()),
          switchMap(() => lookups.bootstrap().pipe(catchError(() => of(null))))
        )
      );
    })
  ]
};
