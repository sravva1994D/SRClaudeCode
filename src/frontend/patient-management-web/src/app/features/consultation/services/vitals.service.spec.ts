import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import { VitalsService } from './vitals.service';

const BASE_URL = `${environment.apiBaseUrl}/visits/5/vitals`;

describe('VitalsService', () => {
  let service: VitalsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VitalsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts only the measured values', () => {
    service
      .record(5, {
        temperature: 36.6,
        bloodPressureSystolic: null,
        bloodPressureDiastolic: null,
        pulse: 72,
      })
      .subscribe();

    const request = http.expectOne(BASE_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      temperature: 36.6,
      bloodPressureSystolic: null,
      bloodPressureDiastolic: null,
      pulse: 72,
    });
    request.flush({ vitals: {}, warnings: [] });
  });

  it('suppresses the error snackbar when recording, because the page shows the message', () => {
    service
      .record(5, {
        temperature: null,
        bloodPressureSystolic: null,
        bloodPressureDiastolic: null,
        pulse: 900,
      })
      .subscribe({ error: () => undefined });

    const request = http.expectOne(BASE_URL);
    expect(request.request.context.get(SUPPRESS_ERROR_NOTIFICATION)).toBe(true);
    request.flush({ code: 'vitals_implausible' }, { status: 400, statusText: 'Bad Request' });
  });

  it('suppresses the error snackbar when reading, because no vitals yet is an expected answer', () => {
    service.get(5).subscribe({ error: () => undefined });

    const request = http.expectOne(BASE_URL);
    expect(request.request.method).toBe('GET');
    expect(request.request.context.get(SUPPRESS_ERROR_NOTIFICATION)).toBe(true);
    request.flush({}, { status: 404, statusText: 'Not Found' });
  });
});
