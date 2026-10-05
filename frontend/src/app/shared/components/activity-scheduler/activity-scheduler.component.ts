import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivityItem, ActivityType, ChatterService } from '../../../core/services/chatter.service';
import { UsersService } from '../../../core/services/users.service';
import { UserPickItem } from '../../../core/models/user';
import { AuthStore } from '../../../core/auth/auth.store';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-activity-scheduler',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="crm-card crm-stack">
      <h2>{{ 'activity.scheduleTitle' | translate }}</h2>
      <form class="form" [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'activity.type' | translate }}</mat-label>
          <mat-select formControlName="type">
            <mat-option value="Todo">{{ 'activity.types.Todo' | translate }}</mat-option>
            <mat-option value="Call">{{ 'activity.types.Call' | translate }}</mat-option>
            <mat-option value="Meeting">{{ 'activity.types.Meeting' | translate }}</mat-option>
            <mat-option value="FollowUp">{{ 'activity.types.FollowUp' | translate }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="grow">
          <mat-label>{{ 'activity.summary' | translate }}</mat-label>
          <input matInput formControlName="summary" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'activity.due' | translate }}</mat-label>
          <input matInput type="datetime-local" formControlName="due" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'activity.assignee' | translate }}</mat-label>
          <mat-select formControlName="assignedUserId">
            @for (u of users(); track u.id) {
              <mat-option [value]="u.id">{{ u.displayName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || busy()">
          {{ 'activity.schedule' | translate }}
        </button>
      </form>

      <ul class="list">
        @for (a of items(); track a.id) {
          <li [class.overdue]="isOverdue(a)" [class.done]="!!a.doneAtUtc">
            <div>
              <strong>{{ typeLabel(a.type) }}</strong> · {{ a.summary }}
              <div class="meta">{{ a.assignedUserName }} · {{ a.dueAtUtc | date: 'short' }}</div>
            </div>
            @if (!a.doneAtUtc) {
              <button mat-stroked-button type="button" (click)="complete(a.id)">{{ 'activity.markDone' | translate }}</button>
            } @else {
              <span class="done-badge">{{ 'activity.done' | translate }}</span>
            }
          </li>
        } @empty {
          <li class="empty">{{ 'activity.emptySchedule' | translate }}</li>
        }
      </ul>
    </section>
  `,
  styles: `
    h2 { margin: 0; font-size: 1.05rem; }
    .form { display: flex; flex-wrap: wrap; gap: 0.65rem; align-items: center; }
    .grow { flex: 1 1 14rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.55rem; }
    .list li { display: flex; justify-content: space-between; gap: 0.75rem; align-items: center; padding: 0.65rem 0.75rem; border-radius: 10px; background: color-mix(in srgb, var(--crm-navy) 4%, white); }
    .list li.overdue { border-inline-start: 3px solid var(--crm-danger); }
    .list li.done { opacity: 0.65; }
    .meta { font-size: 0.78rem; color: var(--crm-muted); }
    .done-badge { font-size: 0.75rem; font-weight: 600; color: var(--crm-teal-dark); }
    .empty { color: var(--crm-muted); }
  `
})
export class ActivitySchedulerComponent implements OnInit {
  private readonly api = inject(ChatterService);
  private readonly usersApi = inject(UsersService);
  private readonly auth = inject(AuthStore);

  readonly opportunityId = input.required<string>();
  readonly changed = output<void>();

  readonly items = signal<ActivityItem[]>([]);
  readonly users = signal<UserPickItem[]>([]);
  readonly busy = signal(false);
  readonly form = new FormGroup({
    type: new FormControl<ActivityType>('Todo', { nonNullable: true }),
    summary: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    due: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    assignedUserId: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  ngOnInit(): void {
    this.usersApi.pickable().subscribe((u) => {
      this.users.set(u);
      const me = this.auth.user()?.id;
      if (me) this.form.patchValue({ assignedUserId: me });
    });
    const due = new Date();
    due.setDate(due.getDate() + 1);
    due.setMinutes(0, 0, 0);
    this.form.patchValue({ due: due.toISOString().slice(0, 16) });
    this.reload();
  }

  reload(): void {
    this.api.activities(this.opportunityId()).subscribe((rows) => this.items.set(rows));
  }

  save(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.api
      .createActivity(this.opportunityId(), {
        type: v.type,
        summary: v.summary,
        dueAtUtc: new Date(v.due).toISOString(),
        assignedUserId: v.assignedUserId
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.form.patchValue({ summary: '' });
          this.reload();
          this.changed.emit();
        },
        error: () => this.busy.set(false)
      });
  }

  complete(id: string): void {
    this.api.completeActivity(id).subscribe(() => {
      this.reload();
      this.changed.emit();
    });
  }

  isOverdue(a: ActivityItem): boolean {
    return !a.doneAtUtc && new Date(a.dueAtUtc).getTime() < Date.now();
  }

  typeLabel(type: ActivityType | number): string {
    const map: Record<number, ActivityType> = { 1: 'Call', 2: 'Meeting', 3: 'Todo', 4: 'FollowUp' };
    const key = typeof type === 'number' ? map[type] ?? 'Todo' : type;
    return key;
  }
}
