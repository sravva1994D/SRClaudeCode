import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';

import { ApiRequestError } from '../../../core/http/api-request-error';
import { Vitals } from '../../../core/models/vitals.model';
import {
  Visit,
  VISIT_STATUS_COMPLETED,
  VISIT_STATUS_IN_PROGRESS,
} from '../../../core/models/visit.model';
import { VisitsService } from '../services/visits.service';
import { VitalsService } from '../services/vitals.service';
import { VisitVitalsPageComponent } from './visit-vitals-page.component';

const VISIT: Visit = {
  visitId: 5,
  patientId: 7,
  appointmentId: 3,
  statusId: VISIT_STATUS_IN_PROGRESS,
};

const SAVED: Vitals = {
  vitalsId: 1,
  visitId: 5,
  temperature: null,
  bloodPressureSystolic: null,
  bloodPressureDiastolic: null,
  pulse: 130,
};

describe('VisitVitalsPageComponent', () => {
  let fixture: ComponentFixture<VisitVitalsPageComponent>;
  let getVisit: ReturnType<typeof vi.fn>;
  let getVitals: ReturnType<typeof vi.fn>;
  let record: ReturnType<typeof vi.fn>;

  function create(visit: Visit, vitals: Vitals | null): void {
    getVisit = vi.fn(() => of(visit));
    getVitals = vi.fn(() => (vitals === null ? throwError(() => new Error('404')) : of(vitals)));
    record = vi.fn();

    TestBed.configureTestingModule({
      imports: [VisitVitalsPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VisitsService, useValue: { get: getVisit } },
        { provide: VitalsService, useValue: { get: getVitals, record } },
      ],
    });

    fixture = TestBed.createComponent(VisitVitalsPageComponent);
    fixture.componentRef.setInput('visitId', '5');
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  it('has unsaved changes only after the user types into the entry form', () => {
    create(VISIT, null);
    const page = fixture.componentInstance;
    expect(page.hasUnsavedChanges()).toBe(false);

    page.form.controls.pulse.setValue(72);
    page.form.controls.pulse.markAsDirty();

    expect(page.hasUnsavedChanges()).toBe(true);
  });

  it('shows the entry form when the visit is in progress and no vitals are recorded', () => {
    create(VISIT, null);

    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.recorded')).toBeNull();
  });

  it('shows the recorded values and no form when vitals already exist', () => {
    create(VISIT, SAVED);

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('.recorded').textContent).toContain('130');
  });

  it('tells the doctor the visit is not in progress and offers no form', () => {
    create({ ...VISIT, statusId: VISIT_STATUS_COMPLETED }, null);

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('.notice')).not.toBeNull();
  });

  it('does not send a request when no value is entered', () => {
    create(VISIT, null);

    fixture.componentInstance.save();
    fixture.detectChanges();

    expect(record).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('shows a warning banner and the saved values when a value is outside its normal range', () => {
    create(VISIT, null);
    record.mockReturnValue(of({ vitals: SAVED, warnings: ['Pulse'] }));
    fixture.componentInstance.form.patchValue({ pulse: 130 });

    fixture.componentInstance.save();
    fixture.detectChanges();

    expect(record).toHaveBeenCalledWith(5, {
      temperature: null,
      bloodPressureSystolic: null,
      bloodPressureDiastolic: null,
      pulse: 130,
    });
    const banner = fixture.nativeElement.querySelector('.warning-banner');
    expect(banner).not.toBeNull();
    expect(banner.textContent).toContain('consultation.vitals.pulse');
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('keeps the form and shows an inline message when a value is outside its plausible range', () => {
    create(VISIT, null);
    record.mockReturnValue(
      throwError(
        () => new ApiRequestError('request', 400, 'vitals_implausible', 'Impossible value', null),
      ),
    );
    fixture.componentInstance.form.patchValue({ pulse: 900 });

    fixture.componentInstance.save();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.recorded')).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'consultation.vitals.implausible',
    );
  });
});
