import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { AuditLogEntry } from '../../../core/models/collaboration';
import { AuditService } from '../../../core/services/audit.service';
import { MATERIAL_IMPORTS } from '../../material';
import { LocalDateTimePipe } from '../../pipes/local-date-time.pipe';
import { TimeAgoPipe } from '../../pipes/time-ago.pipe';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';
import { formatUtcTooltip } from '../../../core/utils/date-time';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { PagedResult } from '../../../core/models/paged-result';

@Component({
  selector: 'crm-activity-log',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, LocalDateTimePipe, TimeAgoPipe, EnumLabelPipe, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="crm-stack">
      <h3 class="crm-section-title">{{ 'activity.title' | translate }}</h3>
      @if (!items().length) {
        <crm-empty-state titleKey="activity.emptyTitle" messageKey="activity.emptyMessage" icon="history" />
      } @else {
        <ol class="log">
          @for (entry of items(); track entry.id) {
            <li class="crm-card">
              <div>
                <strong>{{ entry.action | enumLabel: 'audit' }}</strong>
                <p>{{ entry.description }}</p>
                @if (entry.fromValue || entry.toValue) {
                  <p class="crm-muted">{{ entry.fromValue }} → {{ entry.toValue }}</p>
                }
              </div>
              <p class="crm-muted">
                {{ entry.actorName || entry.actorUserId }} · {{ entry.actorRoleCode }} ·
                <span [matTooltip]="utc(entry.occurredAtUtc) + ' · ' + (entry.occurredAtUtc | timeAgo)">
                  {{ entry.occurredAtUtc | localDateTime }}
                </span>
              </p>
            </li>
          }
        </ol>
      }
    </section>
  `,
  styles: `
    .log { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.75rem; }
    p { margin: 0.25rem 0 0; }
  `
})
export class ActivityLogComponent implements OnInit {
  private readonly api = inject(AuditService);
  readonly entityType = input<string>('');
  readonly entityId = input<string>('');
  readonly opportunityId = input<string>('');
  readonly items = signal<AuditLogEntry[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const opportunityId = this.opportunityId();
    const entityId = this.entityId();
    const entityType = this.entityType();

    // Prefer opportunity-scoped audit (available to all authenticated roles that can open the opp).
    // Global /audit requires CanViewAudit and used to 403-eject SL/Presales from approval pages.
    if (opportunityId) {
      this.api
        .forOpportunity(opportunityId, { page: 1, pageSize: 100, sortBy: 'occurredAtUtc', sortDir: 'desc' })
        .pipe(catchError(() => of([] as AuditLogEntry[])))
        .subscribe((res) => {
          let items = res;
          if (entityType) {
            items = items.filter((e) => e.entityType === entityType);
          }
          if (entityId) {
            items = items.filter((e) => e.entityId === entityId);
          }
          this.items.set(items);
        });
      return;
    }

    this.api
      .list({
        page: 1,
        pageSize: 100,
        sortBy: 'occurredAtUtc',
        sortDir: 'desc',
        entityType: entityType || undefined,
        entityId: entityId || undefined,
        opportunityId: opportunityId || undefined
      })
      .pipe(catchError(() => of([] as AuditLogEntry[])))
      .subscribe((res) => this.items.set(this.extractItems(res)));
  }

  private extractItems(res: AuditLogEntry[] | PagedResult<AuditLogEntry>): AuditLogEntry[] {
    return Array.isArray(res) ? res : res.items;
  }

  utc(value: string): string {
    return formatUtcTooltip(value);
  }
}
