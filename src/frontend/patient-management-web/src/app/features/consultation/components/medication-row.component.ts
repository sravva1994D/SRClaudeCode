import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  Observable,
  of,
  switchMap,
} from 'rxjs';

import {
  MEDICATION_FIELD_MAXIMUM_LENGTH,
  MEDICATION_INSTRUCTIONS_MAXIMUM_LENGTH,
  MEDICINE_NAME_MAXIMUM_LENGTH,
} from '../../../core/models/visit-notes.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { MedicineSuggestionsService } from '../services/medicine-suggestions.service';

const SUGGESTION_DEBOUNCE_MS = 300;

/** The controls of one medication row. The form is typed; the page builds it. */
export interface MedicationControls {
  medicineName: FormControl<string>;
  dosage: FormControl<string>;
  frequency: FormControl<string>;
  duration: FormControl<string>;
  specialInstructions: FormControl<string>;
}

/**
 * One medicine on the consultation form. The medicine name offers suggestions from the doctor's own history after the
 * user pauses typing; the other fields are plain text.
 */
@Component({
  selector: 'app-medication-row',
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    TranslatePipe,
  ],
  templateUrl: './medication-row.component.html',
  styleUrl: './medication-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicationRowComponent implements OnInit {
  readonly group = input.required<FormGroup<MedicationControls>>();
  readonly removeRequested = output<void>();

  private readonly suggestionsService = inject(MedicineSuggestionsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly suggestions = signal<string[]>([]);
  readonly maximumNameLength = MEDICINE_NAME_MAXIMUM_LENGTH;
  readonly maximumFieldLength = MEDICATION_FIELD_MAXIMUM_LENGTH;
  readonly maximumInstructionsLength = MEDICATION_INSTRUCTIONS_MAXIMUM_LENGTH;

  ngOnInit(): void {
    this.group()
      .controls.medicineName.valueChanges.pipe(
        debounceTime(SUGGESTION_DEBOUNCE_MS),
        map((value) => value.trim()),
        distinctUntilChanged(),
        switchMap((term) => this.lookUp(term)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((names) => this.suggestions.set(names));
  }

  private lookUp(term: string): Observable<string[]> {
    if (term === '') {
      return of([]);
    }
    return this.suggestionsService.suggest(term).pipe(catchError(() => of([] as string[])));
  }
}
