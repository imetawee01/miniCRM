import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GateInstance } from '../../core/models/gate';
import { GateService } from '../../core/services/gate.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ApprovalPanelComponent } from '../../shared/components/approval-panel/approval-panel.component';

@Component({
  selector: 'crm-proposal-review-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ApprovalPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="proposal.review" />
      @if (gate(); as g) {
        <crm-approval-panel [gate]="g" />
      }
    </div>
  `
})
export class ProposalReviewPageComponent implements OnInit {
  private readonly gates = inject(GateService);
  private readonly route = inject(ActivatedRoute);
  readonly gate = signal<GateInstance | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.gates.forOpportunity(id).subscribe((rows) => {
      this.gate.set(rows.find((g) => (g.gateCode || g.gate?.code) === 'PROPOSAL_REVIEW' && g.state === 'Pending') ?? rows[0] ?? null);
    });
  }
}
