import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';

import { TranslationService } from '../i18n/translation.service';

/** A page whose form may hold edits that have not been saved. The page decides what counts as unsaved. */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * Asks before leaving a page with unsaved edits (CLAUDE.md 8.3). When the user cancels, the navigation stays on the page
 * and the edits remain.
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  const translations = inject(TranslationService);
  return window.confirm(translations.translate('consultation.unsavedChanges'));
};
