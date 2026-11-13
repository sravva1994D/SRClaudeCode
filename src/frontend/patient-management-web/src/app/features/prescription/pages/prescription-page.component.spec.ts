import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';

import { ApiRequestError } from '../../../core/http/api-request-error';
import {
  PrescriptionDocument,
  PrescriptionPrint,
  PrescriptionPrintSummary,
} from '../../../core/models/prescription.model';
import { PrescriptionService } from '../services/prescription.service';
import { PrescriptionPageComponent } from './prescription-page.component';

const CURRENT: PrescriptionDocument = {
  prescription: { prescriptionId: 9, visitId: 5, prescriptionDate: '2026-11-12' },
  clinic: {
    clinicName: 'Riverside Clinic',
    doctorName: 'Dr. Test',
    qualifications: 'MBBS',
    registrationNumber: 'REG-1',
    address: null,
    phoneNumber: null,
    footerNotes: null,
  },
  patient: { fullName: 'Asha Rao', dateOfBirth: '1990-05-14', phoneNumber: null },
  vitals: {
    temperature: null,
    bloodPressureSystolic: null,
    bloodPressureDiastolic: null,
    pulse: 72,
  },
  diagnoses: [],
  medications: [
    {
      medicineName: 'Amoxicillin',
      dosage: '250mg',
      frequency: 'Thrice daily',
      duration: '5 days',
      specialInstructions: null,
    },
  ],
};

const PRINTED_BEFORE: PrescriptionDocument = {
  ...CURRENT,
  medications: [
    {
      medicineName: 'Paracetamol',
      dosage: '500mg',
      frequency: 'Twice daily',
      duration: '7 days',
      specialInstructions: null,
    },
  ],
};

const EARLIER: PrescriptionPrintSummary = {
  prescriptionPrintId: 4,
  printedAt: '2026-11-12T10:00:00Z',
};

describe('PrescriptionPageComponent', () => {
  let fixture: ComponentFixture<PrescriptionPageComponent>;
  let get: ReturnType<typeof vi.fn>;
  let issue: ReturnType<typeof vi.fn>;
  let recordPrint: ReturnType<typeof vi.fn>;
  let listPrints: ReturnType<typeof vi.fn>;
  let getPrint: ReturnType<typeof vi.fn>;
  let printDialog: ReturnType<typeof vi.spyOn>;

  function create(
    document: PrescriptionDocument | null,
    prints: PrescriptionPrintSummary[] = [],
  ): void {
    get = vi.fn(() =>
      document === null
        ? throwError(
            () =>
              new ApiRequestError(
                'request',
                404,
                'record_not_found',
                'Prescription was not found.',
                null,
              ),
          )
        : of(document),
    );
    issue = vi.fn(() => of(CURRENT.prescription));
    recordPrint = vi.fn();
    listPrints = vi.fn(() => of(prints));
    getPrint = vi.fn();

    TestBed.configureTestingModule({
      imports: [PrescriptionPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        provideRouter([]),
        {
          provide: PrescriptionService,
          useValue: { get, issue, recordPrint, listPrints, getPrint },
        },
      ],
    });

    fixture = TestBed.createComponent(PrescriptionPageComponent);
    fixture.componentRef.setInput('visitId', '5');
    fixture.detectChanges();
  }

  /** Runs the work scheduled after the next render, such as opening the print dialog. */
  function flushRender(): void {
    fixture.detectChanges();
    TestBed.inject(ApplicationRef).tick();
  }

  beforeEach(() => {
    printDialog = vi.spyOn(window, 'print').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  it('shows the issued prescription in the print layout, with its earlier prints', () => {
    create(CURRENT, [EARLIER]);

    expect(fixture.nativeElement.querySelector('.prescription-print-area')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Amoxicillin');
    expect(fixture.nativeElement.textContent).toContain('Riverside Clinic');
    expect(fixture.nativeElement.querySelectorAll('.reprints li')).toHaveLength(1);
    expect(printDialog).not.toHaveBeenCalled();
  });

  it('offers to issue the prescription when none has been issued, and issues it on request', () => {
    create(null);

    expect(fixture.nativeElement.querySelector('.prescription-print-area')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('prescription.notIssued');

    fixture.componentInstance.issue();

    expect(issue).toHaveBeenCalledWith(5);
    expect(get).toHaveBeenCalledTimes(2);
  });

  it('records a print of the current content, shows what was printed, and opens the print dialog', () => {
    create(CURRENT);
    recordPrint.mockReturnValue(
      of({
        prescriptionPrintId: 6,
        printedAt: '2026-11-12T11:00:00Z',
        document: CURRENT,
      } satisfies PrescriptionPrint),
    );

    fixture.componentInstance.print();
    flushRender();

    expect(recordPrint).toHaveBeenCalledWith(5);
    expect(fixture.nativeElement.querySelector('.notice.no-print')?.textContent).toContain(
      'prescription.printedContent',
    );
    expect(printDialog).toHaveBeenCalledTimes(1);
  });

  it('reprints an earlier print from its snapshot, not from the current records', () => {
    create(CURRENT, [EARLIER]);
    getPrint.mockReturnValue(
      of({
        prescriptionPrintId: 4,
        printedAt: EARLIER.printedAt,
        document: PRINTED_BEFORE,
      } satisfies PrescriptionPrint),
    );

    fixture.componentInstance.reprint(EARLIER);
    flushRender();

    expect(getPrint).toHaveBeenCalledWith(5, 4);
    expect(fixture.nativeElement.querySelector('.prescription-print-area').textContent).toContain(
      'Paracetamol',
    );
    expect(
      fixture.nativeElement.querySelector('.prescription-print-area').textContent,
    ).not.toContain('Amoxicillin');
    expect(printDialog).toHaveBeenCalledTimes(1);
  });
});
