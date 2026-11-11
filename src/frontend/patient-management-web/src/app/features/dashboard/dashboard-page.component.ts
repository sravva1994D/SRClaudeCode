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
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, of } from 'rxjs';

import { AppointmentListItem } from '../../core/models/appointment.model';
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

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly agenda = signal<DashboardAgenda>({ today: [], upcoming: [] });

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
}
