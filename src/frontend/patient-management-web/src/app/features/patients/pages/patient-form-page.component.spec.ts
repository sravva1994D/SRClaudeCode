import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';

import { PatientSummary } from '../../../core/models/patient.model';
import { PatientsService } from '../services/patients.service';
import { PatientFormPageComponent } from './patient-form-page.component';

const DEBOUNCE_WAIT_MS = 1000;

const EXISTING_PATIENT: PatientSummary = {
  patientId: 3,
  fullName: 'Asha Rao',
  dateOfBirth: '1990-05-14',
};

describe('PatientFormPageComponent', () => {
  let fixture: ComponentFixture<PatientFormPageComponent>;
  let findDuplicates: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    findDuplicates = vi.fn(() => of([EXISTING_PATIENT]));

    TestBed.configureTestingModule({
      imports: [PatientFormPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: PatientsService,
          useValue: { findDuplicates, get: vi.fn(), create: vi.fn(), update: vi.fn() },
        },
      ],
    });

    fixture = TestBed.createComponent(PatientFormPageComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('shows a duplicate warning when a patient has the same name and date of birth', () => {
    fixture.componentInstance.form.patchValue({ fullName: 'Asha Rao', dateOfBirth: '1990-05-14' });
    vi.advanceTimersByTime(DEBOUNCE_WAIT_MS);
    fixture.detectChanges();

    expect(findDuplicates).toHaveBeenCalledWith('Asha Rao', '1990-05-14', null);
    const warning = fixture.nativeElement.querySelector('.duplicate-warning');
    expect(warning).not.toBeNull();
    expect(warning.textContent).toContain('Asha Rao');
  });

  it('does not look for duplicates while the date of birth is incomplete', () => {
    fixture.componentInstance.form.patchValue({ fullName: 'Asha Rao', dateOfBirth: '1990-05' });
    vi.advanceTimersByTime(DEBOUNCE_WAIT_MS);
    fixture.detectChanges();

    expect(findDuplicates).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.duplicate-warning')).toBeNull();
  });

  it('keeps the save button disabled until the form is valid', () => {
    const save = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(save.disabled).toBe(true);

    fixture.componentInstance.form.patchValue({
      fullName: 'Asha Rao',
      dateOfBirth: '1990-05-14',
      genderId: 2,
    });
    fixture.detectChanges();

    expect(save.disabled).toBe(false);
  });
});
