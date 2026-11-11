import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let signedIn: boolean;
  let refreshSession: ReturnType<typeof vi.fn>;

  const route = {} as ActivatedRouteSnapshot;
  const stateFor = (url: string) => ({ url }) as RouterStateSnapshot;

  function run(url: string): unknown {
    return TestBed.runInInjectionContext(() => authGuard(route, stateFor(url)));
  }

  beforeEach(() => {
    signedIn = false;
    refreshSession = vi.fn(() => of(undefined));

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isSignedIn: () => signedIn, refreshSession: () => refreshSession() },
        },
      ],
    });
  });

  it('lets the page load without a refresh when a session is already in memory', () => {
    signedIn = true;

    const result = run('/settings');

    expect(result).toBe(true);
    expect(refreshSession).not.toHaveBeenCalled();
  });

  it('lets the page load when the refresh cookie starts a new session', async () => {
    const result = (await firstValueFrom(run('/settings') as Observable<unknown>)) as boolean;

    expect(result).toBe(true);
    expect(refreshSession).toHaveBeenCalledTimes(1);
  });

  it('redirects to login and keeps the requested page when no session can be restored', async () => {
    refreshSession.mockReturnValue(throwError(() => new Error('no session')));

    const result = await firstValueFrom(run('/settings') as Observable<unknown>);

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Fsettings',
    );
  });
});
