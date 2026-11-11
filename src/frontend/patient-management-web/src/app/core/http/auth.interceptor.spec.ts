import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

const API = environment.apiBaseUrl;

describe('authInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    signIn('token-1');
  });

  afterEach(() => testing.verify());

  it('apiCall_ValidToken_SendsTheBearerTokenAndCookieCredentials', () => {
    http.get(`${API}/clinic-profile`).subscribe();

    const request = testing.expectOne(`${API}/clinic-profile`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
    expect(request.request.withCredentials).toBe(true);
    request.flush({});
  });

  it('apiCall_Returns401_RefreshesTheSessionAndRepeatsTheRequest', () => {
    let body: unknown;
    http.get(`${API}/clinic-profile`).subscribe((value) => (body = value));

    testing
      .expectOne(`${API}/clinic-profile`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    testing
      .expectOne(`${API}/auth/refresh-token`)
      .flush({ accessToken: 'token-2', expiresInSeconds: 900 });
    const retried = testing.expectOne(`${API}/clinic-profile`);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer token-2');
    retried.flush({ clinicName: 'Synthetic Clinic' });

    expect(body).toEqual({ clinicName: 'Synthetic Clinic' });
  });

  it('apiCall_RefreshFails_ClearsTheSessionAndSendsTheUserToSignIn', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    let failure: unknown;
    http.get(`${API}/clinic-profile`).subscribe({ error: (error: unknown) => (failure = error) });

    testing
      .expectOne(`${API}/clinic-profile`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    testing
      .expectOne(`${API}/auth/refresh-token`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isSignedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(
      ['/login'],
      expect.objectContaining({ queryParams: expect.any(Object) }),
    );
    expect(failure).toBeDefined();
  });

  it('loginCall_Returns401_IsNotRetried', () => {
    http
      .post(`${API}/auth/login`, { username: 'x', password: 'y' })
      .subscribe({ error: () => undefined });

    testing.expectOne(`${API}/auth/login`).flush(null, { status: 401, statusText: 'Unauthorized' });

    // Only the login request exists: no refresh was started.
    testing.expectNone(`${API}/auth/refresh-token`);
  });

  it('externalCall_OutsideTheApi_IsLeftUntouched', () => {
    http.get('https://example.test/data').subscribe();

    const request = testing.expectOne('https://example.test/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.withCredentials).toBe(false);
    request.flush({});
  });

  function signIn(token: string): void {
    auth.login('dev.doctor', 'Passw0rd!').subscribe();
    testing.expectOne(`${API}/auth/login`).flush({ accessToken: token, expiresInSeconds: 900 });
  }
});
