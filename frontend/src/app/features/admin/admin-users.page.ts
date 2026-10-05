import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ROLE_CODES, RoleCode } from '../../core/models/enums';
import { UserListItem } from '../../core/models/user';
import { UsersService } from '../../core/services/users.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EnumLabelPipe } from '../../shared/pipes/enum-label.pipe';

@Component({
  selector: 'crm-admin-users-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="admin.users" />
      <form class="crm-card crm-grid crm-grid-2" [formGroup]="form" (ngSubmit)="create()">
        <mat-form-field appearance="outline"><mat-label>{{ 'auth.email' | translate }}</mat-label><input matInput formControlName="email" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'admin.displayName' | translate }}</mat-label><input matInput formControlName="displayName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'auth.password' | translate }}</mat-label><input matInput type="password" formControlName="password" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'admin.roles' | translate }}</mat-label>
          <mat-select formControlName="roleCodes" multiple>
            @for (r of roles; track r) { <mat-option [value]="r">{{ r | enumLabel: 'roles' }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit">{{ 'common.add' | translate }}</button>
      </form>
      <table>
        <tr><th>{{ 'auth.email' | translate }}</th><th>{{ 'admin.displayName' | translate }}</th><th></th></tr>
        @for (u of users(); track u.id) {
          <tr>
            <td>{{ u.email }}</td>
            <td>{{ u.displayName }}</td>
            <td>
              <button mat-button type="button" (click)="toggle(u)">{{ u.isActive ? ('admin.deactivate' | translate) : ('admin.activate' | translate) }}</button>
            </td>
          </tr>
        }
      </table>
    </div>
  `
})
export class AdminUsersPageComponent implements OnInit {
  private readonly api = inject(UsersService);
  private readonly toast = inject(ToastService);
  readonly users = signal<UserListItem[]>([]);
  readonly roles = ROLE_CODES;
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    roleCodes: new FormControl<RoleCode[]>([], { nonNullable: true })
  });

  ngOnInit(): void {
    this.reload();
  }
  reload(): void {
    this.api.list({ page: 1, pageSize: 100 }).subscribe((r) => this.users.set(r.items));
  }
  create(): void {
    this.api.create(this.form.getRawValue()).subscribe(() => {
      this.toast.success('admin.userCreated');
      this.form.reset();
      this.reload();
    });
  }
  toggle(u: UserListItem): void {
    (u.isActive ? this.api.deactivate(u.id) : this.api.activate(u.id)).subscribe(() => {
      this.toast.success('admin.userUpdated');
      this.reload();
    });
  }
}
