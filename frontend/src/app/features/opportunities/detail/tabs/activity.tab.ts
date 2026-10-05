import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActivityLogComponent } from '../../../../shared/components/activity-log/activity-log.component';

@Component({
  selector: 'crm-activity-tab',
  standalone: true,
  imports: [ActivityLogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<crm-activity-log [opportunityId]="id" />`
})
export class ActivityTabComponent {
  readonly id = inject(ActivatedRoute).parent?.snapshot.paramMap.get('id') ?? '';
}
