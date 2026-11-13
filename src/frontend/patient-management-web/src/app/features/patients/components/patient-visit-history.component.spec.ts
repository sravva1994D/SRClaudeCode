import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';

import { PagedResponse } from '../../../core/models/patient.model';
import { PatientVisit } from '../../../core/models/patient-visit.model';
import { VISIT_STATUS_COMPLETED, VISIT_STATUS_IN_PROGRESS } from '../../../core/models/visit.model';
import { PatientVisitsService } from '../services/patient-visits.service';
import { PatientVisitHistoryComponent } from './patient-visit-history.component';

const VISITS: PatientVisit[] = [
  {
    visitId: 12,
    startedAt: '2026-11-12T09:00:00Z',
    statusId: VISIT_STATUS_IN_PROGRESS,
    modifiedDate: '2026-11-12T09:00:00Z',
    appointmentId: 4,
  },
  {
    visitId: 7,
    startedAt: '2026-10-02T09:00:00Z',
    statusId: VISIT_STATUS_COMPLETED,
    modifiedDate: '2026-10-02T10:00:00Z',
    appointmentId: null,
  },
];

function page(items: PatientVisit[]): PagedResponse<PatientVisit> {
  return { items, totalCount: items.length, pageNumber: 1, pageSize: 20 };
}

describe('PatientVisitHistoryComponent', () => {
  let fixture: ComponentFixture<PatientVisitHistoryComponent>;
  let list: ReturnType<typeof vi.fn>;

  function create(items: PatientVisit[]): void {
    list = vi.fn(() => of(page(items)));

    TestBed.configureTestingModule({
      imports: [PatientVisitHistoryComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: PatientVisitsService, useValue: { list } },
      ],
    });

    fixture = TestBed.createComponent(PatientVisitHistoryComponent);
    fixture.componentRef.setInput('patientId', '7');
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  it('lists the visits in the order the server gives them, with their status', () => {
    create(VISITS);

    const rows = fixture.nativeElement.querySelectorAll('.rows .row');
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelector('.status').textContent).toContain(
      'patients.visitStatus.inProgress',
    );
    expect(rows[1].querySelector('.status').textContent).toContain(
      'patients.visitStatus.completed',
    );
    expect(list).toHaveBeenCalledWith(7, { from: '', to: '' }, 1, 20);
  });

  it('applies the date range the doctor enters, from the first page', () => {
    create(VISITS);
    const component = fixture.componentInstance;
    component.range.setValue({ from: '2026-10-01', to: '2026-10-31' });

    component.apply();

    expect(list).toHaveBeenLastCalledWith(7, { from: '2026-10-01', to: '2026-10-31' }, 1, 20);
  });

  it('refuses a range whose end day is before its start day, and applies nothing', () => {
    create(VISITS);
    const component = fixture.componentInstance;
    list.mockClear();
    component.range.setValue({ from: '2026-11-30', to: '2026-11-01' });

    component.apply();
    fixture.detectChanges();

    expect(list).not.toHaveBeenCalled();
    expect(component.rangeError()).toBe(true);
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'patients.visitsRangeInvalid',
    );
  });

  it('clears the range and shows the whole history again', () => {
    create(VISITS);
    const component = fixture.componentInstance;
    component.range.setValue({ from: '2026-10-01', to: '2026-10-31' });
    component.apply();

    component.clear();

    expect(component.range.getRawValue()).toEqual({ from: '', to: '' });
    expect(list).toHaveBeenLastCalledWith(7, { from: '', to: '' }, 1, 20);
  });

  it('shows the empty message when no visit falls in the period', () => {
    create([]);

    expect(fixture.nativeElement.querySelector('.empty').textContent).toContain(
      'patients.noVisits',
    );
    expect(fixture.nativeElement.querySelector('.rows')).toBeNull();
  });
});
