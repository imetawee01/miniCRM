import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { OpportunityListItem } from '../../core/models/opportunity';
import { QualificationService } from '../../core/services/qualification.service';
import { pickLocalized } from '../../core/utils/locale';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { DataTableComponent, DataTableColumn } from '../../shared/components/data-table/data-table.component';

@Component({
  selector: 'crm-qualification-queue-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, DataTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="qual.queueTitle" />
      <crm-data-table
        [columns]="columns"
        [rows]="rows()"
        [total]="total()"
        emptyTitleKey="qual.emptyTitle"
        emptyMessageKey="qual.emptyMessage"
        (rowClick)="open($event)"
        (pageChange)="load($event.page)"
      />
    </div>
  `
})
export class QualificationQueuePageComponent implements OnInit {
  private readonly api = inject(QualificationService);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslateService);
  readonly rows = signal<OpportunityListItem[]>([]);
  readonly total = signal(0);
  readonly columns: DataTableColumn<OpportunityListItem>[] = [
    { key: 'opportunityNumber', headerKey: 'opp.number' },
    { key: 'name', headerKey: 'opp.name' },
    {
      key: 'statusNameEn',
      headerKey: 'opp.status',
      cell: (r) => pickLocalized(this.i18n.getCurrentLang(), r.statusNameEn, r.statusNameAr, r.statusCode || '')
    }
  ];

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.api.queue({ page, pageSize: 25 }).subscribe((r) => {
      this.rows.set(r.items);
      this.total.set(r.totalCount);
    });
  }

  open(row: OpportunityListItem): void {
    void this.router.navigate(['/opportunities', row.id, 'qualification']);
  }
}
