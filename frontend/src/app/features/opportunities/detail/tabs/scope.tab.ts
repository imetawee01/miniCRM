import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ScopeItem, ScopeOfWork } from '../../../../core/models/opportunity';
import { ScopeService } from '../../../../core/services/scope.service';
import { LookupService } from '../../../../core/services/lookup.service';
import { ToastService } from '../../../../core/services/toast.service';
import { pickLocalized } from '../../../../core/utils/locale';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'crm-scope-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      <form class="crm-card" [formGroup]="briefForm" (ngSubmit)="saveBrief()">
        <mat-form-field appearance="outline" class="full">
          <mat-label>{{ 'scope.brief' | translate }}</mat-label>
          <textarea matInput rows="4" formControlName="brief"></textarea>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit">{{ 'common.save' | translate }}</button>
      </form>
      <form class="crm-card crm-grid crm-grid-2" [formGroup]="itemForm" (ngSubmit)="addItem()">
        <h3 class="crm-section-title">{{ 'scope.addItem' | translate }}</h3>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'scope.title' | translate }}</mat-label>
          <input matInput formControlName="title" required />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'scope.serviceLine' | translate }}</mat-label>
          <mat-select formControlName="serviceLineId" required>
            @for (s of lookups.serviceLines(); track s.id) {
              <mat-option [value]="s.id">{{ s.nameEn }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <button mat-stroked-button type="submit">{{ 'common.add' | translate }}</button>
      </form>
      @if (!(scope()?.items?.length)) {
        <crm-empty-state titleKey="scope.emptyTitle" messageKey="scope.emptyMessage" />
      } @else {
        <mat-list>
          @for (item of scope()?.items ?? []; track item.id) {
            <mat-list-item>
              <span matListItemTitle>{{ item.title }}</span>
              <span matListItemLine>{{ serviceLineLabel(item) }}</span>
              <button mat-icon-button type="button" (click)="remove(item)" [attr.aria-label]="'common.delete' | translate">
                <mat-icon>delete</mat-icon>
              </button>
            </mat-list-item>
          }
        </mat-list>
      }
    </div>
  `,
  styles: `.full { width: 100%; }`
})
export class ScopeTabComponent implements OnInit {
  private readonly api = inject(ScopeService);
  readonly lookups = inject(LookupService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(TranslateService);
  readonly scope = signal<ScopeOfWork | null>(null);
  readonly briefForm = new FormGroup({ brief: new FormControl('', { nonNullable: true }) });
  readonly itemForm = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    serviceLineId: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  private id(): string {
    const withId = [...this.route.pathFromRoot]
      .reverse()
      .find((r) => r.snapshot.paramMap.has('id'));
    return withId?.snapshot.paramMap.get('id') ?? '';
  }

  serviceLineLabel(item: ScopeItem): string {
    return pickLocalized(
      this.i18n.getCurrentLang(),
      item.serviceLineNameEn,
      item.serviceLineNameAr,
      item.serviceLineId
    );
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.get(this.id()).subscribe((s) => {
      this.scope.set(s);
      this.briefForm.patchValue({ brief: s.brief });
    });
  }

  saveBrief(): void {
    this.api.updateBrief(this.id(), this.briefForm.controls.brief.value).subscribe(() => {
      this.toast.success('scope.saved');
      this.reload();
    });
  }

  addItem(): void {
    if (this.itemForm.invalid) return;
    const v = this.itemForm.getRawValue();
    this.api.addItem(this.id(), { title: v.title, serviceLineId: v.serviceLineId }).subscribe(() => {
      this.toast.success('scope.itemAdded');
      this.itemForm.reset();
      this.reload();
    });
  }

  remove(item: ScopeItem): void {
    this.api.deleteItem(this.id(), item.id).subscribe(() => {
      this.toast.success('scope.itemRemoved');
      this.reload();
    });
  }
}
