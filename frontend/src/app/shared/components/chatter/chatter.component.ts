import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ChatterItem, ChatterService } from '../../../core/services/chatter.service';
import { NotesService } from '../../../core/services/notes.service';
import { CommentsService } from '../../../core/services/comments.service';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-chatter',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="chatter crm-card">
      <header class="chatter__head">
        <h2>{{ 'chatter.title' | translate }}</h2>
        <div class="filters">
          @for (f of filters; track f.value) {
            <button
              mat-stroked-button
              type="button"
              [class.active]="filter() === f.value"
              (click)="setFilter(f.value)"
            >
              {{ f.key | translate }}
            </button>
          }
        </div>
      </header>

      <form class="composer" [formGroup]="form" (ngSubmit)="post()">
        <mat-button-toggle-group formControlName="mode" hideSingleSelectionIndicator>
          <mat-button-toggle value="note">{{ 'chatter.logNote' | translate }}</mat-button-toggle>
          <mat-button-toggle value="message">{{ 'chatter.sendMessage' | translate }}</mat-button-toggle>
        </mat-button-toggle-group>
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'chatter.body' | translate }}</mat-label>
          <textarea matInput rows="3" formControlName="body"></textarea>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || busy()">
          {{ 'common.save' | translate }}
        </button>
      </form>

      <ul class="feed">
        @for (item of items(); track item.id + item.kind) {
          <li class="feed-item" [attr.data-kind]="item.kind">
            <div class="feed-item__meta">
              <span class="kind">{{ ('chatter.kind.' + item.kind) | translate }}</span>
              <span class="author">{{ item.authorName || '—' }}</span>
              <span class="when">{{ item.occurredAtUtc | date: 'short' }}</span>
            </div>
            @if (item.title) {
              <strong>{{ item.title }}</strong>
            }
            <p>{{ item.body }}</p>
            @if (item.meta) {
              <small class="meta">{{ item.meta }}</small>
            }
          </li>
        } @empty {
          <li class="empty">{{ 'chatter.empty' | translate }}</li>
        }
      </ul>
    </section>
  `,
  styles: `
    .chatter__head { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
    .chatter__head h2 { margin: 0; font-size: 1.1rem; }
    .filters { display: flex; flex-wrap: wrap; gap: 0.35rem; }
    .filters .active { background: color-mix(in srgb, var(--crm-teal) 18%, transparent); }
    .composer { display: flex; flex-direction: column; gap: 0.75rem; margin-bottom: 1.25rem; }
    .full { width: 100%; }
    .feed { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.85rem; }
    .feed-item { padding: 0.75rem 0.9rem; border-radius: 12px; background: color-mix(in srgb, var(--crm-navy) 4%, white); border-inline-start: 3px solid var(--crm-teal); }
    .feed-item[data-kind='audit'] { border-inline-start-color: var(--crm-muted); }
    .feed-item[data-kind='email'] { border-inline-start-color: #0ea5e9; }
    .feed-item[data-kind='attachment'] { border-inline-start-color: #f59e0b; }
    .feed-item[data-kind='activity'] { border-inline-start-color: #8b5cf6; }
    .feed-item__meta { display: flex; flex-wrap: wrap; gap: 0.5rem 0.85rem; font-size: 0.75rem; color: var(--crm-muted); margin-bottom: 0.35rem; }
    .kind { font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; }
    .feed-item p { margin: 0.25rem 0 0; white-space: pre-wrap; }
    .meta { color: var(--crm-muted); }
    .empty { color: var(--crm-muted); padding: 1rem 0; }
    :host-context([dir='rtl']) .feed-item { border-inline-start-width: 3px; }
  `
})
export class ChatterComponent implements OnInit {
  private readonly api = inject(ChatterService);
  private readonly notes = inject(NotesService);
  private readonly comments = inject(CommentsService);

  readonly opportunityId = input.required<string>();
  readonly changed = output<void>();

  readonly items = signal<ChatterItem[]>([]);
  readonly filter = signal<string>('all');
  readonly busy = signal(false);
  readonly filters = [
    { value: 'all', key: 'chatter.filters.all' },
    { value: 'note', key: 'chatter.filters.notes' },
    { value: 'comment', key: 'chatter.filters.messages' },
    { value: 'audit', key: 'chatter.filters.audit' },
    { value: 'email', key: 'chatter.filters.emails' },
    { value: 'attachment', key: 'chatter.filters.files' },
    { value: 'activity', key: 'chatter.filters.activities' }
  ];
  readonly form = new FormGroup({
    mode: new FormControl<'note' | 'message'>('note', { nonNullable: true }),
    body: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2)] })
  });

  ngOnInit(): void {
    this.reload();
  }

  setFilter(value: string): void {
    this.filter.set(value);
    this.reload();
  }

  reload(): void {
    const kind = this.filter() === 'all' ? undefined : this.filter();
    this.api.feed(this.opportunityId(), kind).subscribe((items) => this.items.set(items));
  }

  post(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const { mode, body } = this.form.getRawValue();
    const id = this.opportunityId();
    const done = () => {
      this.busy.set(false);
      this.form.patchValue({ body: '' });
      this.reload();
      this.changed.emit();
    };
    if (mode === 'note') {
      this.notes.create('opportunities', id, body, 'Internal').subscribe({
        next: done,
        error: () => this.busy.set(false)
      });
    } else {
      this.comments.create('opportunities', id, body).subscribe({
        next: done,
        error: () => this.busy.set(false)
      });
    }
  }
}
