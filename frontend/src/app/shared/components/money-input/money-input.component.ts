import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-money-input',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field appearance="outline" class="full">
      <mat-label>{{ labelKey() | translate }}</mat-label>
      <input matInput type="number" min="0" step="0.01" [formControl]="control()" [required]="required()" />
      <span matTextSuffix>{{ 'common.sar' | translate }}</span>
    </mat-form-field>
  `,
  styles: `.full { width: 100%; }`
})
export class MoneyInputComponent {
  readonly control = input.required<FormControl<number | null>>();
  readonly labelKey = input('common.amount');
  readonly required = input(true);
}
