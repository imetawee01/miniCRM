import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-user-avatar',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="avatar" [matTooltip]="name()" [attr.aria-label]="name()">{{ initials() }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      flex: 0 0 auto;
      line-height: 0;
    }

    .avatar {
      width: 32px; height: 32px; border-radius: 50%; color: #fff;
      background: linear-gradient(135deg, var(--crm-teal), var(--crm-teal-dark));
      display: inline-grid; place-items: center; font-size: 0.75rem; font-weight: 700;
      letter-spacing: 0.02em; flex: 0 0 auto;
      box-shadow: 0 1px 3px rgba(25, 36, 68, 0.25);
    }
  `
})
export class UserAvatarComponent {
  readonly name = input('');
  readonly initials = computed(() =>
    this.name()
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? '')
      .join('')
  );
}
