import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { PendingApproval } from '../../core/models/gate';
import { GateService } from '../../core/services/gate.service';
import { pickLocalized } from '../../core/utils/locale';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { DataTableComponent, DataTableColumn } from '../../shared/components/data-table/data-table.component';

@Component({
  selector: 'crm-pending-approvals-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, DataTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="approvals.pending" />
      <crm-data-table
        [columns]="columns"
        [rows]="rows()"
        [total]="total()"
        emptyTitleKey="approvals.emptyTitle"
        emptyMessageKey="approvals.emptyMessage"
        (rowClick)="open($event)"
        (pageChange)="load($event.page)"
      />
    </div>
  `
})
export class PendingApprovalsPageComponent implements OnInit {
  private readonly api = inject(GateService);
  private readonly router = inject(Router);
  private readonly i18n = inject(TranslateService);
  readonly rows = signal<PendingApproval[]>([]);
  readonly total = signal(0);
  readonly columns: DataTableColumn<PendingApproval>[] = [
    { key: 'opportunityNumber', headerKey: 'opp.number' },
    { key: 'opportunityName', headerKey: 'opp.name' },
    {
      key: 'gateNameEn',
      headerKey: 'gates.name',
      cell: (r) => pickLocalized(this.i18n.getCurrentLang(), r.gateNameEn, r.gateNameAr, r.gateCode || '')
    },
    {
      key: 'assignedRoleCode',
      headerKey: 'gates.role',
      cell: (r) => this.roleLabel(r.assignedRoleCode)
    }
  ];

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.api.pending({ page, pageSize: 25 }).subscribe((r) => {
      this.rows.set(r.items);
      this.total.set(r.totalCount);
    });
  }

  open(row: PendingApproval): void {
    void this.router.navigate(['/approvals', row.id]);
  }

  private roleLabel(code: string): string {
    const key = `roles.${code}`;
    const translated = this.i18n.instant(key);
    return translated === key ? code : translated;
  }
}
