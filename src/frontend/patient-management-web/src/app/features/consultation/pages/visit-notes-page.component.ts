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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';

import { TranslationService } from '../../../core/i18n/translation.service';
import { ApiRequestError } from '../../../core/http/api-request-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  DIAGNOSIS_CODE_MAXIMUM_LENGTH,
  DiagnosisEntry,
  MedicationEntry,
  NOTE_ITEMS_MAXIMUM,
  NOTE_TEXT_MAXIMUM_LENGTH,
  VisitNotes,
} from '../../../core/models/visit-notes.model';
import { Visit, VISIT_STATUS_IN_PROGRESS } from '../../../core/models/visit.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MedicationControls, MedicationRowComponent } from '../components/medication-row.component';
import { VisitsService } from '../services/visits.service';
import { VisitNotesService } from '../services/visit-notes.service';

interface DiagnosisControls {
  text: FormControl<string>;
  code: FormControl<string>;
}

/**
 * Complaints, diagnosis and medications for a visit, saved as a draft. Each save replaces the visit's saved lists.
 * Blank complaint and diagnosis entries are dropped before saving; a medicine must have its name, dosage, frequency
 * and duration. The server enforces the same limits and the in-progress rule; the form mirrors them for feedback.
 */
@Component({
  selector: 'app-visit-notes-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MedicationRowComponent,
    TranslatePipe,
  ],
  templateUrl: './visit-notes-page.component.html',
  styleUrl: './visit-notes-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitNotesPageComponent implements OnInit {
  // Bound from the route (withComponentInputBinding).
  readonly visitId = input.required<string>();

  private readonly visits = inject(VisitsService);
  private readonly notesService = inject(VisitNotesService);
  private readonly notifications = inject(NotificationService);
  private readonly translations = inject(TranslationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly maximumTextLength = NOTE_TEXT_MAXIMUM_LENGTH;
  readonly maximumCodeLength = DIAGNOSIS_CODE_MAXIMUM_LENGTH;
  readonly maximumItems = NOTE_ITEMS_MAXIMUM;

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly visit = signal<Visit | null>(null);
  readonly savedNotes = signal<VisitNotes | null>(null);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly notInProgress = computed(
    () => this.visit() !== null && this.visit()?.statusId !== VISIT_STATUS_IN_PROGRESS,
  );

  readonly complaints = new FormArray<FormControl<string>>([]);
  readonly diagnoses = new FormArray<FormGroup<DiagnosisControls>>([]);
  readonly medications = new FormArray<FormGroup<MedicationControls>>([]);

  ngOnInit(): void {
    this.load();
  }

  get complaintsAtLimit(): boolean {
    return this.complaints.length >= this.maximumItems;
  }

  get diagnosesAtLimit(): boolean {
    return this.diagnoses.length >= this.maximumItems;
  }

  get medicationsAtLimit(): boolean {
    return this.medications.length >= this.maximumItems;
  }

  addComplaint(text = ''): void {
    this.complaints.push(
      new FormControl(text, {
        nonNullable: true,
        validators: [Validators.maxLength(NOTE_TEXT_MAXIMUM_LENGTH)],
      }),
    );
  }

  removeComplaint(index: number): void {
    this.complaints.removeAt(index);
  }

  addDiagnosis(text = '', code = ''): void {
    this.diagnoses.push(
      new FormGroup<DiagnosisControls>({
        text: new FormControl(text, {
          nonNullable: true,
          validators: [Validators.maxLength(NOTE_TEXT_MAXIMUM_LENGTH)],
        }),
        code: new FormControl(code, {
          nonNullable: true,
          validators: [Validators.maxLength(DIAGNOSIS_CODE_MAXIMUM_LENGTH)],
        }),
      }),
    );
  }

  removeDiagnosis(index: number): void {
    this.diagnoses.removeAt(index);
  }

  addMedication(entry: MedicationEntry | null = null): void {
    this.medications.push(
      new FormGroup<MedicationControls>({
        medicineName: new FormControl(entry?.medicineName ?? '', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        dosage: new FormControl(entry?.dosage ?? '', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        frequency: new FormControl(entry?.frequency ?? '', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        duration: new FormControl(entry?.duration ?? '', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        specialInstructions: new FormControl(entry?.specialInstructions ?? '', {
          nonNullable: true,
        }),
      }),
    );
  }

  removeMedication(index: number): void {
    this.medications.removeAt(index);
  }

  save(): void {
    if (this.saving()) {
      return;
    }
    if (this.complaints.invalid || this.diagnoses.invalid || this.medications.invalid) {
      this.complaints.markAllAsTouched();
      this.diagnoses.markAllAsTouched();
      this.medications.markAllAsTouched();
      return;
    }

    const visitId = Number(this.visitId());
    this.saving.set(true);
    this.saveError.set(null);
    forkJoin([
      this.notesService.saveComplaints(visitId, this.complaintTexts()),
      this.notesService.saveDiagnoses(visitId, this.diagnosisEntries()),
      this.notesService.saveMedications(visitId, this.medicationEntries()),
    ])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.notifications.showSuccess('consultation.notes.saved');
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.saveError.set(
            error instanceof ApiRequestError
              ? error.message
              : this.translations.translate('errors.unknown'),
          );
        },
      });
  }

  private load(): void {
    const visitId = Number(this.visitId());
    this.loading.set(true);
    this.loadFailed.set(false);
    forkJoin({
      visit: this.visits.get(visitId).pipe(catchError(() => of(null))),
      notes: this.notesService.get(visitId).pipe(catchError(() => of(null))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ visit, notes }) => {
        this.loading.set(false);
        this.visit.set(visit);
        this.loadFailed.set(visit === null || notes === null);
        this.savedNotes.set(notes);
        if (notes !== null) {
          this.fillForm(notes);
        }
      });
  }

  private fillForm(notes: VisitNotes): void {
    this.complaints.clear();
    notes.complaints.forEach((text) => this.addComplaint(text));
    this.diagnoses.clear();
    notes.diagnoses.forEach((entry) => this.addDiagnosis(entry.text, entry.code ?? ''));
    this.medications.clear();
    notes.medications.forEach((entry) => this.addMedication(entry));
  }

  private complaintTexts(): string[] {
    return this.complaints.controls
      .map((control) => control.value.trim())
      .filter((text) => text.length > 0);
  }

  private diagnosisEntries(): DiagnosisEntry[] {
    return this.diagnoses.controls
      .map((group) => ({
        text: group.controls.text.value.trim(),
        code: group.controls.code.value.trim() || null,
      }))
      .filter((entry) => entry.text.length > 0);
  }

  private medicationEntries(): MedicationEntry[] {
    return this.medications.controls
      .map((group) => {
        const raw = group.getRawValue();
        return {
          medicineName: raw.medicineName.trim(),
          dosage: raw.dosage.trim(),
          frequency: raw.frequency.trim(),
          duration: raw.duration.trim(),
          specialInstructions: raw.specialInstructions.trim() || null,
        };
      })
      .filter((entry) => entry.medicineName.length > 0);
  }
}
