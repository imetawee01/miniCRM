import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { OpportunityListItem, OpportunityListQuery } from '../../../core/models/opportunity';
import { OpportunityService } from '../../../core/services/opportunity.service';
import { LookupService } from '../../../core/services/lookup.service';
import { ToastService } from '../../../core/services/toast.service';
import { WorkflowActionService } from '../../../core/services/workflow-action.service';
import { MetaService } from '../../../core/query/meta.service';
import {
  DomainNode,
  MetaField,
  OpportunityViewDefinition,
  SavedView,
  encodeDomain,
  facetLeaf
} from '../../../core/query/domain.models';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, DataTableColumn } from '../../../shared/components/data-table/data-table.component';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { triggerDownload } from '../../../core/utils/browser';
import { ExportWizardDialogComponent } from './export-wizard.dialog';
import {
  KanbanMoveDialogComponent,
  KanbanMoveDialogResult
} from './kanban-move.dialog';
interface Preset {
  id: string;
  labelKey: string;
  apply: () => Partial<OpportunityListQuery> & { filter?: string; preset?: string };
}

const ALL_COLUMN_KEYS = [
  'opportunityNumber',
  'name',
  'customerName',
  'stageNameEn',
  'statusNameEn',
  'pendingUserName',
  'expectedValueSar',
  'submissionDeadline',
  'ownerDisplayName',
  'builderDisplayName'
] as const;

@Component({
  selector: 'crm-opportunity-list-page',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    ReactiveFormsModule,
    DecimalPipe,
    DragDropModule,
    PageHeaderComponent,
    DataTableComponent,
    HasRoleDirective,
    RouterLink
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="opp.listTitle">
        <ng-container *crmHasRole="['AM', 'BIDS_PRESALES', 'ADMIN']">
          <a mat-flat-button color="primary" routerLink="/opportunities/new">{{ 'opp.create' | translate }}</a>
        </ng-container>
      </crm-page-header>

      <div class="toolbar crm-card">
        <mat-button-toggle-group [value]="viewMode()" (change)="setView($event.value)">
          <mat-button-toggle value="list">{{ 'opp.viewList' | translate }}</mat-button-toggle>
          <mat-button-toggle value="kanban">{{ 'opp.viewKanban' | translate }}</mat-button-toggle>
          <mat-button-toggle value="calendar">{{ 'opp.viewCalendar' | translate }}</mat-button-toggle>
        </mat-button-toggle-group>

        <form class="search-row" [formGroup]="filters" (ngSubmit)="apply()">
          <mat-form-field appearance="outline" class="search-field" subscriptSizing="dynamic">
            <mat-label>{{ 'common.search' | translate }}</mat-label>
            <input matInput formControlName="search" [matAutocomplete]="auto" (keydown.enter)="onSearchEnter($event)" />
            <mat-autocomplete #auto="matAutocomplete" (optionSelected)="onFacet($event.option.value)">
              @for (opt of facetOptions(); track opt.id) {
                <mat-option [value]="opt">{{ opt.label }}</mat-option>
              }
            </mat-autocomplete>
          </mat-form-field>
          <button mat-stroked-button type="submit">{{ 'common.filter' | translate }}</button>
          <button mat-button type="button" [matMenuTriggerFor]="colMenu">{{ 'opp.columns' | translate }}</button>
          <button mat-button type="button" [matMenuTriggerFor]="viewMenu">{{ 'opp.favorites' | translate }}</button>
          <button mat-button type="button" (click)="openExport()">{{ 'common.export' | translate }}</button>
        </form>

        <div class="presets">
          @for (p of presets; track p.id) {
            <button
              mat-stroked-button
              type="button"
              class="preset"
              [class.preset--active]="activePreset() === p.id"
              (click)="applyPreset(p)"
            >
              {{ p.labelKey | translate }}
            </button>
          }
        </div>

        @if (facetChips().length) {
          <div class="chips">
            @for (c of facetChips(); track c.key) {
              <mat-chip (removed)="removeFacet(c.key)">
                {{ c.label }}
                <button matChipRemove type="button"><mat-icon>cancel</mat-icon></button>
              </mat-chip>
            }
          </div>
        }
      </div>

      <mat-menu #colMenu="matMenu">
        @for (col of columnDefs; track col.key) {
          <button mat-menu-item type="button" (click)="$event.stopPropagation(); toggleColumn(col.key)">
            <mat-checkbox [checked]="visibleColumns().includes(col.key)" />
            {{ col.headerKey | translate }}
          </button>
        }
      </mat-menu>

      <mat-menu #viewMenu="matMenu">
        <button mat-menu-item type="button" (click)="saveCurrentView()">{{ 'opp.saveView' | translate }}</button>
        @for (v of savedViews(); track v.id) {
          <button mat-menu-item type="button" (click)="loadView(v)">{{ v.name }}</button>
        }
      </mat-menu>

      @if (viewMode() === 'list') {
        <crm-data-table
          [columns]="activeColumns()"
          [rows]="rows()"
          [total]="total()"
          [page]="page()"
          [pageSize]="pageSize()"
          emptyTitleKey="opp.emptyTitle"
          emptyMessageKey="opp.emptyMessage"
          emptyCtaKey="opp.create"
          emptyCtaLink="/opportunities/new"
          (rowClick)="open($event)"
          (pageChange)="onPage($event)"
        />
      } @else if (viewMode() === 'kanban') {
        <div class="kanban" cdkDropListGroup>
          @for (col of kanbanColumns(); track col.stageId) {
            <section
              class="kanban__col"
              cdkDropList
              [cdkDropListData]="col.items"
              [id]="col.stageId"
              (cdkDropListDropped)="onKanbanDrop($event, col.stageId)"
            >
              <header>
                <strong>{{ col.title }}</strong>
                <span>{{ col.items.length }}</span>
              </header>
              <div class="kanban__cards">
                @for (item of col.items; track item.id) {
                  <button
                    type="button"
                    class="kanban__card"
                    cdkDrag
                    [cdkDragData]="item"
                    [cdkDragDisabled]="item.isClosed"
                    (cdkDragStarted)="onKanbanDragStart()"
                    (click)="open(item)"
                  >
                    <div
                      class="kanban__drag"
                      cdkDragHandle
                      [matTooltip]="'opp.kanbanDragHint' | translate"
                      (click)="$event.stopPropagation()"
                    >
                      <mat-icon>drag_indicator</mat-icon>
                    </div>
                    <span class="kanban__num">{{ item.opportunityNumber }}</span>
                    <strong>{{ item.name }}</strong>
                    <span>{{ lang() === 'ar' ? item.customerNameAr || item.customerName : item.customerName }}</span>
                    <span class="kanban__val">{{ item.expectedValueSar | number }} SAR</span>
                    <span class="kanban__status">{{ lang() === 'ar' ? item.statusNameAr : item.statusNameEn }}</span>
                  </button>
                }
              </div>
            </section>
          }
        </div>
      } @else {
        <div class="cal crm-card">
          <div class="cal__nav">
            <button mat-icon-button type="button" (click)="shiftMonth(-1)" [attr.aria-label]="'opp.calendarPrev' | translate">
              <mat-icon>chevron_left</mat-icon>
            </button>
            <strong>{{ calendarTitle() }}</strong>
            <button mat-icon-button type="button" (click)="shiftMonth(1)" [attr.aria-label]="'opp.calendarNext' | translate">
              <mat-icon>chevron_right</mat-icon>
            </button>
            <button mat-button type="button" (click)="goThisMonth()">{{ 'common.today' | translate }}</button>
          </div>
          <div class="cal__weekdays">
            @for (d of weekdayLabels; track d) {
              <span>{{ d }}</span>
            }
          </div>
          <div class="cal__grid">
            @for (cell of calendarCells(); track cell.key) {
              <div class="cal__day" [class.cal__day--muted]="!cell.inMonth" [class.cal__day--today]="cell.isToday">
                <span class="cal__num">{{ cell.day }}</span>
                <div class="cal__events">
                  @for (ev of cell.events; track ev.id + ev.kind) {
                    <button
                      type="button"
                      class="cal__ev"
                      [class.cal__ev--sub]="ev.kind === 'submission'"
                      [class.cal__ev--int]="ev.kind === 'internal'"
                      (click)="open(ev.item)"
                      [title]="ev.title"
                    >
                      {{ ev.label }}
                    </button>
                  }
                </div>
              </div>
            }
          </div>
          @if (!calendarHasEvents()) {
            <p class="cal__empty">{{ 'opp.calendarNoDeadline' | translate }}</p>
          }
        </div>
      }
    </div>
  `,
  styles: `
    .toolbar {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      padding: 1rem;
    }
    .search-row {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
    }
    .search-field {
      flex: 1 1 240px;
      min-width: 200px;
    }
    .presets {
      display: flex;
      flex-wrap: wrap;
      gap: 0.4rem;
    }
    .preset--active {
      background: color-mix(in srgb, var(--crm-teal, #0d9488) 18%, transparent);
    }
    .chips {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
    }
    .kanban {
      display: grid;
      grid-auto-flow: column;
      grid-auto-columns: minmax(240px, 1fr);
      gap: 0.75rem;
      overflow-x: auto;
      padding-bottom: 0.5rem;
    }
    .kanban__col {
      background: var(--crm-surface-card, #fff);
      border: 1px solid var(--crm-border, #e5e7eb);
      border-radius: var(--crm-radius, 8px);
      min-height: 320px;
      display: flex;
      flex-direction: column;
    }
    .kanban__col header {
      display: flex;
      justify-content: space-between;
      padding: 0.65rem 0.75rem;
      border-bottom: 1px solid var(--crm-border, #e5e7eb);
      font-size: 0.85rem;
    }
    .kanban__cards {
      padding: 0.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      overflow-y: auto;
      max-height: 70vh;
    }
    .kanban__card {
      text-align: start;
      border: 1px solid var(--crm-border, #e5e7eb);
      border-radius: 6px;
      padding: 0.65rem;
      padding-inline-start: 1.75rem;
      background: #fff;
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
      cursor: pointer;
      font: inherit;
      color: inherit;
      position: relative;
    }
    .kanban__card:hover {
      border-color: var(--crm-teal, #0d9488);
    }
    .kanban__drag {
      position: absolute;
      inset-inline-start: 0.15rem;
      top: 0.35rem;
      cursor: grab;
      color: var(--crm-muted, #9ca3af);
      padding: 0;
      line-height: 0;
    }
    .kanban__drag mat-icon {
      font-size: 1.1rem;
      width: 1.1rem;
      height: 1.1rem;
    }
    .kanban__card.cdk-drag-preview {
      box-shadow: 0 8px 24px rgb(0 0 0 / 18%);
      border-color: var(--crm-teal, #0d9488);
    }
    .kanban__card.cdk-drag-placeholder {
      opacity: 0.35;
    }
    .cdk-drag-animating {
      transition: transform 200ms cubic-bezier(0, 0, 0.2, 1);
    }
    .kanban__col.cdk-drop-list-dragging .kanban__card:not(.cdk-drag-placeholder) {
      transition: transform 200ms cubic-bezier(0, 0, 0.2, 1);
    }
    .kanban__num {
      font-size: 0.75rem;
      opacity: 0.7;
    }
    .kanban__val {
      font-size: 0.8rem;
      font-weight: 600;
    }
    .kanban__status {
      font-size: 0.75rem;
      opacity: 0.8;
    }
    .cal {
      padding: 1rem;
    }
    .cal__nav {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      margin-bottom: 0.75rem;
    }
    .cal__nav strong {
      min-width: 10rem;
      text-align: center;
    }
    .cal__weekdays {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 2px;
      margin-bottom: 2px;
      font-size: 0.75rem;
      opacity: 0.7;
      text-align: center;
    }
    .cal__grid {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 2px;
    }
    .cal__day {
      min-height: 6.5rem;
      border: 1px solid var(--crm-border, #e5e7eb);
      border-radius: 4px;
      padding: 0.25rem;
      background: #fff;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }
    .cal__day--muted {
      opacity: 0.45;
      background: color-mix(in srgb, var(--crm-border, #e5e7eb) 25%, #fff);
    }
    .cal__day--today {
      border-color: var(--crm-teal, #0d9488);
      box-shadow: inset 0 0 0 1px var(--crm-teal, #0d9488);
    }
    .cal__num {
      font-size: 0.75rem;
      font-weight: 600;
    }
    .cal__events {
      display: flex;
      flex-direction: column;
      gap: 2px;
      overflow: hidden;
    }
    .cal__ev {
      text-align: start;
      border: none;
      border-radius: 3px;
      padding: 0.15rem 0.3rem;
      font: inherit;
      font-size: 0.68rem;
      cursor: pointer;
      color: #fff;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      background: var(--crm-teal, #0d9488);
    }
    .cal__ev--int {
      background: #64748b;
    }
    .cal__empty {
      margin: 0.75rem 0 0;
      color: var(--crm-muted, #6b7280);
      font-size: 0.85rem;
    }
  `
})
export class OpportunityListPageComponent implements OnInit {
  private readonly api = inject(OpportunityService);
  readonly lookups = inject(LookupService);
  private readonly meta = inject(MetaService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly workflow = inject(WorkflowActionService);
  private kanbanDragMoved = false;

  readonly rows = signal<OpportunityListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly viewMode = signal<'list' | 'kanban' | 'calendar'>('list');
  readonly calendarMonth = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly activePreset = signal<string | null>(null);
  readonly facetChips = signal<{ key: string; label: string; node: DomainNode }[]>([]);
  readonly visibleColumns = signal<string[]>([...ALL_COLUMN_KEYS]);
  readonly savedViews = signal<SavedView[]>([]);
  readonly metaFields = signal<MetaField[]>([]);
  readonly searchText = signal('');

  readonly filters = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    stageId: new FormControl('', { nonNullable: true }),
    myItemsOnly: new FormControl(false, { nonNullable: true }),
    pendingOnMe: new FormControl(false, { nonNullable: true }),
    isClosed: new FormControl<string>('', { nonNullable: true })
  });

  readonly columnDefs: DataTableColumn<OpportunityListItem>[] = [
    { key: 'opportunityNumber', headerKey: 'opp.number' },
    { key: 'name', headerKey: 'opp.name' },
    {
      key: 'customerName',
      headerKey: 'opp.customer',
      cell: (r) => (this.lang() === 'ar' ? r.customerNameAr || r.customerName || '' : r.customerName || '')
    },
    {
      key: 'stageNameEn',
      headerKey: 'opp.stage',
      cell: (r) => (this.lang() === 'ar' ? r.stageNameAr || r.stageCode || '' : r.stageNameEn || r.stageCode || '')
    },
    {
      key: 'statusNameEn',
      headerKey: 'opp.status',
      cell: (r) => (this.lang() === 'ar' ? r.statusNameAr || r.statusCode || '' : r.statusNameEn || r.statusCode || '')
    },
    {
      key: 'pendingUserName',
      headerKey: 'opp.pendingOn',
      cell: (r) =>
        r.pendingUserName ||
        (this.lang() === 'ar' ? r.pendingGateNameAr || r.pendingRoleCode || '' : r.pendingGateNameEn || r.pendingRoleCode || '') ||
        '—'
    },
    { key: 'expectedValueSar', headerKey: 'opp.expectedValue', cell: (r) => String(r.expectedValueSar) },
    {
      key: 'submissionDeadline',
      headerKey: 'deadlines.submission',
      cell: (r) => (r.submissionDeadline ? r.submissionDeadline.slice(0, 10) : '—')
    },
    { key: 'ownerDisplayName', headerKey: 'opp.owner', cell: (r) => r.ownerDisplayName || '—' },
    { key: 'builderDisplayName', headerKey: 'opp.builder', cell: (r) => r.builderDisplayName || '—' }
  ];

  readonly activeColumns = computed(() =>
    this.columnDefs.filter((c) => this.visibleColumns().includes(c.key))
  );

  readonly facetOptions = computed(() => {
    const q = this.searchText().trim();
    if (!q) return [];
    return [
      { id: 'name', label: `${this.i18n.instant('opp.searchName')}: ${q}`, field: 'name', value: q },
      { id: 'number', label: `${this.i18n.instant('opp.searchNumber')}: ${q}`, field: 'opportunityNumber', value: q },
      { id: 'customer', label: `${this.i18n.instant('opp.searchCustomer')}: ${q}`, field: 'customerName', value: q }
    ];
  });

  readonly kanbanColumns = computed(() => {
    const stages = this.lookups.stages();
    const items = this.rows();
    return stages.map((s) => ({
      stageId: s.id,
      title: this.lang() === 'ar' ? s.nameAr : s.nameEn,
      items: items.filter((i) => i.stageId === s.id)
    }));
  });

  readonly weekdayLabels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  readonly calendarTitle = computed(() => {
    const d = this.calendarMonth();
    return d.toLocaleString(this.lang() === 'ar' ? 'ar-SA' : 'en-US', { month: 'long', year: 'numeric' });
  });

  readonly calendarCells = computed(() => {
    const monthStart = this.calendarMonth();
    const y = monthStart.getFullYear();
    const m = monthStart.getMonth();
    const firstDow = new Date(y, m, 1).getDay();
    const daysInMonth = new Date(y, m + 1, 0).getDate();
    const prevDays = new Date(y, m, 0).getDate();
    const todayKey = this.dayKey(new Date());
    const eventsByDay = new Map<string, { id: string; kind: 'submission' | 'internal'; label: string; title: string; item: OpportunityListItem }[]>();

    for (const item of this.rows()) {
      const push = (iso: string | null | undefined, kind: 'submission' | 'internal') => {
        if (!iso) return;
        const d = new Date(iso);
        if (Number.isNaN(d.getTime())) return;
        const key = this.dayKey(d);
        const kindLabel = this.i18n.instant(kind === 'submission' ? 'opp.calendarSubmission' : 'opp.calendarInternal');
        const list = eventsByDay.get(key) ?? [];
        list.push({
          id: item.id,
          kind,
          label: `${item.opportunityNumber}`,
          title: `${kindLabel}: ${item.opportunityNumber} · ${item.name}`,
          item
        });
        eventsByDay.set(key, list);
      };
      push(item.submissionDeadline, 'submission');
      push(item.internalDeadline, 'internal');
    }

    const cells: {
      key: string;
      day: number;
      inMonth: boolean;
      isToday: boolean;
      events: { id: string; kind: 'submission' | 'internal'; label: string; title: string; item: OpportunityListItem }[];
    }[] = [];

    for (let i = 0; i < firstDow; i++) {
      const day = prevDays - firstDow + i + 1;
      const d = new Date(y, m - 1, day);
      const key = this.dayKey(d);
      cells.push({ key: `p-${key}`, day, inMonth: false, isToday: key === todayKey, events: eventsByDay.get(key) ?? [] });
    }
    for (let day = 1; day <= daysInMonth; day++) {
      const d = new Date(y, m, day);
      const key = this.dayKey(d);
      cells.push({ key, day, inMonth: true, isToday: key === todayKey, events: eventsByDay.get(key) ?? [] });
    }
    const trailing = (7 - (cells.length % 7)) % 7;
    for (let day = 1; day <= trailing; day++) {
      const d = new Date(y, m + 1, day);
      const key = this.dayKey(d);
      cells.push({ key: `n-${key}`, day, inMonth: false, isToday: key === todayKey, events: eventsByDay.get(key) ?? [] });
    }
    return cells;
  });

  readonly calendarHasEvents = computed(() => this.calendarCells().some((c) => c.inMonth && c.events.length > 0));

  private dayKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  readonly presets: Preset[] = [
    { id: 'mine', labelKey: 'opp.presetMine', apply: () => ({ myItemsOnly: true, preset: 'mine' }) },
    { id: 'pending', labelKey: 'opp.presetPendingMe', apply: () => ({ pendingOnMe: true, preset: 'pending' }) },
    { id: 'open', labelKey: 'opp.presetOpen', apply: () => ({ isClosed: false, filter: encodeDomain([['isClosed', '=', false]]), preset: 'open' }) },
    { id: 'closed', labelKey: 'opp.presetClosed', apply: () => ({ isClosed: true, filter: encodeDomain([['isClosed', '=', true]]), preset: 'closed' }) },
    {
      id: 'overdue',
      labelKey: 'opp.presetOverdue',
      apply: () => ({
        filter: encodeDomain([
          ['submissionDeadline', '<', new Date().toISOString()],
          '&',
          ['isClosed', '=', false]
        ]),
        preset: 'overdue'
      })
    },
    {
      id: 'bidbond',
      labelKey: 'opp.presetBidBond',
      apply: () => ({ filter: encodeDomain([['requiresBidBond', '=', true]]), preset: 'bidbond' })
    }
  ];

  lang(): string {
    return this.i18n.getCurrentLang() ?? 'en';
  }

  ngOnInit(): void {
    this.lookups.bootstrap().subscribe();
    this.meta.fields('opportunities').subscribe({ next: (f) => this.metaFields.set(f), error: () => null });
    this.reloadViews();
    const storedCols = localStorage.getItem('crm.opp.columns');
    if (storedCols) {
      try {
        this.visibleColumns.set(JSON.parse(storedCols));
      } catch {
        /* ignore */
      }
    }

    this.filters.controls.search.valueChanges.subscribe((v) => this.searchText.set(v ?? ''));

    this.route.queryParamMap.subscribe((q) => {
      this.filters.patchValue(
        {
          search: q.get('search') ?? '',
          stageId: q.get('stageId') ?? '',
          myItemsOnly: q.get('myItemsOnly') === 'true',
          pendingOnMe: q.get('pendingOnMe') === 'true',
          isClosed: q.get('isClosed') ?? ''
        },
        { emitEvent: false }
      );
      this.searchText.set(q.get('search') ?? '');
      this.page.set(Number(q.get('page') ?? 1));
      this.pageSize.set(Number(q.get('pageSize') ?? 25));
      this.viewMode.set(q.get('view') === 'kanban' ? 'kanban' : q.get('view') === 'calendar' ? 'calendar' : 'list');
      this.activePreset.set(q.get('preset'));
      const filter = q.get('filter');
      if (filter) {
        // chips already driven by facets; keep filter in URL
      }
      this.load();
    });
  }

  query(): OpportunityListQuery {
    const v = this.filters.getRawValue();
    const domain = this.facetChips().map((c) => c.node);
    const filterFromUrl = this.route.snapshot.queryParamMap.get('filter');
    let filter = filterFromUrl || undefined;
    if (domain.length) {
      filter = encodeDomain(domain as DomainNode[]);
    }
    return {
      page: this.page(),
      pageSize: this.viewMode() === 'list' ? this.pageSize() : 200,
      search: v.search || undefined,
      stageId: v.stageId || undefined,
      myItemsOnly: v.myItemsOnly || undefined,
      pendingOnMe: v.pendingOnMe || undefined,
      isClosed: v.isClosed === '' ? null : v.isClosed === 'true',
      filter
    };
  }

  load(): void {
    this.api.list(this.query()).subscribe({
      next: (r) => {
        this.rows.set(r.items);
        this.total.set(r.totalCount);
      },
      error: () => {
        this.rows.set([]);
        this.total.set(0);
      }
    });
  }

  apply(): void {
    this.page.set(1);
    this.syncUrl();
  }

  onSearchEnter(ev: Event): void {
    ev.preventDefault();
    this.apply();
  }

  onFacet(opt: { field: string; value: string; label: string }): void {
    const key = `${opt.field}:${opt.value}`;
    if (this.facetChips().some((c) => c.key === key)) return;
    const node = facetLeaf(opt.field, 'ilike', opt.value);
    this.facetChips.update((chips) => [...chips, { key, label: opt.label, node }]);
    this.filters.patchValue({ search: '' });
    this.searchText.set('');
    this.page.set(1);
    this.syncUrl();
  }

  removeFacet(key: string): void {
    this.facetChips.update((chips) => chips.filter((c) => c.key !== key));
    this.page.set(1);
    this.syncUrl();
  }

  applyPreset(p: Preset): void {
    const patch = p.apply();
    this.facetChips.set([]);
    this.filters.patchValue({
      myItemsOnly: !!patch.myItemsOnly,
      pendingOnMe: !!patch.pendingOnMe,
      isClosed: patch.isClosed === undefined || patch.isClosed === null ? '' : String(patch.isClosed),
      search: '',
      stageId: ''
    });
    this.activePreset.set(p.id);
    this.page.set(1);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: 1,
        pageSize: this.pageSize(),
        view: this.viewMode(),
        preset: p.id,
        filter: patch.filter ?? null,
        myItemsOnly: patch.myItemsOnly ? true : null,
        pendingOnMe: patch.pendingOnMe ? true : null,
        isClosed: patch.isClosed === undefined || patch.isClosed === null ? null : patch.isClosed,
        search: null,
        stageId: null
      },
      queryParamsHandling: 'merge'
    });
  }

  setView(mode: 'list' | 'kanban' | 'calendar'): void {
    this.viewMode.set(mode);
    this.page.set(1);
    this.syncUrl();
  }

  shiftMonth(delta: number): void {
    const d = this.calendarMonth();
    this.calendarMonth.set(new Date(d.getFullYear(), d.getMonth() + delta, 1));
  }

  goThisMonth(): void {
    const now = new Date();
    this.calendarMonth.set(new Date(now.getFullYear(), now.getMonth(), 1));
  }

  toggleColumn(key: string): void {
    this.visibleColumns.update((cols) => {
      const next = cols.includes(key) ? cols.filter((c) => c !== key) : [...cols, key];
      localStorage.setItem('crm.opp.columns', JSON.stringify(next));
      return next.length ? next : cols;
    });
  }

  syncUrl(): void {
    const v = this.filters.getRawValue();
    const domain = this.facetChips().map((c) => c.node);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: this.page(),
        pageSize: this.pageSize(),
        view: this.viewMode(),
        search: v.search || null,
        stageId: v.stageId || null,
        myItemsOnly: v.myItemsOnly ? true : null,
        pendingOnMe: v.pendingOnMe ? true : null,
        isClosed: v.isClosed === '' ? null : v.isClosed,
        preset: this.activePreset(),
        filter: domain.length ? encodeDomain(domain as DomainNode[]) : this.route.snapshot.queryParamMap.get('filter')
      },
      queryParamsHandling: 'merge'
    });
  }

  onPage(e: { page: number; pageSize: number }): void {
    this.page.set(e.page);
    this.pageSize.set(e.pageSize);
    this.syncUrl();
  }

  open(row: OpportunityListItem): void {
    if (this.kanbanDragMoved) {
      this.kanbanDragMoved = false;
      return;
    }
    try {
      sessionStorage.setItem('crm.opportunityListIds', JSON.stringify(this.rows().map((r) => r.id)));
    } catch {
      /* ignore */
    }
    void this.router.navigate(['/opportunities', row.id]);
  }

  onKanbanDragStart(): void {
    this.kanbanDragMoved = true;
  }

  onKanbanDrop(event: CdkDragDrop<OpportunityListItem[]>, targetStageId: string): void {
    this.kanbanDragMoved = true;
    if (event.previousContainer === event.container) return;

    const item =
      (event.item.data as OpportunityListItem | undefined) ??
      event.previousContainer.data[event.previousIndex];
    if (!item || item.stageId === targetStageId || item.isClosed) return;

    this.moveKanbanCard(item, targetStageId);
  }

  private moveKanbanCard(item: OpportunityListItem, targetStageId: string): void {
    const stage = this.lookups.stages().find((s) => s.id === targetStageId);
    const stageName = (this.lang() === 'ar' ? stage?.nameAr : stage?.nameEn) || stage?.code || '';
    const stageCode = (stage?.code || '').toUpperCase();

    forkJoin({
      transitions: this.api.availableTransitions(item.id),
      next: this.api.nextActions(item.id)
    }).subscribe({
      next: ({ transitions, next }) => {
        const primary = next.primary;

        if (primary?.code?.startsWith('GATE_')) {
          this.toast.error(this.i18n.instant('opp.kanbanBlockedGate'));
          if (primary.canCurrentUserAct && primary.gateInstanceId) {
            this.workflow.openGateDecide(primary.gateInstanceId, item.id).subscribe((ok) => {
              if (ok) this.load();
            });
          }
          return;
        }

        const intoResponseDev = stageCode === 'RESPONSE_DEVELOPMENT';
        if (intoResponseDev && primary?.code === 'ASSIGN_BUILDER') {
          this.workflow.openAssignBuilder(item.id).subscribe((ok) => {
            if (ok) this.load();
          });
          return;
        }

        const options = transitions.filter((t) => t.stageId === targetStageId);
        if (!options.length) {
          this.toast.error(this.i18n.instant('opp.kanbanIllegalMove'));
          return;
        }

        if (options.length === 1 && !options[0].requiresReason) {
          this.applyKanbanMove(item.id, options[0].toStatusId);
          return;
        }

        this.dialog
          .open(KanbanMoveDialogComponent, {
            width: '480px',
            autoFocus: 'dialog',
            data: {
              opportunityNumber: item.opportunityNumber,
              opportunityName: item.name,
              targetStageName: stageName,
              options
            }
          })
          .afterClosed()
          .subscribe((result: KanbanMoveDialogResult | null | undefined) => {
            if (!result) return;
            this.applyKanbanMove(item.id, result.toStatusId, result.reason);
          });
      },
      error: () => this.toast.error(this.i18n.instant('opp.kanbanMoveFailed'))
    });
  }

  private applyKanbanMove(id: string, toStatusId: string, reason?: string): void {
    this.api.changeStatus(id, toStatusId, reason).subscribe({
      next: () => {
        this.toast.success('opp.kanbanMoved');
        this.load();
      },
      error: (err: { error?: { title?: string; detail?: string } }) => {
        this.toast.error(
          err?.error?.detail || err?.error?.title || this.i18n.instant('opp.kanbanMoveFailed')
        );
        this.load();
      }
    });
  }

  openExport(): void {
    const ref = this.dialog.open(ExportWizardDialogComponent, {
      width: '560px',
      data: { fields: this.metaFields().filter((f) => f.exportable) }
    });
    ref.afterClosed().subscribe((fields: string[] | undefined) => {
      if (!fields?.length) return;
      this.api.export(this.query()).subscribe({
        next: (blob) => triggerDownload(blob, 'opportunities.xlsx'),
        error: () => this.toast.error('Export failed')
      });
    });
  }

  reloadViews(): void {
    this.meta.savedViews('opportunities').subscribe({
      next: (v) => this.savedViews.set(v),
      error: () => this.savedViews.set([])
    });
  }

  saveCurrentView(): void {
    const name = prompt(this.i18n.instant('opp.saveViewPrompt'));
    if (!name?.trim()) return;
    const def: OpportunityViewDefinition = {
      search: this.filters.value.search || undefined,
      filter: this.route.snapshot.queryParamMap.get('filter') || undefined,
      myItemsOnly: this.filters.value.myItemsOnly || undefined,
      pendingOnMe: this.filters.value.pendingOnMe || undefined,
      preset: this.activePreset() || undefined,
      view: this.viewMode(),
      columns: this.visibleColumns()
    };
    this.meta
      .createSavedView({
        name: name.trim(),
        entityType: 'opportunities',
        definitionJson: JSON.stringify(def),
        isShared: false,
        isDefault: false
      })
      .subscribe({
        next: () => {
          this.toast.success('admin.saved');
          this.reloadViews();
        },
        error: () => this.toast.error('Failed to save view')
      });
  }

  loadView(v: SavedView): void {
    try {
      const def = JSON.parse(v.definitionJson) as OpportunityViewDefinition;
      if (def.columns?.length) this.visibleColumns.set(def.columns);
      this.viewMode.set(def.view === 'kanban' ? 'kanban' : def.view === 'calendar' ? 'calendar' : 'list');
      this.filters.patchValue({
        search: def.search ?? '',
        myItemsOnly: !!def.myItemsOnly,
        pendingOnMe: !!def.pendingOnMe
      });
      this.activePreset.set(def.preset ?? null);
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {
          page: 1,
          view: def.view ?? 'list',
          search: def.search ?? null,
          filter: def.filter ?? null,
          myItemsOnly: def.myItemsOnly ? true : null,
          pendingOnMe: def.pendingOnMe ? true : null,
          preset: def.preset ?? null
        }
      });
    } catch {
      this.toast.error('Invalid saved view');
    }
  }
}
