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
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';

import { Patient, PATIENT_GENDER_OPTIONS } from '../../../core/models/patient.model';
import { AgePipe } from '../../../shared/pipes/age.pipe';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PatientsService } from '../services/patients.service';

/**
 * Shows one patient's details and records the visit, so the patient appears in the user's recent list.
 * Visit history is added in Phase 3 and 4; until then the page shows a placeholder for it.
 */
@Component({
  selector: 'app-patient-profile-page',
  imports: [DatePipe, RouterLink, MatButtonModule, MatProgressBarModule, AgePipe, TranslatePipe],
  templateUrl: './patient-profile-page.component.html',
  styleUrl: './patient-profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientProfilePageComponent implements OnInit {
  // Bound from the route (withComponentInputBinding).
  readonly patientId = input.required<string>();

  private readonly patients = inject(PatientsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly patient = signal<Patient | null>(null);

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

    // Best effort: a failed record of the visit must not hide the patient's details.
    this.patients
      .markViewed(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  genderLabelKey(genderId: number): string {
    return PATIENT_GENDER_OPTIONS.find((option) => option.id === genderId)?.labelKey ?? '';
  }
}
