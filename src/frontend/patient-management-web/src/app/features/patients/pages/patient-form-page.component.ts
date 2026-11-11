import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  merge,
  Observable,
  of,
  switchMap,
} from 'rxjs';

import { ApiRequestError } from '../../../core/http/api-request-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  PATIENT_BLOOD_GROUP_IDS,
  PATIENT_GENDER_OPTIONS,
  PatientRequest,
  PatientSummary,
} from '../../../core/models/patient.model';
import { AgePipe } from '../../../shared/pipes/age.pipe';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  dateOfBirthValidator,
  toIsoDate,
} from '../../../shared/validators/date-of-birth.validator';
import {
  normalizePhoneNumber,
  phoneNumberValidator,
} from '../../../shared/validators/phone-number.validator';
import { PatientsService } from '../services/patients.service';

const MAXIMUM_NAME_LENGTH = 200;
const MAXIMUM_ADDRESS_LENGTH = 500;
const MAXIMUM_CHRONIC_CONDITIONS_LENGTH = 1000;
const DUPLICATE_CHECK_DEBOUNCE_MS = 400;

interface DuplicateCriteria {
  readonly fullName: string;
  readonly dateOfBirth: string;
}

/**
 * Registers a new patient or edits an existing one. The duplicate warning uses name and date of birth and never
 * blocks the save: the doctor decides. The server checks every rule again; these validators give quick feedback.
 */
@Component({
  selector: 'app-patient-form-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    AgePipe,
    TranslatePipe,
  ],
  templateUrl: './patient-form-page.component.html',
  styleUrl: './patient-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientFormPageComponent implements OnInit {
  // Bound from the route (withComponentInputBinding). Absent when registering a new patient.
  readonly patientId = input<string>();

  private readonly patients = inject(PatientsService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly genderOptions = PATIENT_GENDER_OPTIONS;
  readonly bloodGroupIds = PATIENT_BLOOD_GROUP_IDS;
  readonly today = toIsoDate(new Date());
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly duplicates = signal<PatientSummary[]>([]);

  private readonly editingPatientId = signal<number | null>(null);

  readonly form = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(MAXIMUM_NAME_LENGTH)],
    }),
    dateOfBirth: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, dateOfBirthValidator()],
    }),
    genderId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [phoneNumberValidator()] }),
    address: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_ADDRESS_LENGTH)],
    }),
    bloodGroupId: new FormControl<number | null>(null),
    chronicConditions: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_CHRONIC_CONDITIONS_LENGTH)],
    }),
  });

  readonly dateOfBirthValue = toSignal(this.form.controls.dateOfBirth.valueChanges, {
    initialValue: this.form.controls.dateOfBirth.value,
  });

  ngOnInit(): void {
    const id = this.patientId();
    if (id !== undefined) {
      this.loadPatient(Number(id));
    }
    this.watchForDuplicates();
  }

  save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.toRequest();
    const editingId = this.editingPatientId();
    const call: Observable<unknown> =
      editingId === null ? this.patients.create(request) : this.patients.update(editingId, request);

    this.saving.set(true);
    this.saveError.set(null);
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notifications.showSuccess('patients.saved');
        void this.router.navigate(['/patients']);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.saveError.set(error instanceof ApiRequestError ? error.message : null);
      },
    });
  }

  private loadPatient(patientId: number): void {
    this.editingPatientId.set(patientId);
    this.loading.set(true);
    this.patients
      .get(patientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (patient) => {
          this.form.reset({
            fullName: patient.fullName,
            dateOfBirth: patient.dateOfBirth,
            genderId: patient.genderId,
            phoneNumber: patient.phoneNumber ?? '',
            address: patient.address ?? '',
            bloodGroupId: patient.bloodGroupId,
            chronicConditions: patient.chronicConditions ?? '',
          });
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  /** Looks for duplicates after the user pauses typing, and drops stale answers when the criteria change again. */
  private watchForDuplicates(): void {
    merge(this.form.controls.fullName.valueChanges, this.form.controls.dateOfBirth.valueChanges)
      .pipe(
        debounceTime(DUPLICATE_CHECK_DEBOUNCE_MS),
        map(() => this.duplicateCriteria()),
        distinctUntilChanged(
          (previous, current) =>
            previous?.fullName === current?.fullName &&
            previous?.dateOfBirth === current?.dateOfBirth,
        ),
        switchMap((criteria) => this.findDuplicates(criteria)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((matches) => this.duplicates.set(matches));
  }

  private findDuplicates(criteria: DuplicateCriteria | null): Observable<PatientSummary[]> {
    if (criteria === null) {
      return of([]);
    }
    return this.patients
      .findDuplicates(criteria.fullName, criteria.dateOfBirth, this.editingPatientId())
      .pipe(catchError(() => of([] as PatientSummary[])));
  }

  /** Both fields must be valid before a duplicate lookup, so the server never gets an incomplete question. */
  private duplicateCriteria(): DuplicateCriteria | null {
    const fullName = this.form.controls.fullName.value.trim();
    const dateOfBirth = this.form.controls.dateOfBirth.value;
    if (
      fullName === '' ||
      this.form.controls.fullName.invalid ||
      this.form.controls.dateOfBirth.invalid
    ) {
      return null;
    }
    return { fullName, dateOfBirth };
  }

  private toRequest(): PatientRequest {
    const raw = this.form.getRawValue();
    return {
      fullName: raw.fullName.trim(),
      dateOfBirth: raw.dateOfBirth,
      genderId: raw.genderId ?? 0,
      phoneNumber: normalizePhoneNumber(raw.phoneNumber),
      address: raw.address.trim() === '' ? null : raw.address.trim(),
      bloodGroupId: raw.bloodGroupId,
      chronicConditions: raw.chronicConditions.trim() === '' ? null : raw.chronicConditions.trim(),
    };
  }
}
