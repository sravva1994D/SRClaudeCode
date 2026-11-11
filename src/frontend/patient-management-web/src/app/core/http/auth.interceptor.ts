import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

/** Session endpoints never trigger a refresh: a 401 there is an answer, not an expired token. */
const SESSION_PATHS = ['/auth/login', '/auth/refresh-token', '/auth/logout'];

/**
 * Adds the access token to API calls and the refresh cookie credentials. On a 401 it refreshes the session once and
 * repeats the request. If the refresh fails, the session has ended on the server: the user is sent to sign in again,
 * and the original 401 is passed on.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiBaseUrl)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const isSessionCall = SESSION_PATHS.some((path) => request.url.endsWith(path));

  return next(withSession(request, auth.currentAccessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isSessionCall) {
        return throwError(() => error);
      }

      return auth.refreshSession().pipe(
        switchMap(() => next(withSession(request, auth.currentAccessToken()))),
        catchError(() => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => error);
        }),
      );
    }),
  );
};

function withSession(
  request: HttpRequest<unknown>,
  accessToken: string | null,
): HttpRequest<unknown> {
  return request.clone({
    withCredentials: true,
    setHeaders: accessToken === null ? {} : { Authorization: `Bearer ${accessToken}` },
  });
}
