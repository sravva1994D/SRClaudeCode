import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { PatientsService } from './patients.service';

const BASE_URL = `${environment.apiBaseUrl}/patients`;

describe('PatientsService recent and visit calls', () => {
  let service: PatientsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PatientsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the recent list', () => {
    service.recent().subscribe();

    const request = http.expectOne(`${BASE_URL}/recent`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('records a visit with a POST to the patient', () => {
    service.markViewed(7).subscribe();

    const request = http.expectOne(`${BASE_URL}/7/viewed`);
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });
});
