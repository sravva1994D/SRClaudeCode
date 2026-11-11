/** One appointment as the calendar shows it. The slot start is an ISO UTC timestamp; the screen shows local time. */
export interface AppointmentListItem {
  appointmentId: number;
  patientId: number;
  patientFullName: string;
  slotStartUtc: string;
  statusId: number;
}

/** The appointment as the booking endpoint returns it. It has no patient name. */
export interface AppointmentDetails {
  appointmentId: number;
  patientId: number;
  slotStartUtc: string;
  statusId: number;
}

/** Booking body. The slot start is sent as an ISO UTC timestamp. */
export interface ScheduleAppointmentRequest {
  patientId: number;
  slotStart: string;
}

/** Appointment status ids, matching the seeded AppointmentStatuses table (migration V0002). */
export const APPOINTMENT_STATUS_SCHEDULED = 1;
