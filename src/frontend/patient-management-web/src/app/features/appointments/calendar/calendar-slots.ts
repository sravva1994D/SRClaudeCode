/** Length of one appointment slot, in minutes (plan Sprint 5–6, item 25). Must match the API's slot length. */
export const SLOT_MINUTES = 15;

/** First and last hour shown on the calendar, in local time. The clinic's hours are not in the plan yet; this is a default. */
export const CALENDAR_FIRST_HOUR = 8;
export const CALENDAR_LAST_HOUR = 20;

/** Every slot start for one local calendar day, from the first hour up to the last slot before the last hour. */
export function slotsForDay(day: Date): Date[] {
  const slots: Date[] = [];
  const count = ((CALENDAR_LAST_HOUR - CALENDAR_FIRST_HOUR) * 60) / SLOT_MINUTES;
  for (let index = 0; index < count; index += 1) {
    const minutes = CALENDAR_FIRST_HOUR * 60 + index * SLOT_MINUTES;
    slots.push(
      new Date(
        day.getFullYear(),
        day.getMonth(),
        day.getDate(),
        Math.floor(minutes / 60),
        minutes % 60,
      ),
    );
  }
  return slots;
}

/** Monday of the week that contains the day, at local midnight. */
export function startOfWeek(day: Date): Date {
  const offsetToMonday = (day.getDay() + 6) % 7;
  return new Date(day.getFullYear(), day.getMonth(), day.getDate() - offsetToMonday);
}

/** The seven days of the week that starts on the given Monday. */
export function weekDays(monday: Date): Date[] {
  return Array.from(
    { length: 7 },
    (_, index) => new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + index),
  );
}

/** Adds whole days at local midnight. */
export function addDays(day: Date, days: number): Date {
  return new Date(day.getFullYear(), day.getMonth(), day.getDate() + days);
}

/** A slot can only be booked when it is still in the future. */
export function isPastSlot(slot: Date, now: Date): boolean {
  return slot.getTime() <= now.getTime();
}

/** Key used to match a slot with an appointment: the local start time, to the minute. */
export function slotKey(slot: Date): string {
  return `${slot.getFullYear()}-${pad(slot.getMonth() + 1)}-${pad(slot.getDate())}T${pad(slot.getHours())}:${pad(slot.getMinutes())}`;
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}
