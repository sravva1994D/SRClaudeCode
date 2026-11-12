import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';

import { ApiRequestError } from '../../../core/http/api-request-error';
import { VisitNotes } from '../../../core/models/visit-notes.model';
import {
  Visit,
  VISIT_STATUS_COMPLETED,
  VISIT_STATUS_IN_PROGRESS,
} from '../../../core/models/visit.model';
import { VisitNotesService } from '../services/visit-notes.service';
import { VisitsService } from '../services/visits.service';
import { VisitNotesPageComponent } from './visit-notes-page.component';

const IN_PROGRESS: Visit = {
  visitId: 5,
  patientId: 7,
  appointmentId: 3,
  statusId: VISIT_STATUS_IN_PROGRESS,
};

const EMPTY_NOTES: VisitNotes = { visitId: 5, complaints: [], diagnoses: [], medications: [] };

const SAVED_NOTES: VisitNotes = {
  visitId: 5,
  complaints: ['Cough'],
  diagnoses: [{ text: 'Viral fever', code: 'J11' }],
  medications: [
    {
      medicineName: 'Paracetamol',
      dosage: '500mg',
      frequency: 'Twice daily',
      duration: '7 days',
      specialInstructions: 'After food',
    },
  ],
};

describe('VisitNotesPageComponent', () => {
  let fixture: ComponentFixture<VisitNotesPageComponent>;
  let saveComplaints: ReturnType<typeof vi.fn>;
  let saveDiagnoses: ReturnType<typeof vi.fn>;
  let saveMedications: ReturnType<typeof vi.fn>;
  let complete: ReturnType<typeof vi.fn>;

  function create(visit: Visit, notes: VisitNotes | null): void {
    saveComplaints = vi.fn(() => of(undefined));
    saveDiagnoses = vi.fn(() => of(undefined));
    saveMedications = vi.fn(() => of(undefined));
    complete = vi.fn(() => of({ ...visit, statusId: VISIT_STATUS_COMPLETED }));

    TestBed.configureTestingModule({
      imports: [VisitNotesPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: VisitsService, useValue: { get: vi.fn(() => of(visit)), complete } },
        {
          provide: VisitNotesService,
          useValue: {
            get: vi.fn(() => (notes === null ? throwError(() => new Error('x')) : of(notes))),
            saveComplaints,
            saveDiagnoses,
            saveMedications,
          },
        },
      ],
    });

    fixture = TestBed.createComponent(VisitNotesPageComponent);
    fixture.componentRef.setInput('visitId', '5');
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  it('fills the form with the saved complaints and diagnoses', () => {
    create(IN_PROGRESS, SAVED_NOTES);

    const page = fixture.componentInstance;
    expect(page.complaints.value).toEqual(['Cough']);
    expect(page.diagnoses.at(0).getRawValue()).toEqual({ text: 'Viral fever', code: 'J11' });
  });

  it('saves trimmed entries and drops the blank ones', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    const page = fixture.componentInstance;
    page.addComplaint('  Fever  ');
    page.addComplaint('   ');
    page.addDiagnosis('  Viral fever ', '  ');

    page.save();

    expect(saveComplaints).toHaveBeenCalledWith(5, ['Fever']);
    expect(saveDiagnoses).toHaveBeenCalledWith(5, [{ text: 'Viral fever', code: null }]);
  });

  it('fills the medications from the saved notes', () => {
    create(IN_PROGRESS, SAVED_NOTES);

    expect(fixture.componentInstance.medications.at(0).getRawValue()).toEqual({
      medicineName: 'Paracetamol',
      dosage: '500mg',
      frequency: 'Twice daily',
      duration: '7 days',
      specialInstructions: 'After food',
    });
  });

  it('does not save when a medicine has no dosage', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    const page = fixture.componentInstance;
    page.addMedication({
      medicineName: 'Paracetamol',
      dosage: '',
      frequency: 'Twice daily',
      duration: '7 days',
      specialInstructions: null,
    });

    page.save();

    expect(saveMedications).not.toHaveBeenCalled();
    expect(saveComplaints).not.toHaveBeenCalled();
  });

  it('saves medicines trimmed, and treats blank instructions as none', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    const page = fixture.componentInstance;
    page.addMedication({
      medicineName: '  Paracetamol ',
      dosage: ' 500mg',
      frequency: 'Twice daily ',
      duration: '7 days',
      specialInstructions: '  ',
    });

    page.save();

    expect(saveMedications).toHaveBeenCalledWith(5, [
      {
        medicineName: 'Paracetamol',
        dosage: '500mg',
        frequency: 'Twice daily',
        duration: '7 days',
        specialInstructions: null,
      },
    ]);
  });

  it('saves the drafts and then completes the visit', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    const page = fixture.componentInstance;
    page.addComplaint('Fever');

    page.completeVisit();

    expect(saveComplaints).toHaveBeenCalledWith(5, ['Fever']);
    expect(complete).toHaveBeenCalledWith(5);
  });

  it('shows the refusal inline and leaves the visit open when it has no vitals', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    complete.mockReturnValue(
      throwError(
        () =>
          new ApiRequestError(
            'business',
            422,
            'vitals_required',
            'Record the vitals before completing the visit.',
            null,
          ),
      ),
    );

    fixture.componentInstance.completeVisit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'Record the vitals before completing the visit.',
    );
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('shows the saved notes read-only, with no form, once the visit is no longer in progress', () => {
    create({ ...IN_PROGRESS, statusId: VISIT_STATUS_COMPLETED }, SAVED_NOTES);

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.querySelector('.notice')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Cough');
  });

  it('shows the server message inline when a save is refused', () => {
    create(IN_PROGRESS, EMPTY_NOTES);
    saveComplaints.mockReturnValue(
      throwError(
        () =>
          new ApiRequestError(
            'business',
            422,
            'visit_not_in_progress',
            'This visit is no longer in progress.',
            null,
          ),
      ),
    );
    fixture.componentInstance.addComplaint('Fever');

    fixture.componentInstance.save();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'This visit is no longer in progress.',
    );
  });
});
