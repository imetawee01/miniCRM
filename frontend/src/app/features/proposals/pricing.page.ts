import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ProposalPricing } from '../../core/models/contract';
import { ProposalService } from '../../core/services/proposal.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SarPipe } from '../../shared/pipes/sar.pipe';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-pricing-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, SarPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="proposal.pricing" />
      <form class="crm-card crm-grid crm-grid-2" [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'proposal.price' | translate }}</mat-label>
          <input matInput type="number" formControlName="priceSar" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'proposal.cost' | translate }}</mat-label>
          <input matInput type="number" formControlName="costSar" />
        </mat-form-field>
        <p>{{ 'proposal.margin' | translate }}: {{ margin() }}%</p>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">{{ 'common.save' | translate }}</button>
      </form>
      <table>
        <tr>
          <th>{{ 'proposal.version' | translate }}</th>
          <th>{{ 'proposal.price' | translate }}</th>
          <th>{{ 'proposal.cost' | translate }}</th>
          <th>{{ 'proposal.margin' | translate }}</th>
        </tr>
        @for (p of versions(); track p.id) {
          <tr>
            <td>{{ p.version }}</td>
            <td>{{ p.priceSar | sar }}</td>
            <td>{{ p.costSar | sar }}</td>
            <td>{{ p.marginPercent }}%</td>
          </tr>
        }
      </table>
    </div>
  `
})
export class PricingPageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(ProposalService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly versions = signal<ProposalPricing[]>([]);
  readonly form = new FormGroup({
    priceSar: new FormControl<number | null>(null, Validators.required),
    costSar: new FormControl<number | null>(null, Validators.required)
  });

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  margin(): string {
    const p = Number(this.form.controls.priceSar.value);
    const c = Number(this.form.controls.costSar.value);
    if (!p) return '0.00';
    return (((p - c) / p) * 100).toFixed(2);
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.api.pricing(id).subscribe((rows) => this.versions.set(rows));
  }

  save(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const v = this.form.getRawValue();
    if (!id || v.priceSar == null || v.costSar == null) return;
    this.api.addPricing(id, v.priceSar, v.costSar).subscribe(() => {
      this.form.markAsPristine();
      this.toast.success('proposal.pricingSaved');
      this.reload();
    });
  }
}
