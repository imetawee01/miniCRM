import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GateInstance } from '../../core/models/gate';
import { GateService } from '../../core/services/gate.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ApprovalPanelComponent } from '../../shared/components/approval-panel/approval-panel.component';

@Component({
  selector: 'crm-approval-detail-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ApprovalPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="approvals.detail" />
      @if (gate(); as g) {
        <crm-approval-panel [gate]="g" />
      }
    </div>
  `
})
export class ApprovalDetailPageComponent implements OnInit {
  private readonly api = inject(GateService);
  private readonly route = inject(ActivatedRoute);
  readonly gate = signal<GateInstance | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('gateInstanceId');
    if (id) this.api.get(id).subscribe((g) => this.gate.set(g));
  }
}
