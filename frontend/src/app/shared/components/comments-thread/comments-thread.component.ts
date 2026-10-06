import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { CollaborationPath } from '../../../core/models/enums';
import { Comment } from '../../../core/models/collaboration';
import { UserPickItem } from '../../../core/models/user';
import { CommentsService } from '../../../core/services/comments.service';
import { UsersService } from '../../../core/services/users.service';
import { ToastService } from '../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { UserAvatarComponent } from '../user-avatar/user-avatar.component';
import { formatUtcTooltip } from '../../../core/utils/date-time';

@Component({
  selector: 'crm-comments-thread',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    ReactiveFormsModule,
    LocalDateTimePipe,
    TimeAgoPipe,
    UserAvatarComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="crm-stack">
      <h3 class="crm-section-title">{{ 'comments.title' | translate }}</h3>
      <form class="crm-card crm-stack" (ngSubmit)="post()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'comments.body' | translate }}</mat-label>
          <textarea matInput rows="3" [formControl]="body" required></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'comments.mention' | translate }}</mat-label>
          <input matInput [formControl]="mentionQuery" [matAutocomplete]="auto" />
          <mat-autocomplete #auto="matAutocomplete" (optionSelected)="addMention($event.option.value)">
            @for (u of filteredUsers(); track u.id) {
              <mat-option [value]="u">{{ u.displayName }} ({{ u.email }})</mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>
        @if (mentions().length) {
          <p class="crm-muted">&#64; {{ mentionNames() }}</p>
        }
        <button mat-flat-button color="primary" type="submit" [disabled]="body.invalid">
          {{ 'comments.post' | translate }}
        </button>
      </form>
      @for (comment of roots(); track comment.id) {
        <article class="crm-card">
          <div class="crm-row">
            <crm-user-avatar [name]="comment.createdByName || ''" />
            <div>
              <strong>{{ comment.createdByName || comment.createdByUserId }}</strong>
              <span class="crm-muted" [matTooltip]="utc(comment.createdAtUtc) + ' · ' + (comment.createdAtUtc | timeAgo)">
                {{ comment.createdAtUtc | localDateTime }}
              </span>
              <p>{{ comment.isDeleted ? ('comments.deleted' | translate) : comment.body }}</p>
            </div>
          </div>
          @for (reply of comment.replies ?? []; track reply.id) {
            <div class="reply crm-row">
              <crm-user-avatar [name]="reply.createdByName || ''" />
              <div>
                <strong>{{ reply.createdByName || reply.createdByUserId }}</strong>
                <span class="crm-muted" [matTooltip]="utc(reply.createdAtUtc) + ' · ' + (reply.createdAtUtc | timeAgo)">
                  {{ reply.createdAtUtc | localDateTime }}
                </span>
                <p>{{ reply.isDeleted ? ('comments.deleted' | translate) : reply.body }}</p>
              </div>
            </div>
          }
          <button mat-button type="button" (click)="replyTo.set(comment.id)">{{ 'comments.reply' | translate }}</button>
          @if (replyTo() === comment.id) {
            <mat-form-field appearance="outline" class="full">
              <textarea matInput [formControl]="replyBody" rows="2"></textarea>
            </mat-form-field>
            <button mat-button type="button" (click)="postReply(comment)">{{ 'comments.post' | translate }}</button>
          }
        </article>
      }
    </section>
  `,
  styles: `
    .reply { margin-inline-start: 1.5rem; margin-top: 0.75rem; }
    .full { width: 100%; }
  `
})
export class CommentsThreadComponent implements OnInit {
  private readonly api = inject(CommentsService);
  private readonly usersApi = inject(UsersService);
  private readonly toast = inject(ToastService);
  readonly entityType = input.required<CollaborationPath>();
  readonly entityId = input.required<string>();
  readonly items = signal<Comment[]>([]);
  readonly users = signal<UserPickItem[]>([]);
  readonly mentions = signal<UserPickItem[]>([]);
  readonly replyTo = signal<string | null>(null);
  readonly mentionFilter = signal('');
  readonly body = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  readonly replyBody = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  readonly mentionQuery = new FormControl('', { nonNullable: true });
  readonly roots = computed(() => this.items().filter((c) => !c.parentCommentId));
  readonly filteredUsers = computed(() => {
    const q = this.mentionFilter().toLowerCase();
    return this.users().filter(
      (u) => !q || u.displayName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    );
  });
  readonly mentionNames = computed(() => this.mentions().map((u) => u.displayName).join(', '));

  constructor() {
    this.mentionQuery.valueChanges.subscribe((v) => this.mentionFilter.set(v));
  }

  ngOnInit(): void {
    this.reload();
    // Use pickable users (available to all authenticated roles). Admin /users list 403s for SL/AM.
    this.usersApi.pickable().subscribe({
      next: (res) => this.users.set(res),
      error: () => this.users.set([])
    });
  }

  utc(value: string): string {
    return formatUtcTooltip(value);
  }

  addMention(user: UserPickItem): void {
    if (!this.mentions().some((u) => u.id === user.id)) {
      this.mentions.update((list) => [...list, user]);
    }
    this.mentionQuery.setValue('');
  }

  reload(): void {
    this.api.list(this.entityType(), this.entityId()).subscribe((rows) => this.items.set(this.nest(rows)));
  }

  private nest(rows: Comment[]): Comment[] {
    const byParent = new Map<string, Comment[]>();
    for (const row of rows) {
      if (row.parentCommentId) {
        const list = byParent.get(row.parentCommentId) ?? [];
        list.push(row);
        byParent.set(row.parentCommentId, list);
      }
    }
    return rows
      .filter((r) => !r.parentCommentId)
      .map((r) => ({ ...r, replies: r.replies?.length ? r.replies : byParent.get(r.id) ?? [] }));
  }

  post(): void {
    if (this.body.invalid) {
      return;
    }
    this.api
      .create(this.entityType(), this.entityId(), this.body.value, null, this.mentions().map((u) => u.id))
      .subscribe(() => {
        this.toast.success('comments.added');
        this.body.reset('');
        this.mentions.set([]);
        this.reload();
      });
  }

  postReply(parent: Comment): void {
    if (this.replyBody.invalid) {
      return;
    }
    this.api.create(this.entityType(), this.entityId(), this.replyBody.value, parent.id).subscribe(() => {
      this.toast.success('comments.added');
      this.replyBody.reset('');
      this.replyTo.set(null);
      this.reload();
    });
  }
}
