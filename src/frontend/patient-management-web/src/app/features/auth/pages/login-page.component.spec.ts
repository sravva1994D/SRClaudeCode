import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../core/auth/auth.service';
import { ApiRequestError } from '../../../core/http/api-request-error';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  let fixture: ComponentFixture<LoginPageComponent>;
  let login: ReturnType<typeof vi.fn>;
  let navigateByUrl: ReturnType<typeof vi.fn>;

  function createPage(returnUrl: string | null): void {
    login = vi.fn(() => of(undefined));

    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: { login } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(returnUrl === null ? {} : { returnUrl }),
            },
          },
        },
      ],
    });

    const router = TestBed.inject(Router);
    navigateByUrl = vi.fn(() => Promise.resolve(true));
    vi.spyOn(router, 'navigateByUrl').mockImplementation(navigateByUrl as never);

    fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();
  }

  function fillIn(username: string, password: string): void {
    fixture.componentInstance.form.setValue({ username, password });
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('signs in with the entered credentials and opens the requested page', () => {
    createPage('/patients/3');
    fillIn('dev.doctor', 'Passw0rd!');

    fixture.componentInstance.submit();

    expect(login).toHaveBeenCalledWith('dev.doctor', 'Passw0rd!');
    expect(navigateByUrl).toHaveBeenCalledWith('/patients/3');
  });

  it('opens the dashboard when no page was requested', () => {
    createPage(null);
    fillIn('dev.doctor', 'Passw0rd!');

    fixture.componentInstance.submit();

    expect(navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('ignores a return address that points outside the application', () => {
    createPage('//attacker.example/steal');
    fillIn('dev.doctor', 'Passw0rd!');

    fixture.componentInstance.submit();

    expect(navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('shows the server message and clears the password after a failed sign-in', () => {
    createPage(null);
    login.mockReturnValue(
      throwError(
        () =>
          new ApiRequestError(
            'business',
            401,
            'invalid_credentials',
            'Invalid username or password.',
            null,
          ),
      ),
    );
    fillIn('dev.doctor', 'wrong-password');

    fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('Invalid username or password.');
    expect(fixture.componentInstance.form.controls.password.value).toBe('');
    expect(fixture.componentInstance.saving()).toBe(false);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not submit while a required field is empty', () => {
    createPage(null);
    fillIn('dev.doctor', '');

    fixture.componentInstance.submit();

    expect(login).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.controls.password.touched).toBe(true);
  });
});
