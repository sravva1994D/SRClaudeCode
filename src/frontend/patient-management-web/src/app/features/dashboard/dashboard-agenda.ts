import { AppointmentListItem } from '../../core/models/appointment.model';

/** Scheduled appointments are the only ones the upcoming list shows (status 1 in the seeded AppointmentStatuses). */
const SCHEDULED_STATUS_ID = 1;

/** How many days after today the upcoming list covers. */
export const UPCOMING_DAYS = 7;

export interface DashboardAgenda {
  /** Every appointment on today's local date, earliest first, whatever its status. */
  readonly today: AppointmentListItem[];
  /** Scheduled appointments from tomorrow to the end of the upcoming window, earliest first. */
  readonly upcoming: AppointmentListItem[];
}

/** The local day to fetch: today through the end of the upcoming window. */
export function agendaRange(now: Date): { from: Date; to: Date } {
  const from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const to = new Date(now.getFullYear(), now.getMonth(), now.getDate() + UPCOMING_DAYS + 1);
  return { from, to };
}

/** Splits the fetched appointments into today and upcoming, using the local calendar day. */
export function splitAgenda(items: readonly AppointmentListItem[], now: Date): DashboardAgenda {
  const todayKey = localDayKey(now);
  const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
  const windowEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate() + UPCOMING_DAYS + 1);

  const byStart = (left: AppointmentListItem, right: AppointmentListItem) =>
    new Date(left.slotStartUtc).getTime() - new Date(right.slotStartUtc).getTime();

  const today = items
    .filter((item) => localDayKey(new Date(item.slotStartUtc)) === todayKey)
    .sort(byStart);

  const upcoming = items
    .filter((item) => {
      const start = new Date(item.slotStartUtc);
      return item.statusId === SCHEDULED_STATUS_ID && start >= tomorrow && start < windowEnd;
    })
    .sort(byStart);

  return { today, upcoming };
}

/** The local calendar day as yyyy-MM-dd, so two dates compare by day regardless of time. */
export function localDayKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
