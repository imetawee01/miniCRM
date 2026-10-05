import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MetaField } from '../../../core/query/domain.models';
import { MATERIAL_IMPORTS } from '../../../shared/material';

@Component({
  selector: 'crm-export-wizard-dialog',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ 'opp.exportWizard' | translate }}</h2>
    <mat-dialog-content class="crm-stack">
      <p>{{ 'opp.exportWizardHint' | translate }}</p>
      <div class="fields">
        @for (f of data.fields; track f.name) {
          <mat-checkbox [checked]="selected.has(f.name)" (change)="toggle(f.name, $event.checked)">
            {{ f.labelEn }}
          </mat-checkbox>
        }
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="primary" type="button" (click)="confirm()" [disabled]="!selected.size">
        {{ 'common.export' | translate }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .fields {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.25rem 1rem;
      max-height: 360px;
      overflow: auto;
    }
  `
})
export class ExportWizardDialogComponent {
  readonly data = inject<{ fields: MetaField[] }>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ExportWizardDialogComponent, string[]>);
  readonly selected = new Set(this.data.fields.map((f) => f.name));

  toggle(name: string, checked: boolean): void {
    if (checked) this.selected.add(name);
    else this.selected.delete(name);
  }

  confirm(): void {
    this.ref.close([...this.selected]);
  }
}
