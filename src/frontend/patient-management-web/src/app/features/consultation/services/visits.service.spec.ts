import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import { VisitsService } from './visits.service';

const URL = `${environment.apiBaseUrl}/visits/5`;

describe('VisitsService', () => {
  let service: VisitsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VisitsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts to the complete action of the visit', () => {
    service.complete(5).subscribe();

    const request = http.expectOne(`${URL}/complete`);
    expect(request.request.method).toBe('POST');
    request.flush({ visitId: 5, patientId: 7, appointmentId: 3, statusId: 2 });
  });

  it('suppresses the error snackbar on completion, because the page shows the refusal inline', () => {
    service.complete(5).subscribe({ error: () => undefined });

    const request = http.expectOne(`${URL}/complete`);
    expect(request.request.context.get(SUPPRESS_ERROR_NOTIFICATION)).toBe(true);
    request.flush({ code: 'vitals_required' }, { status: 422, statusText: 'Unprocessable Entity' });
  });
});
