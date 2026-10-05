import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ProposalTask } from '../../core/models/contract';
import { ProposalService } from '../../core/services/proposal.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { DataTableComponent, DataTableColumn } from '../../shared/components/data-table/data-table.component';

@Component({
  selector: 'crm-my-tasks-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, DataTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="proposal.myTasks" />
      <crm-data-table
        [columns]="columns"
        [rows]="rows()"
        [total]="total()"
        emptyTitleKey="proposal.emptyTitle"
        emptyMessageKey="proposal.emptyMessage"
        (rowClick)="open($event)"
        (pageChange)="load($event.page)"
      />
    </div>
  `
})
export class MyTasksPageComponent implements OnInit {
  private readonly api = inject(ProposalService);
  private readonly router = inject(Router);
  readonly rows = signal<ProposalTask[]>([]);
  readonly total = signal(0);
  readonly columns: DataTableColumn<ProposalTask>[] = [
    { key: 'opportunityNumber', headerKey: 'opp.number' },
    { key: 'opportunityName', headerKey: 'opp.name' },
    { key: 'customerName', headerKey: 'opp.customer' }
  ];

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.api.myTasks({ page, pageSize: 25 }).subscribe((r) => {
      this.rows.set(r.items);
      this.total.set(r.totalCount);
    });
  }

  open(row: ProposalTask): void {
    void this.router.navigate(['/proposals', row.opportunityId, 'workspace']);
  }
}
