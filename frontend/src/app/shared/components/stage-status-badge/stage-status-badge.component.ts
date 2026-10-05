import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { inject } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../material';
import { LookupService } from '../../../core/services/lookup.service';
import { resolveLabel } from '../../../core/utils/locale';

@Component({
  selector: 'crm-stage-status-badge',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="crm-badge" [class]="'crm-badge--' + tone()">
      <span>{{ stageLabel() }}</span>
      @if (statusLabel()) {
        <span aria-hidden="true"> · </span>
        <span>{{ statusLabel() }}</span>
      }
    </span>
  `,
  styles: `
    .crm-badge {
      display: inline-flex;
      align-items: center;
      padding: 0.15rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      background: #e8eef5;
      color: var(--crm-navy);
    }
    .crm-badge--terminal { background: #eceff1; color: #546e7a; }
    .crm-badge--success { background: #e8f5e9; color: #1b5e20; }
    .crm-badge--warn { background: #fff8e1; color: #e65100; }
    .crm-badge--danger { background: #ffebee; color: #b71c1c; }
  `
})
export class StageStatusBadgeComponent {
  private readonly lookups = inject(LookupService);
  private readonly i18n = inject(TranslateService);
  readonly stageCode = input<string>('');
  readonly statusCode = input<string>('');
  readonly stageNameEn = input<string>('');
  readonly stageNameAr = input<string>('');
  readonly statusNameEn = input<string>('');
  readonly statusNameAr = input<string>('');

  readonly stageLabel = computed(() =>
    resolveLabel(
      this.i18n,
      this.stageCode(),
      this.stageNameEn() || this.lookups.stageName(this.stageCode(), 'en'),
      this.stageNameAr() || this.lookups.stageName(this.stageCode(), 'ar'),
      ['stages']
    )
  );
  readonly statusLabel = computed(() =>
    resolveLabel(
      this.i18n,
      this.statusCode(),
      this.statusNameEn() || this.lookups.statusName(this.statusCode(), 'en'),
      this.statusNameAr() || this.lookups.statusName(this.statusCode(), 'ar'),
      ['statuses']
    )
  );
  readonly tone = computed(() => {
    const s = this.statusCode();
    if (['NOT_QUALIFIED', 'LOST', 'CANCELED'].includes(s)) return 'danger';
    if (['CONTRACT_SIGNED', 'WON', 'QUALIFIED', 'SUBMITTED'].includes(s)) return 'success';
    if (['HOLD', 'NOT_APPROVED'].includes(s)) return 'warn';
    if (['CONTRACT_SIGNED', 'NOT_QUALIFIED', 'LOST', 'CANCELED'].includes(s)) return 'terminal';
    return 'info';
  });
}
