import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snack = inject(MatSnackBar);
  private readonly i18n = inject(TranslateService);

  success(messageKey: string, params?: Record<string, unknown>): void {
    this.show(this.i18n.instant(messageKey, params), 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  info(messageKey: string, params?: Record<string, unknown>): void {
    this.show(this.i18n.instant(messageKey, params), 'info');
  }

  show(message: string, panel: 'success' | 'error' | 'info' = 'info'): void {
    this.snack.open(message, this.i18n.instant('common.dismiss'), {
      duration: panel === 'error' ? 8000 : 4000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: [`crm-toast-${panel}`]
    });
  }
}
