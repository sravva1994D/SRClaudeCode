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
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  Observable,
  of,
  switchMap,
  tap,
} from 'rxjs';

import {
  PagedResponse,
  PatientListItem,
  PATIENT_GENDER_OPTIONS,
} from '../../../core/models/patient.model';
import { AgePipe } from '../../../shared/pipes/age.pipe';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PatientListQuery, PatientsService } from '../services/patients.service';

const PAGE_SIZE = 20;
const SEARCH_DEBOUNCE_MS = 300;
const MAXIMUM_SEARCH_LENGTH = 100;

/**
 * Lists active patients a page at a time. Typing narrows the list by name or phone digits. Each change of search or
 * page cancels the request still in flight, so a slow answer never overwrites a newer one.
 */
@Component({
  selector: 'app-patient-list-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    AgePipe,
    TranslatePipe,
  ],
  templateUrl: './patient-list-page.component.html',
  styleUrl: './patient-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientListPageComponent implements OnInit {
  private readonly patients = inject(PatientsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = PAGE_SIZE;
  readonly searchControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(MAXIMUM_SEARCH_LENGTH)],
  });
  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly page = signal<PagedResponse<PatientListItem> | null>(null);

  private readonly query = new BehaviorSubject<PatientListQuery>({
    search: '',
    page: 1,
    pageSize: PAGE_SIZE,
  });

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((search) => {
        if (this.searchControl.valid) {
          this.query.next({ ...this.query.value, search, page: 1 });
        }
      });

    this.query
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadFailed.set(false);
        }),
        switchMap((query) => this.load(query)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.page.set(result);
        this.loading.set(false);
      });
  }

  /** Paginator events are zero-based; the API counts pages from one. */
  changePage(event: PageEvent): void {
    this.query.next({ ...this.query.value, page: event.pageIndex + 1 });
  }

  genderLabelKey(genderId: number): string {
    return PATIENT_GENDER_OPTIONS.find((option) => option.id === genderId)?.labelKey ?? '';
  }

  private load(query: PatientListQuery): Observable<PagedResponse<PatientListItem> | null> {
    return this.patients.list(query).pipe(
      catchError(() => {
        this.loadFailed.set(true);
        return of(null);
      }),
    );
  }
}
