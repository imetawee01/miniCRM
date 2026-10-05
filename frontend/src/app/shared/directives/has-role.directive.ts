import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { RoleCode } from '../../core/models/enums';
import { AuthStore } from '../../core/auth/auth.store';

@Directive({
  selector: '[crmHasRole]',
  standalone: true
})
export class HasRoleDirective {
  private readonly template = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private readonly store = inject(AuthStore);
  readonly crmHasRole = input.required<RoleCode | RoleCode[] | string | string[]>();

  constructor() {
    effect(() => {
      this.vcr.clear();
      if (this.store.hasRole(this.crmHasRole())) {
        this.vcr.createEmbeddedView(this.template);
      }
    });
  }
}
