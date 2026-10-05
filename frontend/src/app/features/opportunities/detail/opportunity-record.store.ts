import { Injectable, computed, inject, signal } from '@angular/core';
import { OpportunityDetail, OpportunityJourney, OpportunityNextActions } from '../../../core/models/opportunity';
import { OpportunityService } from '../../../core/services/opportunity.service';

/**
 * Shared record store for the opportunity detail shell + tabs.
 * Loaded once by the detail page; tabs read from here instead of re-fetching.
 */
@Injectable()
export class OpportunityRecordStore {
  private readonly api = inject(OpportunityService);

  private readonly _detail = signal<OpportunityDetail | null>(null);
  private readonly _journey = signal<OpportunityJourney | null>(null);
  private readonly _nextActions = signal<OpportunityNextActions | null>(null);
  private readonly _loading = signal(false);
  private readonly _id = signal<string | null>(null);

  readonly detail = this._detail.asReadonly();
  readonly journey = this._journey.asReadonly();
  readonly nextActions = this._nextActions.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly id = this._id.asReadonly();
  readonly statusCode = computed(() => this._detail()?.statusCode ?? '');
  readonly stageCode = computed(() => this._detail()?.stageCode ?? '');

  load(id: string): void {
    this._id.set(id);
    this._loading.set(true);
    this.api.get(id).subscribe({
      next: (d: OpportunityDetail) => {
        this._detail.set(d);
        this._loading.set(false);
      },
      error: () => this._loading.set(false)
    });
    this.api.journey(id).subscribe({
      next: (j: OpportunityJourney) => this._journey.set(j),
      error: () => this._journey.set(null)
    });
    this.api.nextActions(id).subscribe({
      next: (a: OpportunityNextActions) => this._nextActions.set(a),
      error: () => this._nextActions.set(null)
    });
  }

  reload(): void {
    const id = this._id();
    if (id) this.load(id);
  }

  patchDetail(partial: Partial<OpportunityDetail>): void {
    const cur = this._detail();
    if (cur) this._detail.set({ ...cur, ...partial });
  }
}
