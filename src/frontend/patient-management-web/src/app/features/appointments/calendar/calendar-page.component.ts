import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  Observable,
  of,
  switchMap,
  tap,
} from 'rxjs';

import {
  APPOINTMENT_STATUS_SCHEDULED,
  AppointmentListItem,
} from '../../../core/models/appointment.model';
import { PatientListItem } from '../../../core/models/patient.model';
import { ApiRequestError } from '../../../core/http/api-request-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PatientsService } from '../../patients/services/patients.service';
import { AppointmentsService } from '../services/appointments.service';
import { addDays, isPastSlot, slotKey, slotsForDay, startOfWeek, weekDays } from './calendar-slots';

const PATIENT_SEARCH_DEBOUNCE_MS = 300;
const PATIENT_MATCH_LIMIT = 10;

interface CalendarRange {
  readonly from: Date;
  readonly to: Date;
}

/**
 * Day and week calendar of 15-minute slots. A booked slot shows the patient's name. A free future slot can be booked
 * by choosing a patient. Slots are shown and chosen in local time and sent to the API as UTC.
 */
@Component({
  selector: 'app-calendar-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslatePipe,
  ],
  templateUrl: './calendar-page.component.html',
  styleUrl: './calendar-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarPageComponent implements OnInit {
  private readonly appointments = inject(AppointmentsService);
  private readonly patients = inject(PatientsService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly view = signal<'day' | 'week'>('day');
  readonly anchor = signal<Date>(startOfDay(new Date()));
  readonly now = signal<Date>(new Date());
  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly appointmentList = signal<AppointmentListItem[]>([]);

  readonly days = computed<Date[]>(() =>
    this.view() === 'day' ? [this.anchor()] : weekDays(startOfWeek(this.anchor())),
  );
  readonly timeSlots = computed<Date[]>(() => slotsForDay(this.anchor()));

  private readonly bookedBySlot = computed(
    () =>
      new Map(this.appointmentList().map((item) => [slotKey(new Date(item.slotStartUtc)), item])),
  );

  readonly selectedSlot = signal<Date | null>(null);
  readonly patientSearch = new FormControl('', { nonNullable: true });
  readonly patientMatches = signal<PatientListItem[]>([]);
  readonly chosenPatient = signal<PatientListItem | null>(null);
  readonly booking = signal(false);
  readonly bookError = signal<string | null>(null);
  /** The appointment whose visit is being started, so a second click cannot start it twice. */
  readonly startingAppointmentId = signal<number | null>(null);

  private readonly range = new BehaviorSubject<CalendarRange>(this.currentRange());

  ngOnInit(): void {
    this.range
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadFailed.set(false);
          this.now.set(new Date());
        }),
        switchMap((range) => this.loadRange(range)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((items) => {
        this.appointmentList.set(items);
        this.loading.set(false);
      });

    this.patientSearch.valueChanges
      .pipe(
        debounceTime(PATIENT_SEARCH_DEBOUNCE_MS),
        map((term) => term.trim()),
        distinctUntilChanged(),
        switchMap((term) => this.searchPatients(term)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((matches) => this.patientMatches.set(matches));
  }

  setView(view: 'day' | 'week'): void {
    this.view.set(view);
    this.reload();
  }

  shift(direction: -1 | 1): void {
    const days = this.view() === 'day' ? 1 : 7;
    this.anchor.set(addDays(this.anchor(), direction * days));
    this.reload();
  }

  goToToday(): void {
    this.anchor.set(startOfDay(new Date()));
    this.reload();
  }

  /** The local Date for one cell: the given day at the given time of day. */
  cellTime(day: Date, slot: Date): Date {
    return new Date(
      day.getFullYear(),
      day.getMonth(),
      day.getDate(),
      slot.getHours(),
      slot.getMinutes(),
    );
  }

  /** The appointment booked in this cell, if any. */
  bookedAt(day: Date, slot: Date): AppointmentListItem | undefined {
    return this.bookedBySlot().get(slotKey(this.cellTime(day, slot)));
  }

  isBookable(day: Date, slot: Date): boolean {
    return !this.bookedAt(day, slot) && !isPastSlot(this.cellTime(day, slot), this.now());
  }

  /** Only a scheduled appointment can start a visit; the API refuses the others (422). */
  canStartVisit(item: AppointmentListItem): boolean {
    return item.statusId === APPOINTMENT_STATUS_SCHEDULED;
  }

  /** Starts the visit and opens the vitals page for it. Failures are shown by the error interceptor. */
  startVisit(item: AppointmentListItem): void {
    if (this.startingAppointmentId() !== null) {
      return;
    }

    this.startingAppointmentId.set(item.appointmentId);
    this.appointments
      .startVisit(item.appointmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (visit) => {
          void this.router.navigate(['/consultation/visits', visit.visitId, 'vitals']);
        },
        error: () => this.startingAppointmentId.set(null),
      });
  }

  selectSlot(day: Date, slot: Date): void {
    this.selectedSlot.set(this.cellTime(day, slot));
    this.patientSearch.setValue('', { emitEvent: false });
    this.patientMatches.set([]);
    this.chosenPatient.set(null);
    this.bookError.set(null);
  }

  choosePatient(patient: PatientListItem): void {
    this.chosenPatient.set(patient);
    this.patientMatches.set([]);
  }

  cancelBooking(): void {
    this.selectedSlot.set(null);
    this.chosenPatient.set(null);
    this.bookError.set(null);
  }

  confirmBooking(): void {
    const slot = this.selectedSlot();
    const patient = this.chosenPatient();
    if (slot === null || patient === null || this.booking()) {
      return;
    }

    this.booking.set(true);
    this.bookError.set(null);
    this.appointments
      .schedule(patient.patientId, slot)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.booking.set(false);
          this.cancelBooking();
          this.notifications.showSuccess('appointments.booked');
          this.reload();
        },
        error: (error: unknown) => {
          this.booking.set(false);
          this.bookError.set(error instanceof ApiRequestError ? error.message : null);
        },
      });
  }

  private reload(): void {
    this.range.next(this.currentRange());
  }

  /** The range to load: the anchor day for the day view, or the Monday to the next Monday for the week view. */
  private currentRange(): CalendarRange {
    const anchor = this.anchor();
    if (this.view() === 'day') {
      return { from: anchor, to: addDays(anchor, 1) };
    }
    const monday = startOfWeek(anchor);
    return { from: monday, to: addDays(monday, 7) };
  }

  private loadRange(range: CalendarRange): Observable<AppointmentListItem[]> {
    return this.appointments.list(range.from, range.to).pipe(
      catchError(() => {
        this.loadFailed.set(true);
        return of([] as AppointmentListItem[]);
      }),
    );
  }

  private searchPatients(term: string): Observable<PatientListItem[]> {
    if (term === '') {
      return of([]);
    }
    return this.patients.list({ search: term, page: 1, pageSize: PATIENT_MATCH_LIMIT }).pipe(
      map((page) => page.items),
      catchError(() => of([] as PatientListItem[])),
    );
  }
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}
