import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';

import { PatientAppointment } from '../../../core/models/appointment.model';
import { PagedResponse, Patient, PATIENT_GENDER_OPTIONS } from '../../../core/models/patient.model';
import { AgePipe } from '../../../shared/pipes/age.pipe';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PatientsService } from '../services/patients.service';

const APPOINTMENTS_PAGE_SIZE = 10;

/**
 * Shows one patient's details, their appointment history, and records the visit so the patient appears in the user's
 * recent list. The visit summary is still a placeholder until Phases 3 and 4 add visits.
 */
@Component({
  selector: 'app-patient-profile-page',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatPaginatorModule,
    MatProgressBarModule,
    AgePipe,
    TranslatePipe,
  ],
  templateUrl: './patient-profile-page.component.html',
  styleUrl: './patient-profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientProfilePageComponent implements OnInit {
  // Bound from the route (withComponentInputBinding).
  readonly patientId = input.required<string>();

  private readonly patients = inject(PatientsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly appointmentsPageSize = APPOINTMENTS_PAGE_SIZE;
  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly patient = signal<Patient | null>(null);
  readonly history = signal<PagedResponse<PatientAppointment> | null>(null);
  readonly historyFailed = signal(false);
  readonly historyPageNumber = signal(1);

  ngOnInit(): void {
    const id = Number(this.patientId());
    this.patients
      .get(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (patient) => {
          this.patient.set(patient);
          this.loading.set(false);
        },
        error: () => {
          this.loadFailed.set(true);
          this.loading.set(false);
        },
      });

    this.loadHistory(id, 1);

    // Best effort: a failed record of the visit must not hide the patient's details.
    this.patients
      .markViewed(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  /** Paginator events are zero-based; the API counts pages from one. */
  changeHistoryPage(event: PageEvent): void {
    this.loadHistory(Number(this.patientId()), event.pageIndex + 1);
  }

  genderLabelKey(genderId: number): string {
    return PATIENT_GENDER_OPTIONS.find((option) => option.id === genderId)?.labelKey ?? '';
  }

  /** Translation key for an appointment status, from the seeded AppointmentStatuses table. */
  statusKey(statusId: number): string {
    return `appointments.status.${statusId}`;
  }

  private loadHistory(patientId: number, pageNumber: number): void {
    this.historyPageNumber.set(pageNumber);
    this.historyFailed.set(false);
    this.patients
      .appointmentHistory(patientId, pageNumber, APPOINTMENTS_PAGE_SIZE)
      .pipe(
        catchError(() => {
          this.historyFailed.set(true);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => this.history.set(page));
  }
}
