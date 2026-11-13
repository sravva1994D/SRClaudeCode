/** One visit in a patient's history. The times are UTC (ISO 8601 with a Z suffix). The status id is a VISIT_STATUS_* value. */
export interface PatientVisit {
  visitId: number;
  startedAt: string;
  statusId: number;
  modifiedDate: string;
  appointmentId: number | null;
}

/** The date filter on a patient's visit history. Each side is a calendar day (yyyy-MM-dd), or empty when open. */
export interface VisitDateRange {
  from: string;
  to: string;
}
