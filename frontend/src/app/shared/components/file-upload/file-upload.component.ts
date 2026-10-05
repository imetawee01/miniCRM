import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ATTACHMENT_CATEGORIES, AttachmentCategory } from '../../../core/models/enums';
import { allowedFile } from '../../../core/utils/validators';
import { formatFileSize } from '../../../core/utils/file-size';
import { MATERIAL_IMPORTS } from '../../material';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';

@Component({
  selector: 'crm-file-upload',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="submit()">
      <h3 class="crm-section-title">{{ 'files.upload' | translate }}</h3>
      <input
        type="file"
        accept=".pdf,.docx,.xlsx,.pptx,.vsdx,.png,.jpg,.jpeg,.zip,.msg"
        (change)="onFile($event)"
        [attr.aria-label]="'files.choose' | translate"
      />
      @if (fileError()) {
        <p class="error">{{ fileError() | translate }}</p>
      }
      @if (file()) {
        <p class="crm-muted">{{ file()!.name }} ({{ sizeLabel() }})</p>
      }
      <mat-form-field appearance="outline">
        <mat-label>{{ 'files.description' | translate }}</mat-label>
        <textarea matInput rows="2" formControlName="description" required></textarea>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>{{ 'files.category' | translate }}</mat-label>
        <mat-select formControlName="category">
          @for (cat of categories; track cat) {
            <mat-option [value]="cat">{{ cat | enumLabel }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <button
        mat-flat-button
        color="primary"
        type="submit"
        [disabled]="form.invalid || !file() || !!fileError()"
      >
        {{ 'files.uploadAction' | translate }}
      </button>
    </form>
  `,
  styles: `.error { color: var(--crm-danger); } input[type='file'] { margin-bottom: 0.5rem; }`
})
export class FileUploadComponent {
  readonly uploading = input(false);
  readonly uploaded = output<{ file: File; description: string; category: AttachmentCategory }>();
  readonly categories = ATTACHMENT_CATEGORIES;
  readonly file = signal<File | null>(null);
  readonly fileError = signal<string | null>(null);
  readonly form = new FormGroup({
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    category: new FormControl<AttachmentCategory>('Other', { nonNullable: true, validators: [Validators.required] })
  });
  readonly sizeLabel = computed(() => (this.file() ? formatFileSize(this.file()!.size) : ''));

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    this.file.set(selected);
    const err = allowedFile(selected);
    if (!selected) {
      this.fileError.set(null);
      return;
    }
    if (err?.['maxSize']) {
      this.fileError.set('files.tooLarge');
    } else if (err?.['fileType']) {
      this.fileError.set('files.badType');
    } else {
      this.fileError.set(null);
    }
  }

  submit(): void {
    const file = this.file();
    if (!file || this.form.invalid || this.fileError()) {
      return;
    }
    const { description, category } = this.form.getRawValue();
    this.uploaded.emit({ file, description, category });
  }

  reset(): void {
    this.file.set(null);
    this.fileError.set(null);
    this.form.reset({ description: '', category: 'Other' });
  }
}
