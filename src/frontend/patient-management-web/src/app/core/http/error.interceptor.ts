import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { TranslationService } from '../i18n/translation.service';
import { NotificationService } from '../notifications/notification.service';
import { toApiRequestError } from './api-request-error';
import { SUPPRESS_ERROR_NOTIFICATION } from './silent-errors';

/**
 * Sign-in and session-check calls report their failures on the page that made them (the login form, or the guard
 * that sends the user to sign in). A snackbar for them would only add noise.
 */
const QUIET_PATHS = ['/auth/login', '/auth/refresh-token'];

/** Normalizes every failed request, tells the user once, and rethrows an ApiRequestError. */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const translations = inject(TranslationService);
  const notifications = inject(NotificationService);
  const isQuiet =
    QUIET_PATHS.some((path) => request.url.endsWith(path)) ||
    request.context.get(SUPPRESS_ERROR_NOTIFICATION);

  return next(request).pipe(
    catchError((error: unknown) => {
      const apiError = toApiRequestError(error, (key) => translations.translate(key));
      if (!isQuiet) {
        notifications.showError(apiError.message);
      }
      return throwError(() => apiError);
    }),
  );
};
