import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CollaborationPath, NOTE_VISIBILITIES, NoteVisibility } from '../../../core/models/enums';
import { Note } from '../../../core/models/collaboration';
import { NotesService } from '../../../core/services/notes.service';
import { AuthStore } from '../../../core/auth/auth.store';
import { ToastService } from '../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';
import { formatUtcTooltip } from '../../../core/utils/date-time';

const EDIT_WINDOW_MS = 15 * 60 * 1000;

@Component({
  selector: 'crm-notes-panel',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, LocalDateTimePipe, TimeAgoPipe, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="crm-stack">
      <h3 class="crm-section-title">{{ 'notes.title' | translate }}</h3>
      <form [formGroup]="form" (ngSubmit)="add()" class="crm-card crm-stack">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'notes.body' | translate }}</mat-label>
          <textarea matInput rows="3" formControlName="body" required></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'notes.visibility' | translate }}</mat-label>
          <mat-select formControlName="visibility">
            @for (v of visibilities; track v) {
              <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          {{ 'notes.add' | translate }}
        </button>
      </form>
      <ul class="list">
        @for (note of items(); track note.id) {
          <li class="crm-card">
            <p>{{ note.body }}</p>
            <p class="crm-muted">
              {{ note.createdByName || note.createdByUserId }} · {{ note.visibility | enumLabel }} ·
              <span [matTooltip]="utc(note.createdAtUtc) + ' · ' + (note.createdAtUtc | timeAgo)">
                {{ note.createdAtUtc | localDateTime }}
              </span>
            </p>
            @if (canEdit(note)) {
              <button mat-button type="button" (click)="startEdit(note)">{{ 'common.edit' | translate }}</button>
            }
            @if (editingId() === note.id) {
              <mat-form-field appearance="outline" class="full">
                <textarea matInput [formControl]="editBody" rows="2"></textarea>
              </mat-form-field>
              <button mat-button type="button" (click)="saveEdit(note)">{{ 'common.save' | translate }}</button>
            }
          </li>
        }
      </ul>
    </section>
  `,
  styles: `
    .list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.75rem; }
    .full { width: 100%; }
  `
})
export class NotesPanelComponent implements OnInit {
  private readonly api = inject(NotesService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);
  readonly entityType = input.required<CollaborationPath>();
  readonly entityId = input.required<string>();
  readonly visibilities = NOTE_VISIBILITIES;
  readonly items = signal<Note[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly editBody = new FormControl('', { nonNullable: true });
  readonly form = new FormGroup({
    body: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    visibility: new FormControl<NoteVisibility>('Internal', { nonNullable: true })
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.list(this.entityType(), this.entityId()).subscribe((rows) => this.items.set(rows));
  }

  utc(value: string): string {
    return formatUtcTooltip(value);
  }

  canEdit(note: Note): boolean {
    const user = this.auth.user();
    if (!user || user.id !== note.createdByUserId) {
      return false;
    }
    return Date.now() - new Date(note.createdAtUtc).getTime() <= EDIT_WINDOW_MS;
  }

  add(): void {
    if (this.form.invalid) {
      return;
    }
    const { body, visibility } = this.form.getRawValue();
    this.api.create(this.entityType(), this.entityId(), body, visibility).subscribe(() => {
      this.toast.success('notes.added');
      this.form.reset({ body: '', visibility: 'Internal' });
      this.reload();
    });
  }

  startEdit(note: Note): void {
    this.editingId.set(note.id);
    this.editBody.setValue(note.body);
  }

  saveEdit(note: Note): void {
    this.api.update(note.id, this.editBody.value, note.visibility).subscribe(() => {
      this.toast.success('notes.updated');
      this.editingId.set(null);
      this.reload();
    });
  }
}
