import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import {
  APPOINTMENT_STATUS_SCHEDULED,
  AppointmentListItem,
} from '../../core/models/appointment.model';
import { Visit } from '../../core/models/visit.model';
import { DashboardPageComponent } from './dashboard-page.component';
import { DashboardService } from './dashboard.service';

describe('DashboardPageComponent', () => {
  let fixture: ComponentFixture<DashboardPageComponent>;
  let appointments: ReturnType<typeof vi.fn>;
  let startVisit: ReturnType<typeof vi.fn>;

  function item(id: number, when: Date, statusId: number, name: string): AppointmentListItem {
    return {
      appointmentId: id,
      patientId: id,
      patientFullName: name,
      slotStartUtc: when.toISOString(),
      statusId,
    };
  }

  function createPage(): void {
    TestBed.configureTestingModule({
      imports: [DashboardPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        { provide: DashboardService, useValue: { appointments, startVisit } },
      ],
    });
    fixture = TestBed.createComponent(DashboardPageComponent);
    fixture.detectChanges();
  }

  beforeEach(() => {
    const today = new Date();
    const tomorrow = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1, 9, 0);
    appointments = vi.fn(() =>
      of([
        item(
          1,
          new Date(today.getFullYear(), today.getMonth(), today.getDate(), 15, 0),
          1,
          'Today Patient',
        ),
        item(2, tomorrow, 1, 'Tomorrow Patient'),
      ]),
    );
    startVisit = vi.fn(() => of({ visitId: 9, patientId: 1, appointmentId: 1, statusId: 1 }));
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows today and upcoming appointments with their patients', () => {
    createPage();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Today Patient');
    expect(text).toContain('Tomorrow Patient');
    expect(appointments).toHaveBeenCalledTimes(1);
  });

  it('offers the quick actions for a new patient, finding a patient, and the calendar', () => {
    createPage();

    const links = Array.from(
      fixture.nativeElement.querySelectorAll('.quick-actions a'),
    ) as HTMLAnchorElement[];
    const hrefs = links.map((link) => link.getAttribute('href'));
    expect(hrefs).toEqual(['/patients/new', '/patients', '/appointments']);
  });

  it('shows an error message when the appointments cannot be loaded', () => {
    appointments.mockReturnValue(throwError(() => new Error('network')));

    createPage();

    expect(fixture.nativeElement.querySelector('.error')).not.toBeNull();
  });

  it("offers a start-visit button only for today's scheduled appointments", () => {
    createPage();

    const buttons = fixture.nativeElement.querySelectorAll(
      '.agenda button',
    ) as NodeListOf<HTMLButtonElement>;
    expect(buttons).toHaveLength(1);
    expect(buttons[0].textContent).toContain('dashboard.startVisit');
  });

  it('starts the visit and opens its vitals page', () => {
    createPage();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fixture.nativeElement.querySelector('.agenda button').click();

    expect(startVisit).toHaveBeenCalledWith(1);
    expect(navigate).toHaveBeenCalledWith(['/consultation/visits', 9, 'vitals']);
  });

  it('starts a visit only once while the first request is in flight', () => {
    startVisit.mockReturnValue(new Subject<Visit>());
    createPage();
    const component = fixture.componentInstance;

    component.startVisit(item(1, new Date(), APPOINTMENT_STATUS_SCHEDULED, 'Today Patient'));
    component.startVisit(item(1, new Date(), APPOINTMENT_STATUS_SCHEDULED, 'Today Patient'));

    expect(startVisit).toHaveBeenCalledTimes(1);
  });
});
