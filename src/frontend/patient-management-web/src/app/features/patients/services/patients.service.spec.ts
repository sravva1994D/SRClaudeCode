import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { PatientRequest } from '../../../core/models/patient.model';
import { PatientsService } from './patients.service';

const BASE_URL = `${environment.apiBaseUrl}/patients`;

const REQUEST: PatientRequest = {
  fullName: 'Asha Rao',
  dateOfBirth: '1990-05-14',
  genderId: 2,
  phoneNumber: '919876543210',
  address: null,
  bloodGroupId: null,
  chronicConditions: null,
};

describe('PatientsService', () => {
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

  it('sends the duplicate question with name, date of birth and the patient being edited', () => {
    service.findDuplicates('Asha Rao', '1990-05-14', 7).subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === `${BASE_URL}/duplicates` && candidate.method === 'GET',
    );
    expect(request.request.params.get('fullName')).toBe('Asha Rao');
    expect(request.request.params.get('dateOfBirth')).toBe('1990-05-14');
    expect(request.request.params.get('excludePatientId')).toBe('7');
    request.flush([]);
  });

  it('leaves the exclusion out when registering a new patient', () => {
    service.findDuplicates('Asha Rao', '1990-05-14', null).subscribe();

    const request = http.expectOne((candidate) => candidate.url === `${BASE_URL}/duplicates`);
    expect(request.request.params.has('excludePatientId')).toBe(false);
    request.flush([]);
  });

  it('posts the request body when creating a patient', () => {
    service.create(REQUEST).subscribe();

    const request = http.expectOne(BASE_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(REQUEST);
    request.flush({});
  });

  it('puts the request body to the patient when updating', () => {
    service.update(7, REQUEST).subscribe();

    const request = http.expectOne(`${BASE_URL}/7`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(REQUEST);
    request.flush({});
  });
});
