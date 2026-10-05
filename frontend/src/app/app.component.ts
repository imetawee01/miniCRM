import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { Dir } from '@angular/cdk/bidi';
import { TranslateService } from '@ngx-translate/core';
import { directionFor } from './core/utils/locale';

@Component({
  selector: 'crm-root',
  imports: [RouterOutlet, Dir],
  template: `<div class="app-root" [dir]="dir()"><router-outlet /></div>`,
  styles: `:host, .app-root { display: block; height: 100%; }`
})
export class AppComponent {
  private readonly i18n = inject(TranslateService);
  readonly dir = signal<'ltr' | 'rtl'>(directionFor(this.i18n.getCurrentLang()));

  constructor() {
    this.i18n.onLangChange.pipe(takeUntilDestroyed()).subscribe((e) => this.dir.set(directionFor(e.lang)));
  }
}
