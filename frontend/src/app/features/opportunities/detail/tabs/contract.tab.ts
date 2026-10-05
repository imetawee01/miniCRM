import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Contract } from '../../../../core/models/contract';
import { ContractService } from '../../../../core/services/contract.service';
import { ToastService } from '../../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { SarPipe } from '../../../../shared/pipes/sar.pipe';
import { EnumLabelPipe } from '../../../../shared/pipes/enum-label.pipe';
import { NotesPanelComponent } from '../../../../shared/components/notes-panel/notes-panel.component';
import { AttachmentListComponent } from '../../../../shared/components/attachment-list/attachment-list.component';

import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';

@Component({
  selector: 'crm-contract-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, SarPipe, EnumLabelPipe, NotesPanelComponent, AttachmentListComponent, HasPermissionDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      @if (contract(); as c) {
        <section class="crm-card">
          <h3>{{ c.contractNumber }}</h3>
          <p>
            {{ c.contractStatus | enumLabel }}
            <ng-container *crmHasPermission="'CanViewPricing'"> · {{ c.contractValueSar | sar }}</ng-container>
          </p>
          <a mat-stroked-button [routerLink]="['/contracting', c.id]">{{ 'contract.open' | translate }}</a>
        </section>
        <crm-notes-panel entityType="contracts" [entityId]="c.id" />
        <crm-attachment-list entityType="contracts" [entityId]="c.id" />
      } @else {
        <button mat-flat-button color="primary" type="button" (click)="create()">{{ 'contract.create' | translate }}</button>
      }
    </div>
  `
})
export class ContractTabComponent implements OnInit {
  private readonly api = inject(ContractService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly contract = signal<Contract | null>(null);

  private id(): string {
    return this.route.parent?.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    this.api.list({ search: this.id(), page: 1, pageSize: 5 }).subscribe((res) => {
      const found = res.items.find((c) => c.opportunityId === this.id()) ?? null;
      this.contract.set(found);
    });
  }

  create(): void {
    this.api.createFromOpportunity(this.id()).subscribe((c) => {
      this.contract.set(c);
      this.toast.success('contract.created');
    });
  }
}
