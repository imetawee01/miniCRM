import { ChangeDetectionStrategy, Component, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OpportunityDetail } from '../../../../core/models/opportunity';
import { OpportunityService } from '../../../../core/services/opportunity.service';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { SarPipe } from '../../../../shared/pipes/sar.pipe';
import { EnumLabelPipe } from '../../../../shared/pipes/enum-label.pipe';
import { DeadlineChipComponent } from '../../../../shared/components/deadline-chip/deadline-chip.component';
import { LocalDateTimePipe } from '../../../../shared/pipes/local-date-time.pipe';
import { OpportunityRecordStore } from '../opportunity-record.store';

@Component({
  selector: 'crm-overview-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, SarPipe, EnumLabelPipe, DeadlineChipComponent, LocalDateTimePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (opp(); as o) {
      <div class="crm-grid crm-grid-2">
        <section class="crm-card">
          <h3 class="crm-section-title">{{ 'opp.overview' | translate }}</h3>
          <p>{{ 'opp.customer' | translate }}: {{ o.customerName }}</p>
          <p>{{ 'opp.sourceChannel' | translate }}: {{ o.sourceChannel | enumLabel }}</p>
          <p>{{ 'opp.submissionTheme' | translate }}: {{ o.submissionTheme | enumLabel }}</p>
          <p>{{ 'opp.expectedValue' | translate }}: {{ o.expectedValueSar | sar }}</p>
          <p>{{ 'opp.createdAt' | translate }}: {{ o.createdAtUtc | localDateTime }}</p>
          @if (o.scopeBrief) {
            <p>{{ 'opp.scopeBrief' | translate }}: {{ o.scopeBrief }}</p>
          }
        </section>
        <section class="crm-card">
          <h3 class="crm-section-title">{{ 'deadlines.title' | translate }}</h3>
          <crm-deadline-chip [date]="o.deadlines?.qualificationDeadline" labelKey="deadlines.qualification" />
          <crm-deadline-chip [date]="o.deadlines?.inquiriesDeadline" labelKey="deadlines.inquiries" />
          <crm-deadline-chip [date]="o.deadlines?.estimatedCostDeadline" labelKey="deadlines.estimatedCost" />
          <crm-deadline-chip [date]="o.deadlines?.internalDeadline" labelKey="deadlines.internal" />
          <crm-deadline-chip [date]="o.deadlines?.submissionDeadline" labelKey="deadlines.submission" />
        </section>
      </div>
    }
  `
})
export class OverviewTabComponent implements OnInit {
  private readonly api = inject(OpportunityService);
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(OpportunityRecordStore, { optional: true });
  readonly opp = signal<OpportunityDetail | null>(null);

  constructor() {
    effect(() => {
      const d = this.store?.detail();
      if (d) this.opp.set(d);
    });
  }

  ngOnInit(): void {
    if (this.store) return;
    const id = this.route.parent?.snapshot.paramMap.get('id');
    if (id) this.api.get(id).subscribe((o) => this.opp.set(o));
  }
}
