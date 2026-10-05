import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MATERIAL_IMPORTS } from '../../material';
import { LookupService } from '../../../core/services/lookup.service';
import { resolveLabel } from '../../../core/utils/locale';

const STAGE_ORDER = ['QUALIFICATION', 'RESPONSE_DEVELOPMENT', 'SUBMISSION', 'CONTRACTING'];

@Component({
  selector: 'crm-status-stepper',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
      <ol class="crm-stepper" [attr.aria-label]="'opp.stageProgress' | translate">
      @for (stage of stages(); track stage.code; let i = $index) {
        <li [class.current]="stage.code === currentStage()" [class.done]="i < currentIndex()">
          <span class="dot">{{ i + 1 }}</span>
          <span>{{ label(stage) }}</span>
        </li>
      }
    </ol>
  `,
  styles: `
    .crm-stepper {
      display: flex; gap: 0.5rem; list-style: none; padding: 0; margin: 0 0 1rem; flex-wrap: wrap;
    }
    li {
      display: flex; align-items: center; gap: 0.4rem; color: var(--crm-muted); font-size: 0.85rem;
      flex: 1; min-width: 120px;
    }
    .dot {
      width: 24px; height: 24px; border-radius: 50%; display: grid; place-items: center;
      background: var(--crm-border); font-size: 0.75rem; font-weight: 700;
    }
    li.done .dot, li.current .dot { background: var(--crm-teal); color: #fff; }
    li.current { color: var(--crm-navy); font-weight: 600; }
  `
})
export class StatusStepperComponent {
  private readonly lookups = inject(LookupService);
  private readonly i18n = inject(TranslateService);
  readonly currentStage = input<string>('');
  readonly stages = computed(() => {
    const fromApi = this.lookups.stages();
    if (fromApi.length) {
      return [...fromApi].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
    }
    return STAGE_ORDER.map((code, i) => ({
      id: code,
      code,
      nameEn: code,
      nameAr: code,
      sortOrder: i
    }));
  });
  readonly currentIndex = computed(() =>
    this.stages().findIndex((s) => s.code === this.currentStage())
  );

  label(stage: { nameEn: string; nameAr: string; code: string }): string {
    return resolveLabel(this.i18n, stage.code, stage.nameEn, stage.nameAr, ['stages']);
  }
}
