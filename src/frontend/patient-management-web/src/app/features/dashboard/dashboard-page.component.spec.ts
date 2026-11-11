import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AppointmentListItem } from '../../core/models/appointment.model';
import { DashboardPageComponent } from './dashboard-page.component';
import { DashboardService } from './dashboard.service';

describe('DashboardPageComponent', () => {
  let fixture: ComponentFixture<DashboardPageComponent>;
  let appointments: ReturnType<typeof vi.fn>;

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
        { provide: DashboardService, useValue: { appointments } },
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
});
