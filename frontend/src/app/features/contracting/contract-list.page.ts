import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Contract } from '../../core/models/contract';
import { ContractService } from '../../core/services/contract.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { DataTableComponent, DataTableColumn } from '../../shared/components/data-table/data-table.component';

@Component({
  selector: 'crm-contract-list-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, DataTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="contract.listTitle" />
      <crm-data-table
        [columns]="columns"
        [rows]="rows()"
        [total]="total()"
        emptyTitleKey="contract.emptyTitle"
        emptyMessageKey="contract.emptyMessage"
        (rowClick)="open($event)"
        (pageChange)="load($event.page)"
      />
    </div>
  `
})
export class ContractListPageComponent implements OnInit {
  private readonly api = inject(ContractService);
  private readonly router = inject(Router);
  readonly rows = signal<Contract[]>([]);
  readonly total = signal(0);
  readonly columns: DataTableColumn<Contract>[] = [
    { key: 'contractNumber', headerKey: 'contract.number' },
    { key: 'opportunityName', headerKey: 'opp.name' },
    { key: 'customerName', headerKey: 'opp.customer' },
    { key: 'contractStatus', headerKey: 'contract.status' }
  ];

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.api.list({ page, pageSize: 25 }).subscribe((r) => {
      this.rows.set(r.items);
      this.total.set(r.totalCount);
    });
  }

  open(row: Contract): void {
    void this.router.navigate(['/contracting', row.id]);
  }
}
