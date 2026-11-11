import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize, map, Observable, share, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AccessTokenResponse, ChangePasswordRequest, LoginRequest } from '../models/auth.model';

/**
 * Holds the access token in memory only (CLAUDE.md 8.4). The refresh token is an HTTP-only cookie the browser sends
 * itself. Refresh tokens rotate, so concurrent refreshes share one request: two parallel refreshes would look like
 * reuse to the server and end the session.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly accessToken = signal<string | null>(null);
  private refreshInFlight: Observable<void> | null = null;

  readonly isSignedIn = computed(() => this.accessToken() !== null);

  currentAccessToken(): string | null {
    return this.accessToken();
  }

  login(username: string, password: string): Observable<void> {
    const body: LoginRequest = { username, password };
    return this.http
      .post<AccessTokenResponse>(`${environment.apiBaseUrl}/auth/login`, body, {
        withCredentials: true,
      })
      .pipe(
        tap((response) => this.accessToken.set(response.accessToken)),
        map(() => undefined),
      );
  }

  /** Gets a new access token from the refresh cookie. Concurrent callers share the same request. */
  refreshSession(): Observable<void> {
    if (this.refreshInFlight === null) {
      this.refreshInFlight = this.http
        .post<AccessTokenResponse>(
          `${environment.apiBaseUrl}/auth/refresh-token`,
          {},
          { withCredentials: true },
        )
        .pipe(
          tap((response) => this.accessToken.set(response.accessToken)),
          map(() => undefined),
          finalize(() => (this.refreshInFlight = null)),
          share(),
        );
    }
    return this.refreshInFlight;
  }

  /** Ends the session on the server. The token is cleared whether or not the server call succeeds. */
  logout(): Observable<void> {
    return this.http
      .post<void>(`${environment.apiBaseUrl}/auth/logout`, {}, { withCredentials: true })
      .pipe(
        finalize(() => this.clearSession()),
        map(() => undefined),
      );
  }

  /** The server ends every session when the password changes, so the caller must sign in again afterwards. */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    const body: ChangePasswordRequest = { currentPassword, newPassword };
    return this.http
      .post<void>(`${environment.apiBaseUrl}/auth/change-password`, body, { withCredentials: true })
      .pipe(
        tap(() => this.clearSession()),
        map(() => undefined),
      );
  }

  clearSession(): void {
    this.accessToken.set(null);
  }
}
