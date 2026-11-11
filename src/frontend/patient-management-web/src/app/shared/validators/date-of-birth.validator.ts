import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const EARLIEST_DATE_OF_BIRTH = '1900-01-01';

/**
 * Accepts a calendar date (yyyy-MM-dd) that is not in the future and not before 1900.
 * The date is compared as text, which is correct for this format, so time zones do not shift the result.
 * The clock is injected so the rule can be tested without the real date.
 */
export function dateOfBirthValidator(today: () => Date = () => new Date()): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value === '') {
      return null;
    }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      return { dateOfBirthFormat: true };
    }
    if (value < EARLIEST_DATE_OF_BIRTH) {
      return { dateOfBirthTooEarly: true };
    }
    return value > toIsoDate(today()) ? { dateOfBirthInFuture: true } : null;
  };
}

/** Formats a date in the local calendar as yyyy-MM-dd, the format the API and date inputs use. */
export function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
