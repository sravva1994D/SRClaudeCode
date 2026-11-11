import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { Patient } from '../../../core/models/patient.model';
import { PatientsService } from '../services/patients.service';
import { PatientProfilePageComponent } from './patient-profile-page.component';

const ASHA: Patient = {
  patientId: 3,
  fullName: 'Asha Rao',
  dateOfBirth: '1990-05-14',
  genderId: 2,
  phoneNumber: '9876543210',
  address: '12 Lake Road',
  bloodGroupId: 3,
  chronicConditions: 'Asthma',
};

describe('PatientProfilePageComponent', () => {
  let fixture: ComponentFixture<PatientProfilePageComponent>;
  let get: ReturnType<typeof vi.fn>;
  let markViewed: ReturnType<typeof vi.fn>;

  function createPage(): void {
    TestBed.configureTestingModule({
      imports: [PatientProfilePageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: PatientsService,
          useValue: {
            get,
            markViewed,
            list: vi.fn(),
            recent: vi.fn(),
            create: vi.fn(),
            update: vi.fn(),
            findDuplicates: vi.fn(),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(PatientProfilePageComponent);
    fixture.componentRef.setInput('patientId', '3');
    fixture.detectChanges();
  }

  beforeEach(() => {
    get = vi.fn(() => of(ASHA));
    markViewed = vi.fn(() => of(undefined));
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows the patient details and records the visit', () => {
    createPage();

    expect(get).toHaveBeenCalledWith(3);
    expect(markViewed).toHaveBeenCalledWith(3);
    expect(fixture.nativeElement.textContent).toContain('Asha Rao');
    expect(fixture.nativeElement.textContent).toContain('12 Lake Road');
  });

  it('still shows the patient when recording the visit fails', () => {
    markViewed.mockReturnValue(throwError(() => new Error('network')));

    createPage();

    expect(fixture.nativeElement.textContent).toContain('Asha Rao');
  });

  it('shows an error when the patient cannot be loaded', () => {
    get.mockReturnValue(throwError(() => new Error('not found')));

    createPage();

    expect(fixture.nativeElement.querySelector('.error')).not.toBeNull();
  });
});
