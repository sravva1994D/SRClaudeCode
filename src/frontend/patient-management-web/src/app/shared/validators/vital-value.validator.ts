import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Tolerance for floating-point error when checking decimal places (36.6 * 10 is not exactly 366 in binary). */
const DECIMAL_TOLERANCE = 1e-9;

export interface VitalValueRule {
  /** Blood pressure and pulse are whole numbers; temperature has one decimal place. */
  readonly wholeNumber: boolean;
  /** The largest value the column can hold. Plausible-range checks are left to the server. */
  readonly maximum: number;
}

/**
 * Format check for one vital value: a number between 0 and the column limit, with the right number of decimal
 * places. It does not judge clinical plausibility; the server does that against the ranges the doctor confirms.
 */
export function vitalValueValidator(rule: VitalValueRule): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === '') {
      return null;
    }
    if (typeof value !== 'number' || !Number.isFinite(value)) {
      return { vitalNotANumber: true };
    }
    if (value < 0 || value > rule.maximum) {
      return { vitalOutOfEntryRange: true };
    }

    const scaled = rule.wholeNumber ? value : value * 10;
    return Math.abs(scaled - Math.round(scaled)) > DECIMAL_TOLERANCE
      ? { vitalDecimalPlaces: true }
      : null;
  };
}

/** A vitals entry must include at least one measured value. */
export const atLeastOneVitalValidator: ValidatorFn = (
  group: AbstractControl,
): ValidationErrors | null => {
  const value: unknown = group.value;
  if (typeof value !== 'object' || value === null) {
    return { noVitals: true };
  }

  const measured = Object.values(value).some(
    (entry) => entry !== null && entry !== undefined && entry !== '',
  );
  return measured ? null : { noVitals: true };
};
