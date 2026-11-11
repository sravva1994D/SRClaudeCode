import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { normalizePhoneNumber, phoneNumberValidator } from './phone-number.validator';

describe('phoneNumberValidator', () => {
  const validate = (value: string) => phoneNumberValidator()(new FormControl(value));

  it('accepts digits with spaces, plus, dashes and brackets', () => {
    expect(validate('+91 (98) 765-43210')).toBeNull();
  });

  it('accepts an empty value, because the phone number is optional', () => {
    expect(validate('')).toBeNull();
  });

  it('rejects letters', () => {
    expect(validate('98765 call')).toEqual({ phoneNumber: true });
  });

  it('rejects more than 20 characters', () => {
    expect(validate('1'.repeat(21))).toEqual({ maxlength: { requiredLength: 20 } });
  });
});

describe('normalizePhoneNumber', () => {
  it('keeps only the digits', () => {
    expect(normalizePhoneNumber('+91 (98) 765-43210')).toBe('919876543210');
  });

  it('returns null for empty or blank input', () => {
    expect(normalizePhoneNumber('')).toBeNull();
    expect(normalizePhoneNumber('  ')).toBeNull();
    expect(normalizePhoneNumber(null)).toBeNull();
  });
});
