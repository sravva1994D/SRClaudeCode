import { describe, expect, it } from 'vitest';

import { calculateAge } from './age.pipe';

describe('calculateAge', () => {
  it('returns the years completed before the day', () => {
    expect(calculateAge('1990-05-14', new Date(2026, 10, 11))).toBe(36);
  });

  it('counts the birthday on the day itself', () => {
    expect(calculateAge('1990-11-11', new Date(2026, 10, 11))).toBe(36);
  });

  it('does not count the birthday the day before it', () => {
    expect(calculateAge('1990-11-12', new Date(2026, 10, 11))).toBe(35);
  });

  it('returns null for an empty date', () => {
    expect(calculateAge('', new Date(2026, 10, 11))).toBeNull();
    expect(calculateAge(null, new Date(2026, 10, 11))).toBeNull();
  });

  it('returns null for a date that is not yyyy-MM-dd', () => {
    expect(calculateAge('14/05/1990', new Date(2026, 10, 11))).toBeNull();
  });

  it('returns null for a date of birth after the day', () => {
    expect(calculateAge('2026-12-01', new Date(2026, 10, 11))).toBeNull();
  });
});
