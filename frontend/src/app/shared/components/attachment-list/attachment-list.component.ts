import { ChangeDetectionStrategy, Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CollaborationPath } from '../../../core/models/enums';
import { Attachment } from '../../../core/models/collaboration';
import { AttachmentsService } from '../../../core/services/attachments.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { triggerDownload } from '../../../core/utils/browser';
import { formatFileSize } from '../../../core/utils/file-size';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';
import { FileUploadComponent } from '../file-upload/file-upload.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { formatUtcTooltip } from '../../../core/utils/date-time';

@Component({
  selector: 'crm-attachment-list',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LocalDateTimePipe, TimeAgoPipe, EnumLabelPipe, FileUploadComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="crm-stack">
      <h3 class="crm-section-title">{{ 'files.title' | translate }}</h3>
      <crm-file-upload (uploaded)="upload($event)" />
      @if (!items().length) {
        <crm-empty-state titleKey="files.emptyTitle" messageKey="files.emptyMessage" icon="attach_file" />
      } @else {
        <ul class="list">
          @for (item of items(); track item.id) {
            <li class="crm-card">
              <div>
                <strong>{{ item.fileName }}</strong>
                <p>{{ item.description }}</p>
                <p class="crm-muted">
                  {{ item.category | enumLabel }} · {{ size(item.sizeBytes) }} ·
                  {{ item.uploadedByName || item.uploadedByUserId }} ·
                  <span [matTooltip]="utc(item.uploadedAtUtc) + ' · ' + (item.uploadedAtUtc | timeAgo)">
                    {{ item.uploadedAtUtc | localDateTime }}
                  </span>
                </p>
              </div>
              <div class="crm-actions">
                <button mat-icon-button type="button" [attr.aria-label]="'files.download' | translate" (click)="download(item)">
                  <mat-icon>download</mat-icon>
                </button>
                <button mat-icon-button type="button" [attr.aria-label]="'common.delete' | translate" (click)="remove(item)">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    .list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.75rem; }
    li { display: flex; justify-content: space-between; gap: 1rem; align-items: flex-start; }
    p { margin: 0.25rem 0 0; }
  `
})
export class AttachmentListComponent implements OnInit {
  private readonly api = inject(AttachmentsService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly entityType = input.required<CollaborationPath>();
  readonly entityId = input.required<string>();
  readonly changed = output<void>();
  readonly items = signal<Attachment[]>([]);

  reload(): void {
    this.api.list(this.entityType(), this.entityId()).subscribe((rows) => this.items.set(rows));
  }

  ngOnInit(): void {
    this.reload();
  }

  size(bytes: number): string {
    return formatFileSize(bytes);
  }

  utc(value: string): string {
    return formatUtcTooltip(value);
  }

  upload(ev: { file: File; description: string; category: Attachment['category'] }): void {
    this.api.upload(this.entityType(), this.entityId(), ev.file, ev.description, ev.category).subscribe({
      next: () => {
        this.toast.success('files.uploaded');
        this.reload();
        this.changed.emit();
      }
    });
  }

  download(item: Attachment): void {
    this.api.download(item.id).subscribe((blob) => triggerDownload(blob, item.fileName));
  }

  remove(item: Attachment): void {
    this.confirm
      .confirm({ titleKey: 'files.deleteTitle', messageKey: 'files.deleteMessage', warn: true })
      .subscribe((ok) => {
        if (!ok) {
          return;
        }
        this.api.delete(item.id).subscribe(() => {
          this.toast.success('files.deleted');
          this.reload();
          this.changed.emit();
        });
      });
  }
}
