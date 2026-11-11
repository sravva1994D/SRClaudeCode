import { describe, expect, it } from 'vitest';

import {
  CALENDAR_FIRST_HOUR,
  addDays,
  isPastSlot,
  slotKey,
  slotsForDay,
  startOfWeek,
  weekDays,
} from './calendar-slots';

describe('calendar slots', () => {
  it('lists 15-minute slots from the first hour to the last slot of the day', () => {
    const slots = slotsForDay(new Date(2026, 10, 11));

    expect(slots).toHaveLength(48);
    expect(slots[0].getHours()).toBe(CALENDAR_FIRST_HOUR);
    expect(slots[1].getMinutes()).toBe(15);
    expect(slots[47].getHours()).toBe(19);
    expect(slots[47].getMinutes()).toBe(45);
  });

  it('starts the week on Monday, even when the day is a Sunday', () => {
    const sunday = new Date(2026, 10, 15);

    expect(startOfWeek(sunday)).toEqual(new Date(2026, 10, 9));
    expect(weekDays(startOfWeek(sunday))).toHaveLength(7);
  });

  it('treats a slot that has started as past', () => {
    const now = new Date(2026, 10, 11, 10, 0);

    expect(isPastSlot(new Date(2026, 10, 11, 9, 45), now)).toBe(true);
    expect(isPastSlot(new Date(2026, 10, 11, 10, 0), now)).toBe(true);
    expect(isPastSlot(new Date(2026, 10, 11, 10, 15), now)).toBe(false);
  });

  it('matches a slot to the same local minute', () => {
    expect(slotKey(new Date(2026, 10, 11, 9, 15))).toBe(
      slotKey(new Date(2026, 10, 11, 9, 15, 0, 0)),
    );
    expect(slotKey(new Date(2026, 10, 11, 9, 15))).not.toBe(slotKey(new Date(2026, 10, 11, 9, 30)));
  });

  it('moves by whole days at local midnight', () => {
    expect(addDays(new Date(2026, 10, 30), 2)).toEqual(new Date(2026, 11, 2));
  });
});
