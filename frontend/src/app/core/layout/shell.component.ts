import { AfterViewInit, ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSidenav } from '@angular/material/sidenav';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { LoadingService } from '../services/loading.service';
import { SidenavComponent } from './sidenav.component';
import { TopbarComponent } from './topbar.component';

@Component({
  selector: 'crm-shell',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterOutlet, SidenavComponent, TopbarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading.active()) {
      <mat-progress-bar class="crm-loading-bar" mode="indeterminate" />
    }
    <mat-sidenav-container class="shell">
      <mat-sidenav #drawer mode="side" opened position="start" class="crm-sidenav">
        <crm-sidenav />
      </mat-sidenav>
      <mat-sidenav-content>
        <crm-topbar #topbar />
        <main id="main" class="main" tabindex="-1">
          <router-outlet />
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: `
    .shell { height: 100vh; background: var(--crm-surface); }

    .crm-sidenav {
      width: var(--crm-sidenav-width);
      border-inline-end: 0;
      background: var(--crm-navy);
      box-shadow: 4px 0 16px rgba(15, 24, 48, 0.16);
      overflow: hidden;
    }

    .main {
      min-height: calc(100vh - var(--crm-topbar-height));
      background: var(--crm-surface);
    }
  `
})
export class ShellComponent implements AfterViewInit {
  readonly loading = inject(LoadingService);
  readonly drawer = viewChild<MatSidenav>('drawer');
  readonly topbar = viewChild(TopbarComponent);

  ngAfterViewInit(): void {
    const bar = this.topbar();
    if (bar) {
      bar.toggleSidenav = () => void this.drawer()?.toggle();
    }
  }
}
