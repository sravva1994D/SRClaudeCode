import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PagedResponse, PatientListItem } from '../../../core/models/patient.model';
import { PatientsService } from '../services/patients.service';
import { PatientListPageComponent } from './patient-list-page.component';

const DEBOUNCE_WAIT_MS = 1000;

const ASHA: PatientListItem = {
  patientId: 3,
  fullName: 'Asha Rao',
  dateOfBirth: '1990-05-14',
  genderId: 2,
  phoneNumber: '9876543210',
};

const ONE_PATIENT_PAGE: PagedResponse<PatientListItem> = {
  items: [ASHA],
  totalCount: 1,
  pageNumber: 1,
  pageSize: 20,
};

describe('PatientListPageComponent', () => {
  let fixture: ComponentFixture<PatientListPageComponent>;
  let list: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    list = vi.fn(() => of(ONE_PATIENT_PAGE));

    TestBed.configureTestingModule({
      imports: [PatientListPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: PatientsService,
          useValue: {
            list,
            get: vi.fn(),
            create: vi.fn(),
            update: vi.fn(),
            findDuplicates: vi.fn(),
            recent: vi.fn(() => of([])),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(PatientListPageComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('loads the first page on start and shows each patient', () => {
    expect(list).toHaveBeenCalledWith({ search: '', page: 1, pageSize: 20 });
    expect(fixture.nativeElement.textContent).toContain('Asha Rao');
  });

  it('searches again after the user pauses typing, starting from the first page', () => {
    fixture.componentInstance.searchControl.setValue('asha');
    vi.advanceTimersByTime(DEBOUNCE_WAIT_MS);

    expect(list).toHaveBeenLastCalledWith({ search: 'asha', page: 1, pageSize: 20 });
  });

  it('shows the recently opened patients while the search is empty', () => {
    fixture.destroy();
    TestBed.resetTestingModule();
    list.mockReturnValue(of(ONE_PATIENT_PAGE));
    TestBed.configureTestingModule({
      imports: [PatientListPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: PatientsService,
          useValue: {
            list,
            recent: vi.fn(() => of([ASHA])),
            get: vi.fn(),
            create: vi.fn(),
            update: vi.fn(),
            findDuplicates: vi.fn(),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(PatientListPageComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.recent')).not.toBeNull();
  });

  it('shows the empty message when no patient matches', () => {
    list.mockReturnValue(of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20 }));
    fixture.componentInstance.searchControl.setValue('zzz');
    vi.advanceTimersByTime(DEBOUNCE_WAIT_MS);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.empty')).not.toBeNull();
  });
});
