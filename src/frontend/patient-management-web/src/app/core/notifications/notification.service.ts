import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

import { TranslationService } from '../i18n/translation.service';

const SNACK_BAR_DURATION_MS = 6000;

/** Shows short, announced messages. Text is passed as a translation key, never as raw English. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly translations = inject(TranslationService);

  showSuccess(messageKey: string): void {
    this.open(messageKey, 'notifications.dismiss');
  }

  showError(message: string): void {
    this.snackBar.open(message, this.translations.translate('notifications.dismiss'), {
      duration: SNACK_BAR_DURATION_MS,
      politeness: 'assertive',
    });
  }

  private open(messageKey: string, actionKey: string): void {
    this.snackBar.open(
      this.translations.translate(messageKey),
      this.translations.translate(actionKey),
      {
        duration: SNACK_BAR_DURATION_MS,
        politeness: 'polite',
      },
    );
  }
}
