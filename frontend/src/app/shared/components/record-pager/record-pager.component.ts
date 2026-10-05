import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-record-pager',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (ids().length > 1 && index() >= 0) {
      <div class="pager" role="navigation" [attr.aria-label]="'common.recordPager' | translate">
        <button mat-icon-button type="button" [disabled]="!hasPrev()" (click)="go(-1)" [attr.aria-label]="'common.prev' | translate">
          <mat-icon>chevron_left</mat-icon>
        </button>
        <span>{{ index() + 1 }} / {{ ids().length }}</span>
        <button mat-icon-button type="button" [disabled]="!hasNext()" (click)="go(1)" [attr.aria-label]="'common.next' | translate">
          <mat-icon>chevron_right</mat-icon>
        </button>
      </div>
    }
  `,
  styles: `
    .pager { display: inline-flex; align-items: center; gap: 0.15rem; font-size: 0.85rem; font-weight: 600; color: var(--crm-muted); }
    :host-context([dir='rtl']) mat-icon { transform: scaleX(-1); }
  `
})
export class RecordPagerComponent {
  private readonly router = inject(Router);
  readonly currentId = input.required<string>();
  /** Ordered ids from the list context (sessionStorage / query). */
  readonly ids = input<string[]>([]);

  readonly index = computed(() => this.ids().indexOf(this.currentId()));
  readonly hasPrev = computed(() => this.index() > 0);
  readonly hasNext = computed(() => this.index() >= 0 && this.index() < this.ids().length - 1);

  go(delta: number): void {
    const next = this.ids()[this.index() + delta];
    if (next) void this.router.navigate(['/opportunities', next]);
  }
}
