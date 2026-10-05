import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'crm-reports-home-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="reports.title" />
      <div class="crm-grid crm-grid-2">
        <a class="crm-card" routerLink="funnel">{{ 'reports.funnel' | translate }}</a>
        <a class="crm-card" routerLink="win-loss">{{ 'reports.winLoss' | translate }}</a>
        <a class="crm-card" routerLink="cycle-time">{{ 'reports.cycleTime' | translate }}</a>
        <a class="crm-card" routerLink="sl-performance">{{ 'reports.slPerformance' | translate }}</a>
        <a class="crm-card" routerLink="deadline-compliance">{{ 'reports.deadlineCompliance' | translate }}</a>
        <a class="crm-card" routerLink="approval-throughput">{{ 'reports.approvalThroughput' | translate }}</a>
        <a class="crm-card" routerLink="pivot">{{ 'reports.pivot' | translate }}</a>
      </div>
    </div>
  `,
  styles: `a.crm-card { text-decoration: none; color: inherit; font-weight: 600; }`
})
export class ReportsHomePageComponent {}
