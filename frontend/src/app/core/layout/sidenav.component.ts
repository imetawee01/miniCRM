import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthStore } from '../auth/auth.store';
import { GateService } from '../services/gate.service';
import { ProposalService } from '../services/proposal.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
interface NavItem {
  path: string;
  labelKey: string;
  icon: string;
  roles?: string[];
  badge?: () => number;
}

interface NavGroup {
  labelKey: string;
  items: NavItem[];
}

@Component({
  selector: 'crm-sidenav',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="rail" [attr.aria-label]="'nav.main' | translate">
      <div class="brand">
        <span class="brand__mark" aria-hidden="true">
          <mat-icon>hub</mat-icon>
        </span>
        <span class="brand__text">
          <span class="brand__name">{{ 'app.title' | translate }}</span>
          @if (primaryRole(); as role) {
            <span class="brand__role">{{ roleLabelKey(role) | translate }}</span>
          }
        </span>
      </div>

      <div class="rail__scroll">
        @for (group of groups(); track group.labelKey) {
          <div class="group">
            <p class="group__label">{{ group.labelKey | translate }}</p>
            <ul class="group__list">
              @for (item of group.items; track item.path) {
                <li>
                  <a
                    class="item"
                    [routerLink]="item.path"
                    routerLinkActive="item--active"
                    [routerLinkActiveOptions]="{ exact: item.path === '/dashboard' }"
                  >
                    <mat-icon class="item__icon">{{ item.icon }}</mat-icon>
                    <span class="item__label">{{ item.labelKey | translate }}</span>
                    @if (item.badge && item.badge()) {
                      <span class="item__badge">{{ item.badge() }}</span>
                    }
                  </a>
                </li>
              }
            </ul>
          </div>
        }
      </div>
    </nav>
  `,
  styles: `
    :host {
      display: block;
      width: var(--crm-sidenav-width);
      height: 100%;
    }

    .rail {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: linear-gradient(180deg, var(--crm-navy) 0%, var(--crm-navy-dark) 100%);
      overflow-x: hidden;
    }

    /* ---- Brand block ---- */
    .brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0 1rem;
      min-height: var(--crm-topbar-height);
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      flex: 0 0 auto;
    }

    .brand__mark {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: var(--crm-radius-sm);
      background: linear-gradient(135deg, var(--crm-teal), var(--crm-teal-dark));
      color: #fff;
      flex: 0 0 auto;
      box-shadow: 0 2px 8px rgba(37, 149, 195, 0.45);
    }

    .brand__mark mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .brand__text {
      display: flex;
      flex-direction: column;
      min-width: 0;
      line-height: 1.25;
    }

    .brand__name {
      color: #fff;
      font-weight: 700;
      font-size: 0.9rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .brand__role {
      color: var(--crm-on-navy-muted);
      font-size: 0.75rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    /* ---- Scroll area ---- */
    .rail__scroll {
      flex: 1 1 auto;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 0.875rem 0.75rem 1.25rem;
    }

    .rail__scroll::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.18);
      background-clip: content-box;
    }

    /* ---- Groups ---- */
    .group + .group {
      margin-top: 1.125rem;
    }

    .group__label {
      margin: 0 0 0.375rem;
      padding-inline: 0.75rem;
      font-size: 0.68rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.09em;
      color: rgba(255, 255, 255, 0.42);
    }

    .group__list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    /* ---- Nav item ---- */
    .item {
      position: relative;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      border-radius: var(--crm-radius-sm);
      color: var(--crm-on-navy-muted);
      text-decoration: none;
      font-size: 0.875rem;
      font-weight: 500;
      transition: background 0.18s ease, color 0.18s ease;
    }

    .item__icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      flex: 0 0 auto;
      color: rgba(255, 255, 255, 0.55);
      transition: color 0.18s ease;
    }

    .item__label {
      flex: 1 1 auto;
      min-width: 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .item:hover {
      background: var(--crm-on-navy-hover);
      color: #fff;
    }

    .item:hover .item__icon {
      color: var(--crm-teal);
    }

    .item--active {
      background: rgba(60, 180, 229, 0.16);
      color: #fff;
      font-weight: 600;
    }

    .item--active .item__icon {
      color: var(--crm-teal);
    }

    /* Active indicator bar on the inline-start edge */
    .item--active::before {
      content: '';
      position: absolute;
      inset-block: 20%;
      inset-inline-start: 0;
      width: 3px;
      border-radius: var(--crm-radius-pill);
      background: var(--crm-teal);
    }

    .item__badge {
      flex: 0 0 auto;
      min-width: 20px;
      padding: 0 0.4rem;
      border-radius: var(--crm-radius-pill);
      background: var(--crm-teal);
      color: #08222f;
      font-size: 0.7rem;
      font-weight: 700;
      line-height: 20px;
      text-align: center;
    }
  `
})
export class SidenavComponent implements OnInit {
  readonly store = inject(AuthStore);
  private readonly gates = inject(GateService);
  private readonly proposals = inject(ProposalService);
  readonly approvalCount = signal(0);
  readonly proposalCount = signal(0);
  readonly inboxCount = computed(() => this.approvalCount() + this.proposalCount());
  readonly primaryRole = computed(() => this.store.roles()[0] ?? '');

  roleLabelKey(role: string): string {
    return `roles.${role}`;
  }

  private readonly navGroups: NavGroup[] = [
    {
      labelKey: 'nav.groupOverview',
      items: [
        { path: '/dashboard', labelKey: 'nav.dashboard', icon: 'dashboard' },
        { path: '/inbox', labelKey: 'nav.inbox', icon: 'inbox', badge: () => this.inboxCount() }
      ]
    },
    {
      labelKey: 'nav.groupPipeline',
      items: [
        { path: '/opportunities', labelKey: 'nav.opportunities', icon: 'work' },
        {
          path: '/contracting',
          labelKey: 'nav.contracting',
          icon: 'handshake',
          roles: ['BIDS_MGMT', 'MGMT', 'ADMIN']
        }
      ]
    },
    {
      labelKey: 'nav.groupSystem',
      items: [
        { path: '/reports', labelKey: 'nav.reports', icon: 'insights', roles: ['BIDS_MGMT', 'MGMT', 'ADMIN'] },
        { path: '/admin/users', labelKey: 'nav.admin', icon: 'admin_panel_settings', roles: ['ADMIN'] },
        { path: '/admin/permissions', labelKey: 'admin.permissions', icon: 'lock', roles: ['ADMIN'] }
      ]
    }
  ];

  readonly groups = computed(() =>
    this.navGroups
      .map((group) => ({
        labelKey: group.labelKey,
        items: group.items.filter((item) => !item.roles || this.store.hasRole(item.roles))
      }))
      .filter((group) => group.items.length > 0)
  );

  ngOnInit(): void {
    this.gates.pendingCount().subscribe({
      next: (r) => this.approvalCount.set(r.count),
      error: () => this.approvalCount.set(0)
    });
    this.proposals.myTasks({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => this.proposalCount.set(r.totalCount),
      error: () => this.proposalCount.set(0)
    });
  }
}
