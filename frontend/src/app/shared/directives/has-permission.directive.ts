import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthStore } from '../../core/auth/auth.store';

@Directive({
  selector: '[crmHasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private readonly template = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private readonly store = inject(AuthStore);
  readonly crmHasPermission = input.required<string | string[]>();

  constructor() {
    effect(() => {
      this.vcr.clear();
      if (this.store.hasPermission(this.crmHasPermission())) {
        this.vcr.createEmbeddedView(this.template);
      }
    });
  }
}
