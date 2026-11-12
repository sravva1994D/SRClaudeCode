/**
 * A visit as the API returns it. The status id is one of the visit status constants below. The modified date is the
 * UTC time the visit or any of its records last changed (ISO 8601 with a Z suffix).
 */
export interface Visit {
  visitId: number;
  patientId: number;
  appointmentId: number | null;
  statusId: number;
  modifiedDate: string;
}

/** Visit status ids, matching the seeded VisitStatuses table (migration V0014). */
export const VISIT_STATUS_IN_PROGRESS = 1;
export const VISIT_STATUS_COMPLETED = 2;
