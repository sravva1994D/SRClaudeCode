import { FormControl, FormGroup } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { atLeastOneVitalValidator, vitalValueValidator } from './vital-value.validator';

const WHOLE = vitalValueValidator({ wholeNumber: true, maximum: 999 });
const TEMPERATURE = vitalValueValidator({ wholeNumber: false, maximum: 999.9 });

describe('vitalValueValidator', () => {
  it('accepts an empty value, because a vital may be left unmeasured', () => {
    expect(WHOLE(new FormControl<number | null>(null))).toBeNull();
  });

  it('accepts a whole number for pulse', () => {
    expect(WHOLE(new FormControl(72))).toBeNull();
  });

  it('rejects a decimal for pulse', () => {
    expect(WHOLE(new FormControl(72.5))).toEqual({ vitalDecimalPlaces: true });
  });

  it('accepts one decimal place for temperature, even where binary floating point is inexact', () => {
    expect(TEMPERATURE(new FormControl(36.6))).toBeNull();
  });

  it('rejects two decimal places for temperature', () => {
    expect(TEMPERATURE(new FormControl(36.66))).toEqual({ vitalDecimalPlaces: true });
  });

  it('rejects a negative value', () => {
    expect(WHOLE(new FormControl(-1))).toEqual({ vitalOutOfEntryRange: true });
  });

  it('rejects a value above the column limit', () => {
    expect(WHOLE(new FormControl(1000))).toEqual({ vitalOutOfEntryRange: true });
  });

  it('does not judge clinical plausibility, which the server decides', () => {
    expect(WHOLE(new FormControl(10))).toBeNull();
  });
});

describe('atLeastOneVitalValidator', () => {
  it('rejects a group with no measured value', () => {
    const group = new FormGroup({
      pulse: new FormControl<number | null>(null),
      temperature: new FormControl<number | null>(null),
    });

    expect(atLeastOneVitalValidator(group)).toEqual({ noVitals: true });
  });

  it('accepts a group with one measured value', () => {
    const group = new FormGroup({
      pulse: new FormControl<number | null>(72),
      temperature: new FormControl<number | null>(null),
    });

    expect(atLeastOneVitalValidator(group)).toBeNull();
  });
});
