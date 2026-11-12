/** One diagnosis as the API returns it: the doctor's notes and an optional reference code. */
export interface DiagnosisEntry {
  text: string;
  code: string | null;
}

/** The complaints and diagnoses saved for a visit, in the order they were entered. */
export interface VisitNotes {
  visitId: number;
  complaints: string[];
  diagnoses: DiagnosisEntry[];
}

/** Limits the API enforces (complaint and diagnosis text, the diagnosis code, and the items per draft). */
export const NOTE_TEXT_MAXIMUM_LENGTH = 2000;
export const DIAGNOSIS_CODE_MAXIMUM_LENGTH = 20;
export const NOTE_ITEMS_MAXIMUM = 20;
