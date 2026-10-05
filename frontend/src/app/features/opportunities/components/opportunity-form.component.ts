import { Component, OnInit, inject, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  ENGAGEMENT_TYPES,
  OPPORTUNITY_TYPES,
  PROPOSAL_LANGUAGES,
  SOURCE_CHANNELS,
  SUBMISSION_THEMES,
  SourceChannel
} from '../../../core/models/enums';
import { CreateOpportunityRequest, OpportunityDetail } from '../../../core/models/opportunity';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { EnumLabelPipe } from '../../../shared/pipes/enum-label.pipe';
import { MoneyInputComponent } from '../../../shared/components/money-input/money-input.component';
import { ScoreSelectorComponent } from '../../../shared/components/score-selector/score-selector.component';
import { CustomerPickerComponent } from '../../../shared/components/customer-picker/customer-picker.component';
import { positiveMoney, requiredIf, scoreRange } from '../../../core/utils/validators';

/** Edit form (create uses the wizard on opportunity-create.page). */
@Component({
  selector: 'crm-opportunity-form',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    ReactiveFormsModule,
    EnumLabelPipe,
    MoneyInputComponent,
    ScoreSelectorComponent,
    CustomerPickerComponent
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="crm-stack">
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.name' | translate }}</mat-label>
        <input matInput formControlName="name" required />
      </mat-form-field>
      <crm-customer-picker [control]="form.controls.customerId" />
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.sourceChannel' | translate }}</mat-label>
        <mat-select formControlName="sourceChannel" required>
          @for (v of sources; track v) {
            <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      @if (form.controls.sourceChannel.value === 'Other') {
        <mat-form-field appearance="outline">
          <mat-label>{{ 'opp.sourceChannelOther' | translate }}</mat-label>
          <input matInput formControlName="sourceChannelOther" />
        </mat-form-field>
      }
      <div class="field-block">
        <span class="label">{{ 'opp.submissionTheme' | translate }}</span>
        <mat-chip-listbox formControlName="submissionTheme">
          @for (v of themes; track v) {
            <mat-chip-option [value]="v">
              <strong>{{ v | enumLabel }}</strong>
              <small>{{ ('opp.themeDesc.' + v) | translate }}</small>
            </mat-chip-option>
          }
        </mat-chip-listbox>
      </div>
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.engagementType' | translate }}</mat-label>
        <mat-select formControlName="engagementType" required>
          @for (v of engagements; track v) {
            <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.opportunityType' | translate }}</mat-label>
        <mat-select formControlName="opportunityType" required>
          @for (v of types; track v) {
            <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <crm-money-input [control]="form.controls.expectedValueSar" labelKey="opp.expectedValue" />
      <crm-score-selector [control]="form.controls.relationWithClientScore" labelKey="opp.relationScore" />
      <crm-score-selector [control]="form.controls.winProbabilityScore" labelKey="opp.winScore" />
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.durationMonths' | translate }}</mat-label>
        <input matInput type="number" formControlName="durationMonths" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.proposalLanguage' | translate }}</mat-label>
        <mat-select formControlName="proposalLanguage" required>
          @for (v of languages; track v) {
            <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-checkbox formControlName="requiresBidBond">{{ 'opp.requiresBidBond' | translate }}</mat-checkbox>
      <mat-form-field appearance="outline">
        <mat-label>{{ 'opp.scopeBrief' | translate }}</mat-label>
        <textarea matInput rows="4" formControlName="scopeBrief"></textarea>
        <mat-hint>{{ 'opp.scopeBriefHint' | translate }}</mat-hint>
      </mat-form-field>
      <div class="crm-actions">
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          {{ 'common.save' | translate }}
        </button>
      </div>
    </form>
  `,
  styles: `
    .field-block { margin-bottom: 1rem; }
    .field-block .label { display: block; font-size: 0.8rem; color: var(--crm-muted); margin-bottom: 0.35rem; }
    mat-chip-option { height: auto !important; padding: 0.5rem 0.75rem; }
    mat-chip-option small { display: block; font-weight: 400; opacity: 0.85; max-width: 220px; white-space: normal; }
  `
})
export class OpportunityFormComponent implements OnInit {
  readonly value = input<OpportunityDetail | null>(null);
  readonly saved = output<CreateOpportunityRequest>();
  readonly sources = SOURCE_CHANNELS;
  readonly themes = SUBMISSION_THEMES;
  readonly engagements = ENGAGEMENT_TYPES;
  readonly types = OPPORTUNITY_TYPES;
  readonly languages = PROPOSAL_LANGUAGES;
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    customerId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sourceChannel: new FormControl<SourceChannel>('DirectClient', { nonNullable: true, validators: [Validators.required] }),
    sourceChannelOther: new FormControl('', { nonNullable: true }),
    submissionTheme: new FormControl<(typeof SUBMISSION_THEMES)[number]>('Emdad', { nonNullable: true }),
    engagementType: new FormControl<(typeof ENGAGEMENT_TYPES)[number]>('Proactive', { nonNullable: true }),
    opportunityType: new FormControl<(typeof OPPORTUNITY_TYPES)[number]>('New', { nonNullable: true }),
    expectedValueSar: new FormControl<number | null>(null, { validators: [positiveMoney()] }),
    relationWithClientScore: new FormControl(3, { nonNullable: true, validators: [scoreRange()] }),
    winProbabilityScore: new FormControl(3, { nonNullable: true, validators: [scoreRange()] }),
    durationMonths: new FormControl<number | null>(null),
    proposalLanguage: new FormControl<(typeof PROPOSAL_LANGUAGES)[number]>('Arabic', { nonNullable: true }),
    requiresBidBond: new FormControl(false, { nonNullable: true }),
    scopeBrief: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(20)] })
  });

  ngOnInit(): void {
    this.form.controls.sourceChannelOther.setValidators([
      requiredIf(() => this.form.controls.sourceChannel.value === 'Other')
    ]);
    this.form.controls.sourceChannel.valueChanges.subscribe(() =>
      this.form.controls.sourceChannelOther.updateValueAndValidity()
    );
    const v = this.value();
    if (v) {
      this.form.patchValue({
        name: v.name,
        customerId: v.customerId,
        sourceChannel: v.sourceChannel,
        sourceChannelOther: v.sourceChannelOther ?? '',
        submissionTheme: v.submissionTheme,
        engagementType: v.engagementType,
        opportunityType: v.opportunityType,
        expectedValueSar: v.expectedValueSar,
        relationWithClientScore: v.relationWithClientScore,
        winProbabilityScore: v.winProbabilityScore,
        durationMonths: v.durationMonths ?? null,
        proposalLanguage: v.proposalLanguage,
        requiresBidBond: v.requiresBidBond,
        scopeBrief: v.scopeBrief ?? ''
      });
    }
  }

  get dirty(): boolean {
    return this.form.dirty;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.saved.emit({
      name: v.name,
      customerId: v.customerId,
      sourceChannel: v.sourceChannel,
      sourceChannelOther: v.sourceChannel === 'Other' ? v.sourceChannelOther : null,
      submissionTheme: v.submissionTheme,
      engagementType: v.engagementType,
      opportunityType: v.opportunityType,
      expectedValueSar: Number(v.expectedValueSar),
      relationWithClientScore: v.relationWithClientScore,
      winProbabilityScore: v.winProbabilityScore,
      durationMonths: v.durationMonths,
      proposalLanguage: v.proposalLanguage,
      requiresBidBond: v.requiresBidBond,
      scopeBrief: v.scopeBrief?.trim() || null
    });
  }
}
