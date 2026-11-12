/** A visit as the API returns it. The status id is one of the visit status constants below. */
export interface Visit {
  visitId: number;
  patientId: number;
  appointmentId: number | null;
  statusId: number;
}

/** Visit status ids, matching the seeded VisitStatuses table (migration V0014). */
export const VISIT_STATUS_IN_PROGRESS = 1;
export const VISIT_STATUS_COMPLETED = 2;
