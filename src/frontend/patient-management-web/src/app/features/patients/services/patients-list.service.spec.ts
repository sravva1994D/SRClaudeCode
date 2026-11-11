import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { PatientsService } from './patients.service';

const LIST_URL = `${environment.apiBaseUrl}/patients`;

describe('PatientsService.list', () => {
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

  it('sends the page and page size, and trims the search text', () => {
    service.list({ search: '  asha ', page: 2, pageSize: 20 }).subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === LIST_URL && candidate.method === 'GET',
    );
    expect(request.request.params.get('search')).toBe('asha');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    request.flush({ items: [], totalCount: 0, pageNumber: 2, pageSize: 20 });
  });

  it('omits the search parameter when the search is blank', () => {
    service.list({ search: '   ', page: 1, pageSize: 20 }).subscribe();

    const request = http.expectOne((candidate) => candidate.url === LIST_URL);
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 });
  });
});
