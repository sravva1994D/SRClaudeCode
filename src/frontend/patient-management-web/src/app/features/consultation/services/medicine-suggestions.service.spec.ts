import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { MedicineSuggestionsService } from './medicine-suggestions.service';

const URL = `${environment.apiBaseUrl}/medicines/suggestions`;

describe('MedicineSuggestionsService', () => {
  let service: MedicineSuggestionsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MedicineSuggestionsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('asks for names that start with the search text, with a limit of ten', () => {
    service.suggest('Par').subscribe();

    const request = http.expectOne((candidate) => candidate.url === URL);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('search')).toBe('Par');
    expect(request.request.params.get('limit')).toBe('10');
    request.flush(['Paracetamol']);
  });
});
