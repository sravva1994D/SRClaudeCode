import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AppointmentListItem } from '../../../core/models/appointment.model';
import { AppointmentsService } from '../services/appointments.service';
import { PatientsService } from '../../patients/services/patients.service';
import { CalendarPageComponent } from './calendar-page.component';

describe('CalendarPageComponent', () => {
  let fixture: ComponentFixture<CalendarPageComponent>;
  let list: ReturnType<typeof vi.fn>;
  let startVisit: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    // Today at 10:00 local time, which is inside the calendar's hours.
    const today = new Date();
    const booked: AppointmentListItem = {
      appointmentId: 1,
      patientId: 3,
      patientFullName: 'Asha Rao',
      slotStartUtc: new Date(
        today.getFullYear(),
        today.getMonth(),
        today.getDate(),
        10,
        0,
      ).toISOString(),
      statusId: 1,
      visitId: null,
    };
    list = vi.fn(() => of([booked]));
    startVisit = vi.fn(() => of({ visitId: 9, patientId: 3, appointmentId: 1, statusId: 1 }));

    TestBed.configureTestingModule({
      imports: [CalendarPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppointmentsService, useValue: { list, schedule: vi.fn(), startVisit } },
        {
          provide: PatientsService,
          useValue: {
            list: vi.fn(() => of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10 })),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(CalendarPageComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the day range and shows the booked patient in the grid', () => {
    expect(list).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.booked')?.textContent).toContain('Asha Rao');
  });

  it('offers Start visit on a booked appointment that has no visit yet', () => {
    const action = fixture.nativeElement.querySelector('.visit-action') as HTMLElement;

    expect(action.tagName).toBe('BUTTON');
    expect(action.textContent).toContain('appointments.startVisit');
  });

  it('offers Open visit, not Start visit, when the appointment already has a visit', () => {
    fixture.componentInstance.appointmentList.update((items) =>
      items.map((item) => ({ ...item, visitId: 7 })),
    );
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('.visit-action') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('/consultation/visits/7/vitals');
  });

  it('starts the visit and opens its vitals page', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    (fixture.nativeElement.querySelector('.visit-action') as HTMLButtonElement).click();

    expect(startVisit).toHaveBeenCalledWith(1);
    expect(navigate).toHaveBeenCalledWith(['/consultation/visits', 9, 'vitals']);
  });

  it('opens the booking panel when a free future slot is chosen', () => {
    // Tomorrow has no bookings and every slot is in the future, so the test does not depend on the time of day.
    const tomorrow = new Date();
    fixture.componentInstance.anchor.set(
      new Date(tomorrow.getFullYear(), tomorrow.getMonth(), tomorrow.getDate() + 1),
    );
    fixture.detectChanges();
    const freeButton = fixture.nativeElement.querySelector('button.free') as HTMLButtonElement;
    freeButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.selectedSlot()).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.booking')).not.toBeNull();
  });
});
