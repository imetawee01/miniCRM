import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthStore } from './auth.store';

@Directive({
  selector: '[crmPermission]',
  standalone: true
})
export class PermissionDirective {
  private readonly template = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private readonly store = inject(AuthStore);
  readonly crmPermission = input.required<string | string[]>();

  constructor() {
    effect(() => {
      this.vcr.clear();
      if (this.store.hasPermission(this.crmPermission())) {
        this.vcr.createEmbeddedView(this.template);
      }
    });
  }
}
