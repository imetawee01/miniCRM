import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChatterComponent } from '../../../../shared/components/chatter/chatter.component';
import { ActivitySchedulerComponent } from '../../../../shared/components/activity-scheduler/activity-scheduler.component';
import { ActivityLogComponent } from '../../../../shared/components/activity-log/activity-log.component';
import { MATERIAL_IMPORTS } from '../../../../shared/material';

@Component({
  selector: 'crm-discussion-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ChatterComponent, ActivitySchedulerComponent, ActivityLogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      <crm-chatter [opportunityId]="id" />
      <crm-activity-scheduler [opportunityId]="id" />
      <details class="audit-wrap">
        <summary>{{ 'activity.title' | translate }}</summary>
        <crm-activity-log [opportunityId]="id" />
      </details>
    </div>
  `,
  styles: `
    .audit-wrap { background: transparent; }
    .audit-wrap summary {
      cursor: pointer; font-weight: 600; color: var(--crm-muted); padding: 0.5rem 0;
    }
  `
})
export class DiscussionTabComponent {
  readonly id = inject(ActivatedRoute).parent?.snapshot.paramMap.get('id') ?? '';
}
