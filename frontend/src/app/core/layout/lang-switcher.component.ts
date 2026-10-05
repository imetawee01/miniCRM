import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { AppLang, LANG_STORAGE_KEY, applyDocumentLanguage } from '../utils/locale';

@Component({
  selector: 'crm-lang-switcher',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      class="lang"
      mat-button
      type="button"
      [matMenuTriggerFor]="menu"
      [attr.aria-label]="'nav.language' | translate"
    >
      <mat-icon>language</mat-icon>
      {{ current() }}
    </button>
    <mat-menu #menu="matMenu">
      <button mat-menu-item type="button" (click)="setLang('en')">EN</button>
      <button mat-menu-item type="button" (click)="setLang('ar')">AR</button>
    </mat-menu>
  `,
  styles: `
    .lang {
      color: var(--crm-muted);
      font-weight: 600;
    }

    .lang:hover {
      color: var(--crm-teal-dark);
      background: var(--crm-blue-soft);
    }

    .lang mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }
  `
})
export class LangSwitcherComponent {
  private readonly i18n = inject(TranslateService);

  current(): string {
    return (this.i18n.getCurrentLang() ?? 'en').toUpperCase();
  }

  setLang(lang: AppLang): void {
    this.i18n.use(lang);
    localStorage.setItem(LANG_STORAGE_KEY, lang);
    applyDocumentLanguage(lang);
  }
}

export function initStoredLanguage(i18n: TranslateService) {
  const lang = (localStorage.getItem(LANG_STORAGE_KEY) as AppLang | null) ?? 'en';
  applyDocumentLanguage(lang);
  i18n.addLangs(['en', 'ar']);
  i18n.setFallbackLang('en');
  return i18n.use(lang);
}
