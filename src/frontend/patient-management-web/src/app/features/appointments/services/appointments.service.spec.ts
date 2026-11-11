import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AppointmentsService } from './appointments.service';

const URL = `${environment.apiBaseUrl}/appointments`;

describe('AppointmentsService', () => {
  let service: AppointmentsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppointmentsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the range as UTC instants', () => {
    const from = new Date(Date.UTC(2026, 10, 9, 0, 0));
    const to = new Date(Date.UTC(2026, 10, 16, 0, 0));

    service.list(from, to).subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === URL && candidate.method === 'GET',
    );
    expect(request.request.params.get('from')).toBe('2026-11-09T00:00:00.000Z');
    expect(request.request.params.get('to')).toBe('2026-11-16T00:00:00.000Z');
    request.flush([]);
  });

  it('books the slot as a UTC instant', () => {
    const slot = new Date(Date.UTC(2026, 10, 11, 9, 15));

    service.schedule(7, slot).subscribe();

    const request = http.expectOne(URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ patientId: 7, slotStart: '2026-11-11T09:15:00.000Z' });
    request.flush({
      appointmentId: 1,
      patientId: 7,
      slotStartUtc: '2026-11-11T09:15:00Z',
      statusId: 1,
    });
  });
});
