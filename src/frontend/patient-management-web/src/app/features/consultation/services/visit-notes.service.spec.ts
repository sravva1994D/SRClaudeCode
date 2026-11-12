import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import { VisitNotesService } from './visit-notes.service';

const BASE_URL = `${environment.apiBaseUrl}/visits/5`;

describe('VisitNotesService', () => {
  let service: VisitNotesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VisitNotesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the saved notes for the visit', () => {
    service.get(5).subscribe();

    const request = http.expectOne(`${BASE_URL}/notes`);
    expect(request.request.method).toBe('GET');
    request.flush({ visitId: 5, complaints: [], diagnoses: [] });
  });

  it('puts the complaints list as a draft', () => {
    service.saveComplaints(5, ['Cough']).subscribe();

    const request = http.expectOne(`${BASE_URL}/complaints`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ complaints: ['Cough'] });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('puts the diagnoses list with each code as a draft', () => {
    service.saveDiagnoses(5, [{ text: 'Viral fever', code: 'J11' }]).subscribe();

    const request = http.expectOne(`${BASE_URL}/diagnoses`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ diagnoses: [{ text: 'Viral fever', code: 'J11' }] });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('suppresses the error snackbar on saves, because the page shows the message inline', () => {
    service.saveComplaints(5, ['Cough']).subscribe({ error: () => undefined });

    const request = http.expectOne(`${BASE_URL}/complaints`);
    expect(request.request.context.get(SUPPRESS_ERROR_NOTIFICATION)).toBe(true);
    request.flush(
      { code: 'visit_not_in_progress' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
  });
});
