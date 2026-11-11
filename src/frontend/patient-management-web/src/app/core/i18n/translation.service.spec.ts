import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { TranslationService } from './translation.service';

describe('TranslationService', () => {
  let service: TranslationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TranslationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('translate_LoadedKey_ReturnsEnglishText', () => {
    service.load().subscribe();
    http.expectOne('assets/i18n/en.json').flush({ 'nav.dashboard': 'Dashboard' });

    expect(service.translate('nav.dashboard')).toBe('Dashboard');
  });

  it('translate_MissingKey_ReturnsTheKey', () => {
    service.load().subscribe();
    http.expectOne('assets/i18n/en.json').flush({});

    expect(service.translate('nav.unknown')).toBe('nav.unknown');
  });

  it('load_FileUnavailable_KeepsKeysVisibleWithoutFailing', () => {
    let completed = false;
    service.load().subscribe({ complete: () => (completed = true) });
    http
      .expectOne('assets/i18n/en.json')
      .flush('not found', { status: 404, statusText: 'Not Found' });

    expect(completed).toBe(true);
    expect(service.translate('nav.dashboard')).toBe('nav.dashboard');
  });
});
