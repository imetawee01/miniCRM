import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';
import { ConfirmDialogService } from '../services/confirm-dialog.service';

export interface CanComponentDeactivate {
  canDeactivate: () => boolean | Observable<boolean>;
}

export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  if (!component?.canDeactivate || component.canDeactivate()) {
    return true;
  }
  return inject(ConfirmDialogService).confirm({
    titleKey: 'common.unsavedTitle',
    messageKey: 'common.unsavedMessage',
    confirmKey: 'common.leave',
    cancelKey: 'common.stay',
    warn: true
  });
};
