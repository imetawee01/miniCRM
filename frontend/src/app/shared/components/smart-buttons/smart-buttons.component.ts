import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SmartButtons } from '../../../core/services/chatter.service';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'crm-smart-buttons',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (buttons(); as b) {
      <div class="smart" role="navigation">
        <a mat-stroked-button [routerLink]="[base(), 'attachments']">
          <mat-icon>attach_file</mat-icon>
          {{ 'opp.tabs.attachments' | translate }}
          <span class="count">{{ b.attachments }}</span>
        </a>
        <a mat-stroked-button [routerLink]="[base(), 'discussion']">
          <mat-icon>forum</mat-icon>
          {{ 'chatter.title' | translate }}
          <span class="count">{{ b.notes + b.comments }}</span>
        </a>
        <a mat-stroked-button [routerLink]="[base(), 'emails']">
          <mat-icon>email</mat-icon>
          {{ 'opp.tabs.emails' | translate }}
          <span class="count">{{ b.emails }}</span>
        </a>
        <a mat-stroked-button [routerLink]="[base(), 'approvals']">
          <mat-icon>how_to_reg</mat-icon>
          {{ 'opp.tabs.approvals' | translate }}
          <span class="count">{{ b.approvals }}</span>
        </a>
        <a mat-stroked-button [routerLink]="[base(), 'scope']">
          <mat-icon>list_alt</mat-icon>
          {{ 'opp.tabs.scope' | translate }}
          <span class="count">{{ b.scopeItems }}</span>
        </a>
        <a mat-stroked-button [routerLink]="[base(), 'discussion']" [queryParams]="{ tab: 'activities' }">
          <mat-icon>event</mat-icon>
          {{ 'activity.scheduleTitle' | translate }}
          <span class="count">{{ b.activities }}</span>
        </a>
        @if (b.contractId) {
          <a mat-stroked-button [routerLink]="['/contracting', b.contractId]">
            <mat-icon>handshake</mat-icon>
            {{ 'opp.tabs.contract' | translate }}
          </a>
        }
      </div>
    }
  `,
  styles: `
    .smart { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 0.75rem; }
    .smart a { display: inline-flex; align-items: center; gap: 0.35rem; }
    .count {
      display: inline-grid; place-items: center; min-width: 1.4rem; height: 1.4rem;
      padding: 0 0.3rem; border-radius: 999px; font-size: 0.72rem; font-weight: 700;
      background: color-mix(in srgb, var(--crm-teal) 18%, transparent); color: var(--crm-navy);
    }
    mat-icon { font-size: 18px; width: 18px; height: 18px; }
  `
})
export class SmartButtonsComponent {
  readonly opportunityId = input.required<string>();
  readonly buttons = input<SmartButtons | null>(null);
  readonly base = () => `/opportunities/${this.opportunityId()}`;
}
