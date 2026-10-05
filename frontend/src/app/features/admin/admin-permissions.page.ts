import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { UsersService } from '../../core/services/users.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EnumLabelPipe } from '../../shared/pipes/enum-label.pipe';

export interface PermissionCell {
  policyName: string;
  roleCode: string;
  isAllowed: boolean;
}

export interface PermissionMatrix {
  policies: string[];
  roles: string[];
  cells: PermissionCell[];
}

@Component({
  selector: 'crm-admin-permissions-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="admin.permissions" />
      <p class="crm-muted">{{ 'admin.permissionsHint' | translate }}</p>

      @if (loading()) {
        <div class="crm-card crm-row">
          <mat-spinner diameter="28" />
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      } @else if (loadError()) {
        <div class="crm-card crm-stack">
          <p>{{ loadError() }}</p>
          <button mat-stroked-button type="button" (click)="reload()">{{ 'common.retry' | translate }}</button>
        </div>
      } @else if (matrix()) {
        <div class="perm-scroll">
          <table class="perm-matrix">
            <thead>
              <tr>
                <th>{{ 'admin.capability' | translate }}</th>
                @for (role of matrix()!.roles; track role) {
                  <th>{{ role | enumLabel: 'roles' }}</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (policy of matrix()!.policies; track policy) {
                <tr>
                  <td>{{ policy }}</td>
                  @for (role of matrix()!.roles; track role) {
                    <td>
                      <mat-checkbox
                        [checked]="isAllowed(policy, role)"
                        (change)="toggle(policy, role, $event.checked)"
                      />
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
        <button mat-flat-button color="primary" type="button" (click)="save()" [disabled]="saving()">
          {{ 'common.save' | translate }}
        </button>
      }
    </div>
  `,
  styles: `
    .perm-scroll {
      overflow: auto;
      max-height: 70vh;
      border: 1px solid var(--crm-border, #e5e7eb);
      background: var(--crm-surface, #fff);
    }
    .perm-matrix {
      border-collapse: collapse;
      min-width: 720px;
      width: 100%;
      font-size: 0.85rem;
    }
    .perm-matrix th,
    .perm-matrix td {
      border-bottom: 1px solid var(--crm-border, #e5e7eb);
      padding: 0.35rem 0.5rem;
      text-align: center;
      white-space: nowrap;
    }
    .perm-matrix th:first-child,
    .perm-matrix td:first-child {
      text-align: start;
      position: sticky;
      left: 0;
      background: var(--crm-surface, #fff);
      font-weight: 600;
      z-index: 1;
    }
    .crm-muted {
      opacity: 0.75;
      margin: 0;
    }
    .crm-row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
  `
})
export class AdminPermissionsPageComponent implements OnInit {
  private readonly api = inject(UsersService);
  private readonly toast = inject(ToastService);
  readonly matrix = signal<PermissionMatrix | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saving = signal(false);
  private dirty = new Map<string, boolean>();

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.matrix.set(null);
    this.api.permissionMatrix().subscribe({
      next: (m) => {
        this.dirty.clear();
        this.matrix.set(m);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        const status = err?.status as number | undefined;
        this.loadError.set(
          status === 404
            ? 'Permissions API not found. Point the app at the local API (proxy /api → :5088) and restart ng serve.'
            : status === 403
              ? 'You do not have permission to view the matrix.'
              : 'Failed to load the permissions matrix.'
        );
      }
    });
  }

  isAllowed(policy: string, role: string): boolean {
    const key = `${policy}|${role}`;
    if (this.dirty.has(key)) return this.dirty.get(key)!;
    const cell = this.matrix()?.cells.find((c) => c.policyName === policy && c.roleCode === role);
    return cell?.isAllowed ?? false;
  }

  toggle(policy: string, role: string, checked: boolean): void {
    this.dirty.set(`${policy}|${role}`, checked);
  }

  save(): void {
    const m = this.matrix();
    if (!m) return;
    const cells = m.policies.flatMap((policy) =>
      m.roles.map((role) => ({
        policyName: policy,
        roleCode: role,
        isAllowed: this.isAllowed(policy, role)
      }))
    );
    this.saving.set(true);
    this.api.savePermissionMatrix(cells).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('admin.saved');
        this.reload();
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to save permissions.');
      }
    });
  }
}
