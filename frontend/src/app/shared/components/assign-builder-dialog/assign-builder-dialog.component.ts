import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { UserPickItem } from '../../../core/models/user';
import { UsersService } from '../../../core/services/users.service';
import { MATERIAL_IMPORTS } from '../../material';
import { EnumLabelPipe } from '../../pipes/enum-label.pipe';

export interface AssignBuilderDialogResult {
  builderType: 'Presales' | 'ServiceLine';
  builderUserId: string;
}

@Component({
  selector: 'crm-assign-builder-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, EnumLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'proposal.assignTitle' | translate }}</h2>
    <mat-dialog-content>
      <p>{{ 'proposal.assignMessage' | translate }}</p>
      <form [formGroup]="form" class="crm-stack">
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'proposal.builderType' | translate }}</mat-label>
          <mat-select formControlName="builderType">
            <mat-option value="Presales">{{ 'Presales' | enumLabel }}</mat-option>
            <mat-option value="ServiceLine">{{ 'ServiceLine' | enumLabel }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'proposal.builderUserId' | translate }}</mat-label>
          <mat-select formControlName="builderUserId">
            @for (u of builders(); track u.id) {
              <mat-option [value]="u.id">{{ u.displayName }} ({{ u.email }})</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="ref.close(null)">{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid" (click)="submit()">
        {{ 'proposal.assign' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    p { margin: 0 0 0.75rem; color: var(--crm-muted); }
    .full { width: 100%; display: block; }
  `
})
export class AssignBuilderDialogComponent implements OnInit {
  readonly ref = inject(MatDialogRef<AssignBuilderDialogComponent, AssignBuilderDialogResult | null>);
  private readonly users = inject(UsersService);
  readonly builders = signal<UserPickItem[]>([]);
  readonly form = new FormGroup({
    builderType: new FormControl<'Presales' | 'ServiceLine'>('Presales', { nonNullable: true }),
    builderUserId: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  ngOnInit(): void {
    this.loadBuilders(this.form.controls.builderType.value);
    this.form.controls.builderType.valueChanges.subscribe((t) => {
      this.form.controls.builderUserId.setValue('');
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

  submit(): void {
    if (this.form.invalid) return;
    this.ref.close(this.form.getRawValue());
  }
}
