import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import {
  ATTACHMENT_CATEGORIES,
  ENGAGEMENT_TYPES,
  OPPORTUNITY_TYPES,
  PROPOSAL_LANGUAGES,
  SOURCE_CHANNELS,
  SUBMISSION_THEMES,
  AttachmentCategory,
  SourceChannel,
  SubmissionTheme
} from '../../../core/models/enums';
import { CreateOpportunityRequest } from '../../../core/models/opportunity';
import { ServiceLineLookup } from '../../../core/models/lookups';
import { OpportunityService } from '../../../core/services/opportunity.service';
import { AttachmentsService } from '../../../core/services/attachments.service';
import { UsersService } from '../../../core/services/users.service';
import { ToastService } from '../../../core/services/toast.service';
import { CanComponentDeactivate } from '../../../core/auth/unsaved-changes.guard';
import { positiveMoney, requiredIf, scoreRange } from '../../../core/utils/validators';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { MoneyInputComponent } from '../../../shared/components/money-input/money-input.component';
import { ScoreSelectorComponent } from '../../../shared/components/score-selector/score-selector.component';
import { CustomerPickerComponent } from '../../../shared/components/customer-picker/customer-picker.component';
import { EnumLabelPipe } from '../../../shared/pipes/enum-label.pipe';

interface StagedFile {
  file: File;
  description: string;
  category: AttachmentCategory;
  status: 'pending' | 'uploading' | 'done' | 'error';
  error?: string;
}

@Component({
  selector: 'crm-opportunity-create-page',
  standalone: true,
  imports: [
    ...MATERIAL_IMPORTS,
    FormsModule,
    ReactiveFormsModule,
    DecimalPipe,
    PageHeaderComponent,
    MoneyInputComponent,
    ScoreSelectorComponent,
    CustomerPickerComponent,
    EnumLabelPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-page">
      <crm-page-header titleKey="opp.create" subtitleKey="opp.createWizardHint" />

      <mat-stepper [linear]="true" #stepper class="wizard">
        <mat-step [stepControl]="details" [label]="'opp.stepDetails' | translate">
          <form [formGroup]="details" class="crm-stack crm-card">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.name' | translate }}</mat-label>
              <input matInput formControlName="name" />
            </mat-form-field>

            <crm-customer-picker [control]="details.controls.customerId" />

            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.sourceChannel' | translate }}</mat-label>
              <mat-select formControlName="sourceChannel">
                @for (v of sources; track v) {
                  <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            @if (details.controls.sourceChannel.value === 'Other') {
              <mat-form-field appearance="outline">
                <mat-label>{{ 'opp.sourceChannelOther' | translate }}</mat-label>
                <input matInput formControlName="sourceChannelOther" />
              </mat-form-field>
            }

            <div class="field-block">
              <span class="label">{{ 'opp.submissionTheme' | translate }}</span>
              <mat-chip-listbox formControlName="submissionTheme" aria-label="Submission theme">
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
              <mat-select formControlName="engagementType">
                @for (v of engagements; track v) {
                  <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.opportunityType' | translate }}</mat-label>
              <mat-select formControlName="opportunityType">
                @for (v of types; track v) {
                  <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <crm-money-input [control]="details.controls.expectedValueSar" labelKey="opp.expectedValue" />
            <crm-score-selector [control]="details.controls.relationWithClientScore" labelKey="opp.relationScore" />
            <crm-score-selector [control]="details.controls.winProbabilityScore" labelKey="opp.winScore" />

            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.durationMonths' | translate }}</mat-label>
              <input matInput type="number" formControlName="durationMonths" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.proposalLanguage' | translate }}</mat-label>
              <mat-select formControlName="proposalLanguage">
                @for (v of languages; track v) {
                  <mat-option [value]="v">{{ v | enumLabel }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-checkbox formControlName="requiresBidBond">{{ 'opp.requiresBidBond' | translate }}</mat-checkbox>

            <div class="crm-actions">
              <button mat-flat-button color="primary" type="button" matStepperNext [disabled]="details.invalid">
                {{ 'common.next' | translate }}
              </button>
            </div>
          </form>
        </mat-step>

        <mat-step [stepControl]="scope" [label]="'opp.stepScope' | translate">
          <form [formGroup]="scope" class="crm-stack crm-card">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'opp.scopeBrief' | translate }}</mat-label>
              <textarea matInput rows="5" formControlName="scopeBrief"></textarea>
              <mat-hint>{{ 'opp.scopeBriefHint' | translate }}</mat-hint>
            </mat-form-field>

            <div class="items-head">
              <h3>{{ 'opp.scopeItems' | translate }}</h3>
              <button mat-stroked-button type="button" (click)="addScopeItem()">
                <mat-icon>add</mat-icon> {{ 'opp.addScopeItem' | translate }}
              </button>
            </div>

            <div formArrayName="items" class="crm-stack">
              @for (item of scopeItems.controls; track $index; let i = $index) {
                <div class="scope-item" [formGroupName]="i">
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'opp.scopeItemTitle' | translate }}</mat-label>
                    <input matInput formControlName="title" />
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'opp.serviceLine' | translate }}</mat-label>
                    <mat-select formControlName="serviceLineId">
                      @for (sl of serviceLines(); track sl.id) {
                        <mat-option [value]="sl.id">{{ sl.nameEn }} / {{ sl.nameAr }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'opp.scopeItemComment' | translate }}</mat-label>
                    <input matInput formControlName="comment" />
                  </mat-form-field>
                  <button mat-icon-button type="button" (click)="removeScopeItem(i)" [disabled]="scopeItems.length <= 1">
                    <mat-icon>delete</mat-icon>
                  </button>
                </div>
              }
            </div>

            <div class="attach-block">
              <h3>{{ 'opp.attachmentsAtCreate' | translate }}</h3>
              <p class="muted">{{ 'opp.attachmentsAtCreateHint' | translate }}</p>
              <div class="file-row">
                <input type="file" #fileInput multiple (change)="onFilesSelected($event)" />
              </div>
              @for (f of staged(); track f.file.name + $index; let i = $index) {
                <div class="staged">
                  <div class="staged__meta">
                    <strong>{{ f.file.name }}</strong>
                    <span class="muted">({{ f.file.size | number }} bytes)</span>
                    <span class="status" [class]="f.status">{{ f.status }}</span>
                  </div>
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'files.description' | translate }}</mat-label>
                    <input matInput [ngModel]="f.description" (ngModelChange)="updateStaged(i, { description: $event })" [ngModelOptions]="{ standalone: true }" />
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'files.category' | translate }}</mat-label>
                    <mat-select [ngModel]="f.category" (ngModelChange)="updateStaged(i, { category: $event })" [ngModelOptions]="{ standalone: true }">
                      @for (c of categories; track c) {
                        <mat-option [value]="c">{{ c | enumLabel }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                  <button mat-icon-button type="button" (click)="removeStaged(i)"><mat-icon>close</mat-icon></button>
                </div>
              }
            </div>

            <div class="crm-actions">
              <button mat-button type="button" matStepperPrevious>{{ 'common.back' | translate }}</button>
              <button mat-flat-button color="primary" type="button" matStepperNext [disabled]="scope.invalid || !attachmentsValid()">
                {{ 'common.next' | translate }}
              </button>
            </div>
          </form>
        </mat-step>

        <mat-step [label]="'opp.stepReview' | translate">
          <div class="crm-card crm-stack review">
            <h3>{{ 'opp.reviewTitle' | translate }}</h3>
            <p><strong>{{ 'opp.name' | translate }}:</strong> {{ details.controls.name.value }}</p>
            <p><strong>{{ 'opp.submissionTheme' | translate }}:</strong> {{ details.controls.submissionTheme.value | enumLabel }}</p>
            <p><strong>{{ 'opp.expectedValue' | translate }}:</strong> {{ details.controls.expectedValueSar.value | number }} SAR</p>
            <p><strong>{{ 'opp.scopeBrief' | translate }}:</strong> {{ scope.controls.scopeBrief.value }}</p>
            <p><strong>{{ 'opp.scopeItems' | translate }}:</strong> {{ scopeItems.length }}</p>
            <p><strong>{{ 'opp.attachmentsAtCreate' | translate }}:</strong> {{ staged().length }}</p>

            @if (uploadErrors().length) {
              <div class="errors">
                <p>{{ 'opp.uploadPartialFail' | translate }}</p>
                <ul>
                  @for (e of uploadErrors(); track e) {
                    <li>{{ e }}</li>
                  }
                </ul>
                <button mat-stroked-button type="button" [disabled]="busy()" (click)="retryFailedUploads()">
                  {{ 'opp.retryUploads' | translate }}
                </button>
              </div>
            }

            <div class="crm-actions">
              <button mat-button type="button" matStepperPrevious [disabled]="busy()">{{ 'common.back' | translate }}</button>
              <button mat-flat-button color="primary" type="button" [disabled]="busy() || details.invalid || scope.invalid" (click)="create()">
                @if (busy()) {
                  <mat-spinner diameter="18" />
                } @else {
                  {{ 'opp.create' | translate }}
                }
              </button>
            </div>
          </div>
        </mat-step>
      </mat-stepper>
    </div>
  `,
  styles: `
    .wizard { background: transparent; }
    .field-block { margin-bottom: 1rem; }
    .field-block .label { display: block; font-size: 0.8rem; color: var(--crm-muted); margin-bottom: 0.35rem; }
    mat-chip-option { height: auto !important; padding: 0.5rem 0.75rem; }
    mat-chip-option small { display: block; font-weight: 400; opacity: 0.85; max-width: 220px; white-space: normal; }
    .items-head { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
    .scope-item { display: grid; grid-template-columns: 1.2fr 1fr 1fr auto; gap: 0.5rem; align-items: start; }
    .attach-block { border-top: 1px solid var(--crm-border); padding-top: 1rem; }
    .muted { color: var(--crm-muted); font-size: 0.85rem; }
    .staged { display: grid; grid-template-columns: 1fr 1fr 1fr auto; gap: 0.5rem; align-items: start; padding: 0.5rem 0; border-bottom: 1px dashed var(--crm-border); }
    .staged__meta { grid-column: 1 / -1; display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    .status.uploading { color: #0284c7; }
    .status.done { color: #15803d; }
    .status.error { color: #b91c1c; }
    .errors { background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; padding: 0.75rem 1rem; }
    .crm-actions { display: flex; gap: 0.75rem; justify-content: flex-end; margin-top: 1rem; }
    @media (max-width: 900px) {
      .scope-item, .staged { grid-template-columns: 1fr; }
    }
  `
})
export class OpportunityCreatePageComponent implements OnInit, CanComponentDeactivate {
  private readonly api = inject(OpportunityService);
  private readonly attachments = inject(AttachmentsService);
  private readonly users = inject(UsersService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly sources = SOURCE_CHANNELS;
  readonly themes = SUBMISSION_THEMES;
  readonly engagements = ENGAGEMENT_TYPES;
  readonly types = OPPORTUNITY_TYPES;
  readonly languages = PROPOSAL_LANGUAGES;
  readonly categories = ATTACHMENT_CATEGORIES;

  readonly serviceLines = signal<ServiceLineLookup[]>([]);
  readonly staged = signal<StagedFile[]>([]);
  readonly busy = signal(false);
  readonly uploadErrors = signal<string[]>([]);
  private createdOppId: string | null = null;

  readonly details = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    customerId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    sourceChannel: new FormControl<SourceChannel>('DirectClient', { nonNullable: true, validators: [Validators.required] }),
    sourceChannelOther: new FormControl('', { nonNullable: true }),
    submissionTheme: new FormControl<SubmissionTheme>('Emdad', { nonNullable: true, validators: [Validators.required] }),
    engagementType: new FormControl<(typeof ENGAGEMENT_TYPES)[number]>('Proactive', { nonNullable: true }),
    opportunityType: new FormControl<(typeof OPPORTUNITY_TYPES)[number]>('New', { nonNullable: true }),
    expectedValueSar: new FormControl<number | null>(null, { validators: [Validators.required, positiveMoney()] }),
    relationWithClientScore: new FormControl(3, { nonNullable: true, validators: [scoreRange()] }),
    winProbabilityScore: new FormControl(3, { nonNullable: true, validators: [scoreRange()] }),
    durationMonths: new FormControl<number | null>(null),
    proposalLanguage: new FormControl<(typeof PROPOSAL_LANGUAGES)[number]>('Arabic', { nonNullable: true }),
    requiresBidBond: new FormControl(false, { nonNullable: true })
  });

  readonly scope = new FormGroup({
    scopeBrief: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(20)] }),
    items: new FormArray([this.newScopeItem()])
  });

  get scopeItems(): FormArray<FormGroup> {
    return this.scope.controls.items as FormArray<FormGroup>;
  }

  ngOnInit(): void {
    this.details.controls.sourceChannelOther.setValidators([
      requiredIf(() => this.details.controls.sourceChannel.value === 'Other')
    ]);
    this.details.controls.sourceChannel.valueChanges.subscribe(() =>
      this.details.controls.sourceChannelOther.updateValueAndValidity()
    );
    this.users.serviceLines().subscribe((rows) => this.serviceLines.set(rows));
  }

  canDeactivate(): boolean {
    if (this.busy()) return false;
    return !(this.details.dirty || this.scope.dirty || this.staged().length > 0);
  }

  newScopeItem(): FormGroup {
    return new FormGroup({
      title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      serviceLineId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      comment: new FormControl('', { nonNullable: true })
    });
  }

  addScopeItem(): void {
    this.scopeItems.push(this.newScopeItem());
  }

  removeScopeItem(i: number): void {
    if (this.scopeItems.length <= 1) return;
    this.scopeItems.removeAt(i);
  }

  onFilesSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (!files.length) return;
    this.staged.update((list) => [
      ...list,
      ...files.map((file) => ({
        file,
        description: file.name,
        category: 'RFP' as AttachmentCategory,
        status: 'pending' as const
      }))
    ]);
    input.value = '';
  }

  updateStaged(i: number, patch: Partial<StagedFile>): void {
    this.staged.update((list) => list.map((f, idx) => (idx === i ? { ...f, ...patch } : f)));
  }

  removeStaged(i: number): void {
    this.staged.update((list) => list.filter((_, idx) => idx !== i));
  }

  attachmentsValid(): boolean {
    return this.staged().every((f) => f.description.trim().length > 0 && !!f.category);
  }

  create(): void {
    if (this.details.invalid || this.scope.invalid || !this.attachmentsValid()) {
      this.details.markAllAsTouched();
      this.scope.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.uploadErrors.set([]);
    const d = this.details.getRawValue();
    const s = this.scope.getRawValue();
    const body: CreateOpportunityRequest = {
      name: d.name,
      customerId: d.customerId,
      sourceChannel: d.sourceChannel,
      sourceChannelOther: d.sourceChannel === 'Other' ? d.sourceChannelOther : null,
      submissionTheme: d.submissionTheme,
      engagementType: d.engagementType,
      opportunityType: d.opportunityType,
      expectedValueSar: Number(d.expectedValueSar),
      relationWithClientScore: d.relationWithClientScore,
      winProbabilityScore: d.winProbabilityScore,
      durationMonths: d.durationMonths,
      proposalLanguage: d.proposalLanguage,
      requiresBidBond: d.requiresBidBond,
      scopeBrief: s.scopeBrief.trim(),
      scopeItems: (s.items as Array<{ title: string; serviceLineId: string; comment: string }>).map((i) => ({
        title: i.title,
        serviceLineId: i.serviceLineId,
        comment: i.comment || null
      }))
    };

    this.api.create(body).subscribe({
      next: (opp) => {
        this.createdOppId = opp.id;
        this.details.markAsPristine();
        this.scope.markAsPristine();
        if (!this.staged().length) {
          this.toast.success('opp.created');
          void this.router.navigate(['/opportunities', opp.id, 'overview']);
          return;
        }
        this.uploadAll(opp.id, false);
      },
      error: () => this.busy.set(false)
    });
  }

  retryFailedUploads(): void {
    if (!this.createdOppId) return;
    this.busy.set(true);
    this.uploadAll(this.createdOppId, true);
  }

  private uploadAll(oppId: string, retryOnly: boolean): void {
    const list = this.staged().map((f, i) => ({ f, i }));
    const targets = retryOnly ? list.filter((x) => x.f.status === 'error' || x.f.status === 'pending') : list;

    targets.forEach(({ i }) => this.updateStaged(i, { status: 'uploading', error: undefined }));

    const calls = targets.map(({ f, i }) =>
      this.attachments.upload('opportunities', oppId, f.file, f.description, f.category).pipe(
        catchError((err) => {
          const msg = err?.error?.detail || err?.message || f.file.name;
          this.updateStaged(i, { status: 'error', error: String(msg) });
          return of(null);
        })
      )
    );

    forkJoin(calls.length ? calls : [of(null)])
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe((results) => {
        results.forEach((res, idx) => {
          if (res != null) {
            const i = targets[idx].i;
            this.updateStaged(i, { status: 'done' });
          }
        });
        const failed = this.staged().filter((f) => f.status === 'error');
        if (failed.length) {
          this.uploadErrors.set(failed.map((f) => `${f.file.name}: ${f.error || 'failed'}`));
          this.toast.error('opp.uploadPartialFail');
          return;
        }
        this.toast.success('opp.created');
        void this.router.navigate(['/opportunities', oppId, 'overview']);
      });
  }
}
