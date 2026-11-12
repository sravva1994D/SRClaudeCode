import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, of } from 'rxjs';

import {
  APPOINTMENT_STATUS_SCHEDULED,
  AppointmentListItem,
} from '../../core/models/appointment.model';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { agendaRange, DashboardAgenda, splitAgenda } from './dashboard-agenda';
import { DashboardService } from './dashboard.service';

/**
 * Today's appointments, the next week's scheduled appointments, and quick actions. Recent patients are deferred
 * (plan Sprint 5–6, item 29), so they are not shown here.
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, RouterLink, MatButtonModule, MatProgressBarModule, TranslatePipe],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboard = inject(DashboardService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly agenda = signal<DashboardAgenda>({ today: [], upcoming: [] });
  /** The appointment whose visit is being started, so a second click cannot start it twice. */
  readonly startingAppointmentId = signal<number | null>(null);

  ngOnInit(): void {
    const now = new Date();
    const range = agendaRange(now);
    this.dashboard
      .appointments(range.from, range.to)
      .pipe(
        catchError(() => {
          this.loadFailed.set(true);
          return of([] as AppointmentListItem[]);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((items) => {
        this.agenda.set(splitAgenda(items, now));
        this.loading.set(false);
      });
  }

  /** Translation key for an appointment status, from the seeded AppointmentStatuses table. */
  statusKey(statusId: number): string {
    return `appointments.status.${statusId}`;
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
    this.dashboard
      .startVisit(item.appointmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (visit) => {
          void this.router.navigate(['/consultation/visits', visit.visitId, 'vitals']);
        },
        error: () => this.startingAppointmentId.set(null),
      });
  }
}
