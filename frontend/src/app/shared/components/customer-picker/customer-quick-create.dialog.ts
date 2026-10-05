import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Customer, UpsertCustomerRequest } from '../../../core/models/lookups';
import { CustomersService } from '../../../core/services/customers.service';
import { ToastService } from '../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../material';

export interface CustomerQuickCreateData {
  presetName?: string;
}

@Component({
  selector: 'crm-customer-quick-create-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'customer.quickCreateTitle' | translate }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="crm-stack">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.nameEn' | translate }}</mat-label>
          <input matInput formControlName="nameEn" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.nameAr' | translate }}</mat-label>
          <input matInput formControlName="nameAr" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.sector' | translate }}</mat-label>
          <input matInput formControlName="sector" />
        </mat-form-field>
        <mat-checkbox formControlName="isGovernment">{{ 'customer.isGovernment' | translate }}</mat-checkbox>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.website' | translate }}</mat-label>
          <input matInput formControlName="website" />
        </mat-form-field>
        <h3 class="contact-title">{{ 'customer.primaryContact' | translate }}</h3>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.contactName' | translate }}</mat-label>
          <input matInput formControlName="contactName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.contactEmail' | translate }}</mat-label>
          <input matInput type="email" formControlName="contactEmail" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.contactPhone' | translate }}</mat-label>
          <input matInput formControlName="contactPhone" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'customer.contactTitle' | translate }}</mat-label>
          <input matInput formControlName="contactTitle" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="busy()" (click)="ref.close(null)">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid || busy()" (click)="save()">
        {{ 'common.create' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .contact-title { margin: 0.5rem 0 0; font-size: 0.9rem; color: var(--crm-navy); }
    mat-dialog-content { min-width: min(420px, 92vw); }
  `
})
export class CustomerQuickCreateDialogComponent {
  readonly data = inject<CustomerQuickCreateData>(MAT_DIALOG_DATA, { optional: true });
  readonly ref = inject(MatDialogRef<CustomerQuickCreateDialogComponent, Customer | null>);
  private readonly api = inject(CustomersService);
  private readonly toast = inject(ToastService);
  readonly busy = signal(false);

  readonly form = new FormGroup({
    nameEn: new FormControl(this.data?.presetName ?? '', { nonNullable: true, validators: [Validators.required] }),
    nameAr: new FormControl(this.data?.presetName ?? '', { nonNullable: true, validators: [Validators.required] }),
    sector: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isGovernment: new FormControl(false, { nonNullable: true }),
    website: new FormControl('', { nonNullable: true }),
    contactName: new FormControl('', { nonNullable: true }),
    contactEmail: new FormControl('', { nonNullable: true }),
    contactPhone: new FormControl('', { nonNullable: true }),
    contactTitle: new FormControl('', { nonNullable: true })
  });

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    const v = this.form.getRawValue();
    const body: UpsertCustomerRequest = {
      nameEn: v.nameEn.trim(),
      nameAr: v.nameAr.trim(),
      sector: v.sector.trim(),
      isGovernment: v.isGovernment,
      website: v.website.trim() || null,
      isQuickCreated: true,
      primaryContact: v.contactName.trim()
        ? {
            name: v.contactName.trim(),
            email: v.contactEmail.trim() || null,
            phone: v.contactPhone.trim() || null,
            title: v.contactTitle.trim() || null
          }
        : null
    };
    this.api.create(body).subscribe({
      next: (c) => {
        this.toast.success('customer.created');
        this.ref.close(c);
      },
      error: () => this.busy.set(false)
    });
  }
}
