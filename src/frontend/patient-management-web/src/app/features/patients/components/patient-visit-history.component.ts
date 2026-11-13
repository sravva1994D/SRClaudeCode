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
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';

import { PagedResponse } from '../../../core/models/patient.model';
import { PatientVisit, VisitDateRange } from '../../../core/models/patient-visit.model';
import { VISIT_STATUS_COMPLETED, VISIT_STATUS_IN_PROGRESS } from '../../../core/models/visit.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PatientVisitsService } from '../services/patient-visits.service';

const VISITS_PAGE_SIZE = 20;

/**
 * A patient's visits, newest first, with an optional date range (plan Sprint 9, item 42). The range is applied only when
 * the doctor presses Apply, so typing a date does not reload the list.
 */
@Component({
  selector: 'app-patient-visit-history',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    TranslatePipe,
  ],
  templateUrl: './patient-visit-history.component.html',
  styleUrl: './patient-visit-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientVisitHistoryComponent implements OnInit {
  // The patient's id as the route gives it.
  readonly patientId = input.required<string>();

  private readonly visits = inject(PatientVisitsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly visitsPageSize = VISITS_PAGE_SIZE;
  readonly page = signal<PagedResponse<PatientVisit> | null>(null);
  readonly failed = signal(false);
  readonly pageNumber = signal(1);
  /** The range the list currently shows. The form can hold a different, not yet applied range. */
  readonly appliedRange = signal<VisitDateRange>({ from: '', to: '' });
  /** True when the end day in the form is before the start day, so nothing is applied. */
  readonly rangeError = signal(false);

  readonly range = new FormGroup({
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    this.load(1);
  }

  statusKey(statusId: number): string {
    return statusId === VISIT_STATUS_COMPLETED
      ? 'patients.visitStatus.completed'
      : statusId === VISIT_STATUS_IN_PROGRESS
        ? 'patients.visitStatus.inProgress'
        : 'patients.visitStatus.unknown';
  }

  /** Applies the date range the form holds, from the first page. An end day before the start day is shown inline. */
  apply(): void {
    const next = this.range.getRawValue();
    const inverted = next.from !== '' && next.to !== '' && next.to < next.from;
    this.rangeError.set(inverted);
    if (inverted) {
      return;
    }
    this.appliedRange.set(next);
    this.load(1);
  }

  clear(): void {
    this.range.reset({ from: '', to: '' });
    this.rangeError.set(false);
    this.appliedRange.set({ from: '', to: '' });
    this.load(1);
  }

  changePage(event: PageEvent): void {
    this.load(event.pageIndex + 1);
  }

  private load(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.failed.set(false);
    this.visits
      .list(Number(this.patientId()), this.appliedRange(), pageNumber, VISITS_PAGE_SIZE)
      .pipe(
        catchError(() => {
          this.failed.set(true);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((page) => this.page.set(page));
  }
}
