import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { TranslationService } from '../i18n/translation.service';
import { NotificationService } from '../notifications/notification.service';
import { toApiRequestError } from './api-request-error';

/** Normalizes every failed request, tells the user once, and rethrows an ApiRequestError. */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const translations = inject(TranslationService);
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      const apiError = toApiRequestError(error, (key) => translations.translate(key));
      notifications.showError(apiError.message);
      return throwError(() => apiError);
    }),
  );
};
