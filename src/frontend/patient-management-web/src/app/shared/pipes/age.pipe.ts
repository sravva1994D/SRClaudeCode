import { Pipe, PipeTransform } from '@angular/core';

/**
 * Whole years between a date of birth (yyyy-MM-dd) and a day. Returns null for an empty or invalid date.
 * A birthday counts on the day itself, so the age goes up on that day, not the day after.
 */
export function calculateAge(dateOfBirth: string | null, today: Date): number | null {
  if (!dateOfBirth || !/^\d{4}-\d{2}-\d{2}$/.test(dateOfBirth)) {
    return null;
  }
  const [year, month, day] = dateOfBirth.split('-').map(Number);
  let age = today.getFullYear() - year;
  const hasHadBirthdayThisYear =
    today.getMonth() + 1 > month || (today.getMonth() + 1 === month && today.getDate() >= day);
  if (!hasHadBirthdayThisYear) {
    age -= 1;
  }
  return age < 0 ? null : age;
}

/** Shows the age in years for a date of birth. Usage: {{ form.value.dateOfBirth | age }} */
@Pipe({ name: 'age' })
export class AgePipe implements PipeTransform {
  transform(dateOfBirth: string | null): number | null {
    return calculateAge(dateOfBirth, new Date());
  }
}
