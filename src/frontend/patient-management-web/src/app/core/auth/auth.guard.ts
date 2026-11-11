import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AuthService } from './auth.service';

/**
 * Lets the page load when a session is in memory, or when the refresh cookie can start one (after a page reload).
 * Otherwise sends the user to the login page and remembers where they were going.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isSignedIn()) {
    return true;
  }

  const router = inject(Router);
  return auth.refreshSession().pipe(
    map(() => true),
    catchError(() =>
      of(router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })),
    ),
  );
};
