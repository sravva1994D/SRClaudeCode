import { inject, Pipe, PipeTransform } from '@angular/core';

import { TranslationService } from '../../core/i18n/translation.service';

/** Turns a translation key into the English text. Usage: {{ 'nav.dashboard' | translate }} */
@Pipe({ name: 'translate' })
export class TranslatePipe implements PipeTransform {
  private readonly translations = inject(TranslationService);

  transform(key: string): string {
    return this.translations.translate(key);
  }
}
