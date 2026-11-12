import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { forkJoin, of, catchError } from 'rxjs';

import { TranslationService } from '../../../core/i18n/translation.service';
import { ApiRequestError } from '../../../core/http/api-request-error';
import { HasUnsavedChanges } from '../../../core/guards/unsaved-changes.guard';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  RecordVitalsRequest,
  VITALS_IMPLAUSIBLE_CODE,
  Vitals,
} from '../../../core/models/vitals.model';
import { Visit, VISIT_STATUS_IN_PROGRESS } from '../../../core/models/visit.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import {
  atLeastOneVitalValidator,
  vitalValueValidator,
} from '../../../shared/validators/vital-value.validator';
import { VisitsService } from '../services/visits.service';
import { VitalsService } from '../services/vitals.service';

const BLOOD_PRESSURE_MAXIMUM = 999;
const PULSE_MAXIMUM = 999;
const TEMPERATURE_MAXIMUM = 999.9;

/** Translation keys for the sign names the server returns in warnings (its VitalSigns constants). */
const SIGN_LABEL_KEYS: Readonly<Record<string, string>> = {
  Temperature: 'consultation.vitals.temperature',
  BloodPressureSystolic: 'consultation.vitals.systolic',
  BloodPressureDiastolic: 'consultation.vitals.diastolic',
  Pulse: 'consultation.vitals.pulse',
};

/**
 * Records the vitals for a visit, and corrects them later at any visit status (plan item 38). A value outside its normal
 * range is saved and shown as a warning. A value outside its plausible range is refused with an inline message, and
 * nothing is saved. The server enforces both rules; the form only checks that each entry can be stored.
 */
@Component({
  selector: 'app-visit-vitals-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslatePipe,
  ],
  templateUrl: './visit-vitals-page.component.html',
  styleUrl: './visit-vitals-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitVitalsPageComponent implements OnInit, HasUnsavedChanges {
  // Bound from the route (withComponentInputBinding).
  readonly visitId = input.required<string>();

  private readonly visits = inject(VisitsService);
  private readonly vitalsService = inject(VitalsService);
  private readonly notifications = inject(NotificationService);
  private readonly translations = inject(TranslationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly visitMissing = signal(false);
  readonly visit = signal<Visit | null>(null);
  readonly recorded = signal<Vitals | null>(null);
  readonly warnings = signal<readonly string[]>([]);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly submitted = signal(false);
  /** True while recorded vitals are open for correction in the form. */
  readonly editing = signal(false);

  /** The names of the signs outside their normal range, translated, for the warning banner. */
  readonly warningLabels = computed(() =>
    this.warnings()
      .map((sign) => this.translations.translate(SIGN_LABEL_KEYS[sign] ?? sign))
      .join(', '),
  );

  readonly notInProgress = computed(
    () => this.visit() !== null && this.visit()?.statusId !== VISIT_STATUS_IN_PROGRESS,
  );

  readonly form = new FormGroup(
    {
      temperature: new FormControl<number | null>(null, {
        validators: [vitalValueValidator({ wholeNumber: false, maximum: TEMPERATURE_MAXIMUM })],
      }),
      bloodPressureSystolic: new FormControl<number | null>(null, {
        validators: [vitalValueValidator({ wholeNumber: true, maximum: BLOOD_PRESSURE_MAXIMUM })],
      }),
      bloodPressureDiastolic: new FormControl<number | null>(null, {
        validators: [vitalValueValidator({ wholeNumber: true, maximum: BLOOD_PRESSURE_MAXIMUM })],
      }),
      pulse: new FormControl<number | null>(null, {
        validators: [vitalValueValidator({ wholeNumber: true, maximum: PULSE_MAXIMUM })],
      }),
    },
    { validators: [atLeastOneVitalValidator] },
  );

  ngOnInit(): void {
    this.load();
  }

  /** The entry form holds edits that are not saved. The read-only summary has none. */
  hasUnsavedChanges(): boolean {
    return (this.recorded() === null || this.editing()) && this.form.dirty;
  }

  /** Opens the recorded values in the form so they can be corrected. */
  startEditing(): void {
    const vitals = this.recorded();
    if (vitals === null) {
      return;
    }
    this.form.reset({
      temperature: vitals.temperature,
      bloodPressureSystolic: vitals.bloodPressureSystolic,
      bloodPressureDiastolic: vitals.bloodPressureDiastolic,
      pulse: vitals.pulse,
    });
    this.submitted.set(false);
    this.saveError.set(null);
    this.editing.set(true);
  }

  cancelEditing(): void {
    this.form.reset();
    this.submitted.set(false);
    this.saveError.set(null);
    this.editing.set(false);
  }

  save(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.saving()) {
      return;
    }

    const visitId = Number(this.visitId());
    const request = this.toRequest();
    const correcting = this.recorded() !== null;
    this.saving.set(true);
    this.saveError.set(null);
    const save = correcting
      ? this.vitalsService.update(visitId, request)
      : this.vitalsService.record(visitId, request);
    save.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.recorded.set(response.vitals);
        this.warnings.set(response.warnings);
        this.editing.set(false);
        this.form.reset();
        this.refreshVisit();
        if (response.warnings.length === 0) {
          this.notifications.showSuccess('consultation.vitals.saved');
        }
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.saveError.set(this.recordErrorMessage(error));
        // Someone else (or an earlier attempt) already saved vitals for this visit: show what is stored.
        if (error instanceof ApiRequestError && error.code === 'vitals_already_recorded') {
          this.load();
        }
      },
    });
  }

  private load(): void {
    const visitId = Number(this.visitId());
    this.loading.set(true);
    this.visitMissing.set(false);
    forkJoin({
      visit: this.visits.get(visitId).pipe(catchError(() => of(null))),
      vitals: this.vitalsService.get(visitId).pipe(catchError(() => of(null))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ visit, vitals }) => {
        this.loading.set(false);
        this.visitMissing.set(visit === null);
        this.visit.set(visit);
        this.recorded.set(vitals);
      });
  }

  /** Re-reads only the visit, so the last-modified time follows a save without reloading the vitals. */
  private refreshVisit(): void {
    this.visits
      .get(Number(this.visitId()))
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((visit) => {
        if (visit !== null) {
          this.visit.set(visit);
        }
      });
  }

  private recordErrorMessage(error: unknown): string {
    if (error instanceof ApiRequestError && error.code === VITALS_IMPLAUSIBLE_CODE) {
      return this.translations.translate('consultation.vitals.implausible');
    }
    return error instanceof ApiRequestError
      ? error.message
      : this.translations.translate('errors.unknown');
  }

  private toRequest(): RecordVitalsRequest {
    const raw = this.form.getRawValue();
    return {
      temperature: raw.temperature,
      bloodPressureSystolic: raw.bloodPressureSystolic,
      bloodPressureDiastolic: raw.bloodPressureDiastolic,
      pulse: raw.pulse,
    };
  }
}
