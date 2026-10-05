import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ProblemDetails } from '../../core/models/paged-result';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import { LangSwitcherComponent } from '../../core/layout/lang-switcher.component';

@Component({
  selector: 'crm-login-page',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, ReactiveFormsModule, AutofocusDirective, LangSwitcherComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="login">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ 'app.title' | translate }}</mat-card-title>
          <mat-card-subtitle>{{ 'auth.signIn' | translate }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" class="crm-stack">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'auth.email' | translate }}</mat-label>
              <input matInput type="email" formControlName="email" crmAutofocus autocomplete="username" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'auth.password' | translate }}</mat-label>
              <input matInput type="password" formControlName="password" autocomplete="current-password" />
            </mat-form-field>
            @if (error()) {
              <p class="error">{{ error() }}</p>
            }
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || busy()">
              {{ 'auth.signIn' | translate }}
            </button>
            <!-- TODO(SSO): enable when GET /auth/sso/config returns ssoEnabled true -->
            <button
              mat-stroked-button
              type="button"
              disabled
              [matTooltip]="'auth.ssoComingSoon' | translate"
            >
              {{ 'auth.sso' | translate }}
            </button>
          </form>
        </mat-card-content>
        <mat-card-actions>
          <crm-lang-switcher />
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: `
    .login { min-height: 100vh; display: grid; place-items: center; background: linear-gradient(160deg, #192444, #2595c3); padding: 1rem; }
    mat-card { width: min(420px, 100%); }
    .error { color: var(--crm-danger); }
  `
})
export class LoginPageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(TranslateService);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly ssoEnabled = signal(false);
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  ngOnInit(): void {
    this.auth.ssoConfig().subscribe((c) => this.ssoEnabled.set(c.ssoEnabled));
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const raw = this.form.getRawValue();
    const payload = {
      email: raw.email.trim(),
      password: raw.password
    };
    this.auth.login(payload).subscribe({
      next: () => {
        this.busy.set(false);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.loginErrorMessage(err));
      }
    });
  }

  private loginErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0 || err.status === 502 || err.status === 504) {
      return this.i18n.instant('auth.apiUnreachable');
    }
    if (err.status === 401 || err.status === 400) {
      return this.i18n.instant('auth.loginFailed');
    }
    if (err.status === 429) {
      return this.i18n.instant('auth.tooManyAttempts');
    }
    const problem =
      err.error && typeof err.error === 'object' ? (err.error as ProblemDetails) : null;
    return problem?.detail || problem?.title || this.i18n.instant('auth.loginFailed');
  }
}
