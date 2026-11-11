import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

const API = environment.apiBaseUrl;

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login_ValidCredentials_KeepsTheAccessTokenInMemory', () => {
    auth.login('dev.doctor', 'Passw0rd!').subscribe();

    http.expectOne(`${API}/auth/login`).flush({ accessToken: 'token-1', expiresInSeconds: 900 });

    expect(auth.currentAccessToken()).toBe('token-1');
    expect(auth.isSignedIn()).toBe(true);
  });

  it('refreshSession_CalledTwiceAtOnce_SendsOneRequest', () => {
    let completed = 0;
    auth.refreshSession().subscribe({ complete: () => completed++ });
    auth.refreshSession().subscribe({ complete: () => completed++ });

    // Refresh tokens rotate, so two parallel refreshes would look like reuse to the server.
    http
      .expectOne(`${API}/auth/refresh-token`)
      .flush({ accessToken: 'token-2', expiresInSeconds: 900 });

    expect(completed).toBe(2);
    expect(auth.currentAccessToken()).toBe('token-2');
  });

  it('logout_ServerCallFails_StillClearsTheToken', () => {
    auth.login('dev.doctor', 'Passw0rd!').subscribe();
    http.expectOne(`${API}/auth/login`).flush({ accessToken: 'token-1', expiresInSeconds: 900 });

    auth.logout().subscribe({ error: () => undefined });
    http.expectOne(`${API}/auth/logout`).flush('down', { status: 500, statusText: 'Server Error' });

    expect(auth.isSignedIn()).toBe(false);
  });

  it('changePassword_Succeeds_EndsTheLocalSession', () => {
    auth.login('dev.doctor', 'Passw0rd!').subscribe();
    http.expectOne(`${API}/auth/login`).flush({ accessToken: 'token-1', expiresInSeconds: 900 });

    auth.changePassword('Passw0rd!', 'NewPassw0rd!').subscribe();
    http
      .expectOne(`${API}/auth/change-password`)
      .flush(null, { status: 204, statusText: 'No Content' });

    expect(auth.isSignedIn()).toBe(false);
  });
});
