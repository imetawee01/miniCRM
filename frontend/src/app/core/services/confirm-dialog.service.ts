import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { ReasonDialogComponent, ReasonDialogData } from '../../shared/components/reason-dialog/reason-dialog.component';

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  confirm(data: ConfirmDialogData): Observable<boolean> {
    return this.dialog
      .open(ConfirmDialogComponent, {
        data,
        width: '480px',
        autoFocus: 'dialog',
        ariaLabelledBy: 'crm-confirm-title'
      })
      .afterClosed()
      .pipe(map((result) => result === true));
  }

  /** Opens a localized reason dialog. Emits the trimmed reason, or null if cancelled. */
  reason(data: ReasonDialogData): Observable<string | null> {
    return this.dialog
      .open(ReasonDialogComponent, {
        data,
        width: '480px',
        autoFocus: 'dialog',
        ariaLabelledBy: 'crm-reason-title'
      })
      .afterClosed()
      .pipe(map((result) => (typeof result === 'string' && result.trim() ? result.trim() : null)));
  }
}
