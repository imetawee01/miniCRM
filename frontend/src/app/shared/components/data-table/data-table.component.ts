import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { PageEvent } from '@angular/material/paginator';
import { MATERIAL_IMPORTS } from '../../material';
import { EmptyStateComponent } from '../empty-state/empty-state.component';

export interface DataTableColumn<T> {
  key: string;
  headerKey: string;
  cell?: (row: T) => string;
}

@Component({
  selector: 'crm-data-table',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!rows().length && !loading()) {
      <crm-empty-state
        [titleKey]="emptyTitleKey()"
        [messageKey]="emptyMessageKey()"
        [ctaKey]="emptyCtaKey()"
        [ctaLink]="emptyCtaLink()"
      />
    } @else {
      <div class="table-wrap" role="region" [attr.aria-label]="'common.table' | translate">
        <table>
          <thead>
            <tr>
              @for (col of columns(); track col.key) {
                <th scope="col">{{ col.headerKey | translate }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track $index) {
              <tr tabindex="0" (click)="rowClick.emit(row)" (keyup.enter)="rowClick.emit(row)">
                @for (col of columns(); track col.key) {
                  <td>{{ display(row, col) }}</td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="cards">
        @for (row of rows(); track $index) {
          <article class="crm-card" tabindex="0" (click)="rowClick.emit(row)" (keyup.enter)="rowClick.emit(row)">
            @for (col of columns(); track col.key) {
              <div class="card-row">
                <span class="card-row__label">{{ col.headerKey | translate }}</span>
                <strong class="card-row__value">{{ display(row, col) }}</strong>
              </div>
            }
          </article>
        }
      </div>
      <mat-paginator
        [length]="total()"
        [pageIndex]="page() - 1"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[10, 25, 50]"
        (page)="onPage($event)"
      />
    }
  `,
  styles: `
    :host { display: block; }

    .table-wrap {
      overflow: auto;
      background: var(--crm-surface-card);
      border-radius: var(--crm-radius);
      border: 1px solid var(--crm-border);
      box-shadow: var(--crm-shadow-sm);
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    th, td {
      text-align: start;
      padding: 0.9rem 1.15rem;
      border-bottom: 1px solid var(--crm-border);
      vertical-align: middle;
    }

    th {
      color: var(--crm-navy);
      font-size: 0.78rem;
      font-weight: 700;
      background: #fafbfc;
      white-space: nowrap;
    }

    td {
      color: var(--crm-ink);
    }

    tbody tr { cursor: pointer; }
    tbody tr:hover { background: var(--crm-blue-softer); }
    tbody tr:last-child td { border-bottom: 0; }

    .cards { display: none; }

    .card-row {
      display: flex;
      flex-direction: column;
      align-items: start;
      gap: 0.15rem;
      padding-block: 0.45rem;
    }

    .card-row + .card-row {
      border-top: 1px solid var(--crm-border);
    }

    .card-row__label {
      font-size: 0.72rem;
      font-weight: 600;
      color: var(--crm-muted);
    }

    .card-row__value {
      font-size: 0.95rem;
      font-weight: 600;
      color: var(--crm-navy);
      overflow-wrap: anywhere;
    }

    @media (max-width: 768px) {
      .table-wrap { display: none; }
      .cards {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }
    }
  `
})
export class DataTableComponent<T> {
  readonly columns = input.required<DataTableColumn<T>[]>();
  readonly rows = input<T[]>([]);
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(25);
  readonly loading = input(false);
  readonly emptyTitleKey = input('common.emptyTitle');
  readonly emptyMessageKey = input('common.emptyMessage');
  readonly emptyCtaKey = input('');
  readonly emptyCtaLink = input<string | string[]>('');
  readonly rowClick = output<T>();
  readonly pageChange = output<{ page: number; pageSize: number }>();

  display(row: T, col: DataTableColumn<T>): string {
    if (col.cell) {
      return col.cell(row);
    }
    const value = (row as Record<string, unknown>)[col.key];
    return value === null || value === undefined ? '' : String(value);
  }

  onPage(ev: PageEvent): void {
    this.pageChange.emit({ page: ev.pageIndex + 1, pageSize: ev.pageSize });
  }
}
