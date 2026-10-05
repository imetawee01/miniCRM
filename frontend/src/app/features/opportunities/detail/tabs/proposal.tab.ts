import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BidBond, EstimatedCost, ProposalPricing, SlResponse } from '../../../../core/models/contract';
import { UserPickItem } from '../../../../core/models/user';
import { ProposalService } from '../../../../core/services/proposal.service';
import { BidBondService } from '../../../../core/services/bid-bond.service';
import { UsersService } from '../../../../core/services/users.service';
import { ToastService } from '../../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { SarPipe } from '../../../../shared/pipes/sar.pipe';
import { EnumLabelPipe } from '../../../../shared/pipes/enum-label.pipe';

import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';

@Component({
  selector: 'crm-proposal-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, RouterLink, SarPipe, EnumLabelPipe, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      <div class="crm-actions">
        <a mat-stroked-button [routerLink]="['/proposals', id(), 'workspace']">{{ 'proposal.workspace' | translate }}</a>
        <a mat-stroked-button *crmHasPermission="'CanViewPricing'" [routerLink]="['/proposals', id(), 'pricing']">{{ 'proposal.pricing' | translate }}</a>
        <a mat-stroked-button [routerLink]="['/proposals', id(), 'review']">{{ 'proposal.review' | translate }}</a>
      </div>
      <form class="crm-card crm-row" [formGroup]="builderForm" (ngSubmit)="assign()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'proposal.builderType' | translate }}</mat-label>
          <mat-select formControlName="builderType">
            <mat-option value="Presales">{{ 'Presales' | enumLabel }}</mat-option>
            <mat-option value="ServiceLine">{{ 'ServiceLine' | enumLabel }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'proposal.builderUserId' | translate }}</mat-label>
          <mat-select formControlName="builderUserId">
            @for (u of builders(); track u.id) {
              <mat-option [value]="u.id">{{ u.displayName }} ({{ u.email }})</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <button mat-flat-button type="submit" [disabled]="builderForm.invalid">{{ 'proposal.assign' | translate }}</button>
      </form>
      @if (cost(); as c) {
        <ng-container *crmHasPermission="'CanViewPricing'">
          <p>{{ 'proposal.estimatedCost' | translate }}: {{ c.amount | sar }}</p>
        </ng-container>
      }
      @if (bond(); as b) {
        <p>{{ 'proposal.bidBond' | translate }}: {{ b.status | enumLabel }}</p>
      }
      @if (pricing().length && pricing()[0].pricingVisible !== false) {
        <ng-container *crmHasPermission="'CanViewPricing'">
          <p>{{ 'proposal.currentMargin' | translate }}: {{ pricing()[0].marginPercent }}%</p>
        </ng-container>
      }
    </div>
  `
})
export class ProposalTabComponent implements OnInit {
  private readonly proposals = inject(ProposalService);
  private readonly bonds = inject(BidBondService);
  private readonly users = inject(UsersService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly cost = signal<EstimatedCost | null>(null);
  readonly bond = signal<BidBond | null>(null);
  readonly pricing = signal<ProposalPricing[]>([]);
  readonly sl = signal<SlResponse[]>([]);
  readonly builders = signal<UserPickItem[]>([]);
  readonly builderForm = new FormGroup({
    builderType: new FormControl<'Presales' | 'ServiceLine'>('Presales', { nonNullable: true }),
    builderUserId: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  id(): string {
    return this.route.parent?.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    const id = this.id();
    this.proposals.estimatedCost(id).subscribe({ next: (v) => this.cost.set(v), error: () => null });
    this.bonds.get(id).subscribe({ next: (v) => this.bond.set(v), error: () => null });
    this.proposals.pricing(id).subscribe({ next: (v) => this.pricing.set(v), error: () => null });
    this.proposals.slResponses(id).subscribe({ next: (v) => this.sl.set(v), error: () => null });
    this.loadBuilders(this.builderForm.controls.builderType.value);
    this.builderForm.controls.builderType.valueChanges.subscribe((t) => {
      this.builderForm.controls.builderUserId.setValue('');
      this.loadBuilders(t);
    });
  }

  private loadBuilders(type: 'Presales' | 'ServiceLine'): void {
    const role = type === 'Presales' ? 'PRESALES' : 'SL';
    this.users.pickable(role).subscribe({
      next: (u) => this.builders.set(u),
      error: () => this.builders.set([])
    });
  }

  assign(): void {
    if (this.builderForm.invalid) return;
    this.proposals.assignBuilder(this.id(), this.builderForm.getRawValue()).subscribe(() => this.toast.success('proposal.assigned'));
  }
}
