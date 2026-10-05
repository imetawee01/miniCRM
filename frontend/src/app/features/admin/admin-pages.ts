import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Customer } from '../../core/models/lookups';
import { RoleDto } from '../../core/models/user';
import { ServiceLineLookup } from '../../core/models/lookups';
import { EmailTemplate } from '../../core/models/contract';
import { AuditLogEntry } from '../../core/models/collaboration';
import { WorkflowGate } from '../../core/models/gate';
import { CustomersService } from '../../core/services/customers.service';
import { UsersService } from '../../core/services/users.service';
import { EmailsService } from '../../core/services/emails.service';
import { AuditService } from '../../core/services/audit.service';
import { GateService } from '../../core/services/gate.service';
import { LookupService } from '../../core/services/lookup.service';
import { ToastService } from '../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ActivityLogComponent } from '../../shared/components/activity-log/activity-log.component';
import { EnumLabelPipe } from '../../shared/pipes/enum-label.pipe';

@Component({
  selector: 'crm-admin-roles-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="admin.roles" />
      @for (r of roles(); track r.id) {
        <p>{{ r.code | enumLabel: 'roles' }} — {{ r.nameEn }} / {{ r.nameAr }}</p>
      }
    </div>
  `
})
export class AdminRolesPageComponent implements OnInit {
  private readonly api = inject(UsersService);
  readonly roles = signal<RoleDto[]>([]);
  ngOnInit(): void {
    this.api.roles().subscribe((r) => this.roles.set(r));
  }
}

@Component({
  selector: 'crm-admin-customers-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="admin.customers" />
      <form class="crm-card crm-grid crm-grid-2" [formGroup]="form" (ngSubmit)="create()">
        <mat-form-field appearance="outline"><mat-label>{{ 'admin.nameEn' | translate }}</mat-label><input matInput formControlName="nameEn" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'admin.nameAr' | translate }}</mat-label><input matInput formControlName="nameAr" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>{{ 'admin.sector' | translate }}</mat-label><input matInput formControlName="sector" /></mat-form-field>
        <mat-checkbox formControlName="isGovernment">{{ 'admin.isGovernment' | translate }}</mat-checkbox>
        <button mat-flat-button type="submit">{{ 'common.add' | translate }}</button>
      </form>
      @for (c of rows(); track c.id) {
        <p>{{ c.nameEn }} / {{ c.nameAr }}</p>
      }
    </div>
  `
})
export class AdminCustomersPageComponent implements OnInit {
  private readonly api = inject(CustomersService);
  private readonly toast = inject(ToastService);
  readonly rows = signal<Customer[]>([]);
  readonly form = new FormGroup({
    nameEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nameAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sector: new FormControl('', { nonNullable: true }),
    isGovernment: new FormControl(false, { nonNullable: true })
  });
  ngOnInit(): void {
    this.reload();
  }
  reload(): void {
    this.api.list({ page: 1, pageSize: 100 }).subscribe((r) => this.rows.set(r.items));
  }
  create(): void {
    this.api.create(this.form.getRawValue()).subscribe(() => {
      this.toast.success('admin.customerCreated');
      this.reload();
    });
  }
}

@Component({
  selector: 'crm-admin-service-lines-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="admin.serviceLines" subtitleKey="admin.serviceLinesHint" />
      <form class="crm-card crm-stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="row">
          <mat-form-field appearance="outline"><mat-label>{{ 'admin.code' | translate }}</mat-label><input matInput formControlName="code" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'admin.nameEn' | translate }}</mat-label><input matInput formControlName="nameEn" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'admin.nameAr' | translate }}</mat-label><input matInput formControlName="nameAr" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'admin.sortOrder' | translate }}</mat-label><input matInput type="number" formControlName="sortOrder" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'admin.colourHex' | translate }}</mat-label><input matInput formControlName="colourHex" placeholder="#0ea5e9" /></mat-form-field>
          <mat-checkbox formControlName="isActive">{{ 'admin.active' | translate }}</mat-checkbox>
        </div>
        <div class="crm-actions">
          <button mat-button type="button" (click)="resetForm()" [disabled]="!editingId()">{{ 'common.cancel' | translate }}</button>
          <button mat-flat-button color="primary" type="submit">{{ editingId() ? ('common.save' | translate) : ('common.add' | translate) }}</button>
        </div>
      </form>
      <table mat-table [dataSource]="rows()" class="crm-card full">
        <ng-container matColumnDef="code"><th mat-header-cell *matHeaderCellDef>{{ 'admin.code' | translate }}</th><td mat-cell *matCellDef="let s">{{ s.code }}</td></ng-container>
        <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>{{ 'admin.nameEn' | translate }}</th><td mat-cell *matCellDef="let s">{{ s.nameEn }} / {{ s.nameAr }}</td></ng-container>
        <ng-container matColumnDef="sort"><th mat-header-cell *matHeaderCellDef>{{ 'admin.sortOrder' | translate }}</th><td mat-cell *matCellDef="let s">{{ s.sortOrder }}</td></ng-container>
        <ng-container matColumnDef="active"><th mat-header-cell *matHeaderCellDef>{{ 'admin.active' | translate }}</th><td mat-cell *matCellDef="let s">{{ s.isActive ? '✓' : '—' }}</td></ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let s">
            <button mat-button type="button" (click)="edit(s)">{{ 'common.edit' | translate }}</button>
            @if (s.isActive) {
              <button mat-button type="button" color="warn" (click)="deactivate(s.id)">{{ 'admin.deactivate' | translate }}</button>
            }
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
      </table>
    </div>
  `,
  styles: `
    .row { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 0.75rem; align-items: center; }
    .full { width: 100%; }
    .crm-actions { display: flex; gap: 0.5rem; justify-content: flex-end; }
  `
})
export class AdminServiceLinesPageComponent implements OnInit {
  private readonly api = inject(UsersService);
  private readonly toast = inject(ToastService);
  readonly rows = signal<ServiceLineLookup[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly cols = ['code', 'name', 'sort', 'active', 'actions'];
  readonly form = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nameEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nameAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sortOrder: new FormControl(0, { nonNullable: true }),
    colourHex: new FormControl('', { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true })
  });
  ngOnInit(): void {
    this.reload();
  }
  reload(): void {
    this.api.serviceLines(true).subscribe((r) => this.rows.set(r));
  }
  edit(s: ServiceLineLookup): void {
    this.editingId.set(s.id);
    this.form.patchValue({
      code: s.code,
      nameEn: s.nameEn,
      nameAr: s.nameAr,
      sortOrder: s.sortOrder ?? 0,
      colourHex: s.colourHex ?? '',
      isActive: s.isActive !== false
    });
  }
  resetForm(): void {
    this.editingId.set(null);
    this.form.reset({ code: '', nameEn: '', nameAr: '', sortOrder: 0, colourHex: '', isActive: true });
  }
  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const id = this.editingId();
    const done = () => {
      this.toast.success('admin.saved');
      this.resetForm();
      this.reload();
    };
    if (id) {
      this.api
        .updateServiceLine(id, {
          code: v.code,
          nameEn: v.nameEn,
          nameAr: v.nameAr,
          sortOrder: v.sortOrder,
          colourHex: v.colourHex || null,
          isActive: v.isActive
        })
        .subscribe(done);
    } else {
      this.api
        .createServiceLine({
          code: v.code,
          nameEn: v.nameEn,
          nameAr: v.nameAr,
          sortOrder: v.sortOrder,
          colourHex: v.colourHex || null
        })
        .subscribe(done);
    }
  }
  deactivate(id: string): void {
    this.api.deactivateServiceLine(id).subscribe(() => {
      this.toast.success('admin.saved');
      this.reload();
    });
  }
}

@Component({
  selector: 'crm-admin-lookups-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="admin.lookups" />
      @for (s of lookups.stages(); track s.id) {
        <p>{{ s.code }} — {{ s.nameEn }} / {{ s.nameAr }}</p>
      }
    </div>
  `
})
export class AdminLookupsPageComponent {
  readonly lookups = inject(LookupService);
}

@Component({
  selector: 'crm-admin-email-templates-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page crm-stack">
      <crm-page-header titleKey="admin.emailTemplates" />
      @for (t of rows(); track t.code) {
        <form class="crm-card crm-stack" [formGroup]="formOf(t.code)" (ngSubmit)="save(t.code)">
          <h3>{{ t.nameEn }}</h3>
          <mat-form-field appearance="outline"><mat-label>{{ 'emails.subject' | translate }}</mat-label><input matInput formControlName="subjectTemplate" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'emails.body' | translate }}</mat-label><textarea matInput rows="6" formControlName="bodyTemplateHtml"></textarea></mat-form-field>
          <button mat-flat-button type="submit">{{ 'common.save' | translate }}</button>
        </form>
      }
    </div>
  `
})
export class AdminEmailTemplatesPageComponent implements OnInit {
  private readonly api = inject(EmailsService);
  private readonly toast = inject(ToastService);
  readonly rows = signal<EmailTemplate[]>([]);
  forms: Record<string, FormGroup> = {};
  ngOnInit(): void {
    this.api.templates().subscribe((rows) => {
      this.rows.set(rows);
      for (const t of rows) {
        this.forms[t.code] = new FormGroup({
          subjectTemplate: new FormControl(t.subjectTemplate, { nonNullable: true }),
          bodyTemplateHtml: new FormControl(t.bodyTemplateHtml, { nonNullable: true }),
          defaultTo: new FormControl(t.defaultTo, { nonNullable: true }),
          defaultCc: new FormControl(t.defaultCc, { nonNullable: true })
        });
      }
    });
  }
  save(code: string): void {
    this.api.updateTemplate(code, this.formOf(code).getRawValue()).subscribe(() => this.toast.success('admin.saved'));
  }

  formOf(code: string): FormGroup {
    return this.forms[code] ?? new FormGroup({});
  }
}

@Component({
  selector: 'crm-admin-workflow-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="admin.workflow" />
      @for (g of gates(); track g.id) {
        <p>{{ g.code }} — {{ g.nameEn }} ({{ g.responsibleRoleCode }}) {{ g.allowedDecisions }}</p>
      }
    </div>
  `
})
export class AdminWorkflowPageComponent implements OnInit {
  private readonly api = inject(GateService);
  readonly gates = signal<WorkflowGate[]>([]);
  ngOnInit(): void {
    this.api.workflowGates().subscribe((g) => this.gates.set(g));
  }
}

@Component({
  selector: 'crm-admin-audit-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, PageHeaderComponent, ActivityLogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="admin.audit" />
      <crm-activity-log />
    </div>
  `
})
export class AdminAuditPageComponent {
  readonly extra = signal<AuditLogEntry[]>([]);
}
