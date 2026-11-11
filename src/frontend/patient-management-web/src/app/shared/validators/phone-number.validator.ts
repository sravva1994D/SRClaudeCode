import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const MAXIMUM_PHONE_LENGTH = 20;

const ALLOWED_PHONE_CHARACTERS = /^[0-9 +()-]*$/;

/** Accepts digits, spaces, +, - and brackets, up to 20 characters. Mirrors the server rule (CLAUDE.md 6.6). */
export function phoneNumberValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value.length > MAXIMUM_PHONE_LENGTH) {
      return { maxlength: { requiredLength: MAXIMUM_PHONE_LENGTH } };
    }
    return ALLOWED_PHONE_CHARACTERS.test(value) ? null : { phoneNumber: true };
  };
}

/** Keeps only the digits. The API does the same, so the stored value is identical either way. */
export function normalizePhoneNumber(value: string | null): string | null {
  const digits = (value ?? '').replace(/\D/g, '');
  return digits.length > 0 ? digits : null;
}
