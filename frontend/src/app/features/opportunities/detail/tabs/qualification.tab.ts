import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { QualificationMeeting } from '../../../../core/models/contract';
import { QualificationService } from '../../../../core/services/qualification.service';
import { OpportunityService } from '../../../../core/services/opportunity.service';
import { ToastService } from '../../../../core/services/toast.service';
import { MATERIAL_IMPORTS } from '../../../../shared/material';

@Component({
  selector: 'crm-qualification-tab',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="crm-stack">
      <form class="crm-card" [formGroup]="routeForm" (ngSubmit)="setRoute()">
        <h3 class="crm-section-title">{{ 'qual.route' | translate }}</h3>
        <mat-slide-toggle formControlName="requiresQualificationMeeting">
          {{ 'qual.requiresMeeting' | translate }}
        </mat-slide-toggle>
        <button mat-stroked-button type="submit">{{ 'common.save' | translate }}</button>
      </form>
      <div class="crm-actions">
        <a mat-button [routerLink]="['/qualification', id(), 'gw1-review']">{{ 'qual.gw1' | translate }}</a>
        <a mat-button [routerLink]="['/qualification', id(), 'decision']">{{ 'qual.decision' | translate }}</a>
        <a mat-button [routerLink]="['/qualification', id(), 'meeting']">{{ 'qual.meeting' | translate }}</a>
      </div>
      @if (meeting(); as m) {
        <section class="crm-card">
          <p>{{ 'qual.outcome' | translate }}: {{ m.outcome }}</p>
          <p>{{ m.agenda }}</p>
        </section>
      }
    </div>
  `
})
export class QualificationTabComponent implements OnInit {
  private readonly api = inject(QualificationService);
  private readonly opportunities = inject(OpportunityService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly meeting = signal<QualificationMeeting | null>(null);
  readonly routeForm = new FormGroup({
    requiresQualificationMeeting: new FormControl(false, { nonNullable: true })
  });

  id(): string {
    return this.route.parent?.snapshot.paramMap.get('id') ?? '';
  }

  ngOnInit(): void {
    this.opportunities.get(this.id()).subscribe((opp) => {
      this.routeForm.patchValue({
        requiresQualificationMeeting: !!opp.requiresQualificationMeeting
      });
      this.routeForm.markAsPristine();
    });

    this.api.getMeeting(this.id()).subscribe({
      next: (m) => this.meeting.set(m),
      error: () => this.meeting.set(null)
    });
  }

  setRoute(): void {
    this.api.setRoute(this.id(), this.routeForm.getRawValue()).subscribe(() => {
      this.toast.success('qual.routeSaved');
      this.opportunities.get(this.id()).subscribe((opp) => {
        this.routeForm.patchValue({
          requiresQualificationMeeting: !!opp.requiresQualificationMeeting
        });
        this.routeForm.markAsPristine();
      });
    });
  }
}
