import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MATERIAL_IMPORTS } from '../../material';
import { LookupService } from '../../../core/services/lookup.service';
import { resolveLabel } from '../../../core/utils/locale';

const TERMINAL_RED = new Set(['NOT_QUALIFIED', 'LOST', 'CANCELED']);
const HOLD = 'HOLD';

@Component({
  selector: 'crm-status-bar',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="status-bar" role="group" [attr.aria-label]="'opp.stageProgress' | translate">
      <div class="stages" role="list">
        @for (stage of stages(); track stage.code; let i = $index) {
          <div
            class="stage"
            role="listitem"
            [class.done]="i < currentIndex()"
            [class.current]="i === currentIndex() && !isTerminal()"
            [class.terminal]="i === currentIndex() && isTerminal()"
            [class.hold]="isHold()"
            [class.upcoming]="i > currentIndex()"
          >
            <span class="stage__label">{{ label(stage) }}</span>
          </div>
        }
      </div>
      @if (statusesInStage().length) {
        <div class="statuses" role="list">
          @for (st of statusesInStage(); track st.code) {
            <span
              class="status-chip"
              role="listitem"
              [class.active]="st.code === currentStatus()"
              [class.terminal]="st.isTerminal"
            >
              {{ statusLabel(st) }}
            </span>
          }
        </div>
      }
    </div>
  `,
  styles: `
    .status-bar { display: flex; flex-direction: column; gap: 0.6rem; margin: 0.75rem 0 0.25rem; }
    .stages {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 0.35rem;
    }
    .stage {
      position: relative;
      padding: 0.55rem 0.75rem;
      background: #e8eef5;
      color: var(--crm-muted);
      font-size: 0.78rem;
      font-weight: 600;
      clip-path: polygon(0 0, calc(100% - 10px) 0, 100% 50%, calc(100% - 10px) 100%, 0 100%, 10px 50%);
    }
    .stage:first-child { clip-path: polygon(0 0, calc(100% - 10px) 0, 100% 50%, calc(100% - 10px) 100%, 0 100%); padding-inline-start: 0.85rem; }
    .stage:last-child { clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%, 10px 50%); }
    .stage.done { background: var(--crm-teal); color: #fff; }
    .stage.current { background: var(--crm-navy); color: #fff; }
    .stage.terminal { background: var(--crm-danger); color: #fff; }
    .stage.hold.current { background: #b45309; color: #fff; }
    .statuses { display: flex; flex-wrap: wrap; gap: 0.35rem; }
    .status-chip {
      padding: 0.2rem 0.65rem;
      border-radius: var(--crm-radius-pill);
      background: #f1f5f9;
      color: var(--crm-muted);
      font-size: 0.72rem;
      font-weight: 600;
    }
    .status-chip.active { background: var(--crm-blue-soft); color: var(--crm-blue-strong); }
    .status-chip.terminal.active { background: #fee2e2; color: var(--crm-danger); }
    @media (max-width: 720px) {
      .stages { grid-template-columns: 1fr 1fr; }
      .stage, .stage:first-child, .stage:last-child { clip-path: none; border-radius: var(--crm-radius-sm); }
    }
  `
})
export class StatusBarComponent {
  private readonly lookups = inject(LookupService);
  private readonly i18n = inject(TranslateService);
  readonly currentStage = input<string>('');
  readonly currentStatus = input<string>('');

  readonly stages = computed(() => {
    const fromApi = this.lookups.stages();
    return [...fromApi].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
  });

  readonly currentIndex = computed(() => this.stages().findIndex((s) => s.code === this.currentStage()));
  readonly isTerminal = computed(() => TERMINAL_RED.has(this.currentStatus()));
  readonly isHold = computed(() => this.currentStatus() === HOLD);

  readonly statusesInStage = computed(() => {
    const stage = this.stages().find((s) => s.code === this.currentStage());
    if (!stage) return [];
    return this.lookups
      .statuses()
      .filter((s) => s.stageId === stage.id)
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));
  });

  label(stage: { nameEn: string; nameAr: string; code: string }): string {
    return resolveLabel(this.i18n, stage.code, stage.nameEn, stage.nameAr, ['stages']);
  }

  statusLabel(st: { nameEn: string; nameAr: string; code: string }): string {
    return resolveLabel(this.i18n, st.code, st.nameEn, st.nameAr, ['statuses']);
  }
}
