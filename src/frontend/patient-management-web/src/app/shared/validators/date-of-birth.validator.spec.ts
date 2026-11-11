import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { dateOfBirthValidator } from './date-of-birth.validator';

describe('dateOfBirthValidator', () => {
  const today = () => new Date(2026, 10, 11);
  const validate = (value: string) => dateOfBirthValidator(today)(new FormControl(value));

  it('accepts a date in the past', () => {
    expect(validate('1990-05-14')).toBeNull();
  });

  it('accepts today', () => {
    expect(validate('2026-11-11')).toBeNull();
  });

  it('rejects a date after today', () => {
    expect(validate('2026-11-12')).toEqual({ dateOfBirthInFuture: true });
  });

  it('rejects a date before 1900', () => {
    expect(validate('1899-12-31')).toEqual({ dateOfBirthTooEarly: true });
  });

  it('rejects text that is not yyyy-MM-dd', () => {
    expect(validate('14/05/1990')).toEqual({ dateOfBirthFormat: true });
  });

  it('leaves an empty value to the required rule', () => {
    expect(validate('')).toBeNull();
  });
});
