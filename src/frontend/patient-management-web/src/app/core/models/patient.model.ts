/** A patient as the API returns it. Dates are ISO calendar dates (yyyy-MM-dd). */
export interface Patient {
  patientId: number;
  fullName: string;
  dateOfBirth: string;
  genderId: number;
  phoneNumber: string | null;
  address: string | null;
}

/** Create and update body. The API stores the phone number as digits only. */
export interface PatientRequest {
  fullName: string;
  dateOfBirth: string;
  genderId: number;
  phoneNumber: string | null;
  address: string | null;
}

/** The few fields a duplicate warning shows. */
export interface PatientSummary {
  patientId: number;
  fullName: string;
  dateOfBirth: string;
}

/** A row in the patient list. */
export interface PatientListItem {
  patientId: number;
  fullName: string;
  dateOfBirth: string;
  genderId: number;
  phoneNumber: string | null;
}

/** One page of a list, in the shape every paged endpoint returns (CLAUDE.md 6.5). */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/** Gender choices. The ids match the seeded Genders table (migration V0002). */
export const PATIENT_GENDER_OPTIONS = [
  { id: 1, labelKey: 'patients.gender.male' },
  { id: 2, labelKey: 'patients.gender.female' },
  { id: 3, labelKey: 'patients.gender.other' },
] as const;
