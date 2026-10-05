import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { matchFields } from '../../core/utils/validators';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { CanComponentDeactivate } from '../../core/auth/unsaved-changes.guard';

@Component({
  selector: 'crm-change-password-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="auth.changePassword" />
      <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'auth.currentPassword' | translate }}</mat-label>
          <input matInput type="password" formControlName="currentPassword" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'auth.newPassword' | translate }}</mat-label>
          <input matInput type="password" formControlName="newPassword" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'auth.confirmPassword' | translate }}</mat-label>
          <input matInput type="password" formControlName="confirmPassword" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || busy()">
          {{ 'common.save' | translate }}
        </button>
      </form>
    </div>
  `
})
export class ChangePasswordPageComponent implements CanComponentDeactivate {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  readonly busy = signal(false);
  readonly form = new FormGroup(
    {
      currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] })
    },
    { validators: matchFields('newPassword', 'confirmPassword') }
  );

  canDeactivate(): boolean {
    return !this.form.dirty;
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.busy.set(true);
    const { currentPassword, newPassword } = this.form.getRawValue();
    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.form.markAsPristine();
        this.toast.success('auth.passwordChanged');
        this.busy.set(false);
      },
      error: () => this.busy.set(false)
    });
  }
}
