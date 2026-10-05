import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AttachmentListComponent } from '../../../../shared/components/attachment-list/attachment-list.component';

@Component({
  selector: 'crm-attachments-tab',
  standalone: true,
  imports: [AttachmentListComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<crm-attachment-list entityType="opportunities" [entityId]="id" />`
})
export class AttachmentsTabComponent {
  readonly id = inject(ActivatedRoute).parent?.snapshot.paramMap.get('id') ?? '';
}
