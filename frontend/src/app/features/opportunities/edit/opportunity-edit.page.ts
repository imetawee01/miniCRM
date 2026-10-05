import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateOpportunityRequest, OpportunityDetail } from '../../../core/models/opportunity';
import { OpportunityService } from '../../../core/services/opportunity.service';
import { ToastService } from '../../../core/services/toast.service';
import { CanComponentDeactivate } from '../../../core/auth/unsaved-changes.guard';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { OpportunityFormComponent } from '../components/opportunity-form.component';

@Component({
  selector: 'crm-opportunity-edit-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, OpportunityFormComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="opp.edit" />
      @if (opp(); as o) {
        <div class="crm-card">
          <crm-opportunity-form [value]="o" (saved)="save($event)" />
        </div>
      }
    </div>
  `
})
export class OpportunityEditPageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(OpportunityService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly opp = signal<OpportunityDetail | null>(null);
  readonly form = viewChild(OpportunityFormComponent);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.api.get(id).subscribe((o) => this.opp.set(o));
    }
  }

  canDeactivate(): boolean {
    return !this.form()?.dirty;
  }

  save(body: CreateOpportunityRequest): void {
    const id = this.opp()?.id;
    if (!id) {
      return;
    }
    this.api.update(id, { ...body, rowVersion: this.opp()?.rowVersion }).subscribe((opp) => {
      this.form()?.form.markAsPristine();
      this.toast.success('opp.updated');
      void this.router.navigate(['/opportunities', opp.id, 'overview']);
    });
  }
}
