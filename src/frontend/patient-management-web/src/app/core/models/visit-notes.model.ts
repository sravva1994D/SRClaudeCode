/** One diagnosis as the API returns it: the doctor's notes and an optional reference code. */
export interface DiagnosisEntry {
  text: string;
  code: string | null;
}

/** One medicine: the name, dosage, frequency, duration, and an optional instruction such as "After food". */
export interface MedicationEntry {
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  specialInstructions: string | null;
}

/** The complaints, diagnoses and medications saved for a visit, in the order they were entered. */
export interface VisitNotes {
  visitId: number;
  complaints: string[];
  diagnoses: DiagnosisEntry[];
  medications: MedicationEntry[];
}

/** Limits the API enforces. */
export const NOTE_TEXT_MAXIMUM_LENGTH = 2000;
export const DIAGNOSIS_CODE_MAXIMUM_LENGTH = 20;
export const MEDICINE_NAME_MAXIMUM_LENGTH = 200;
export const MEDICATION_FIELD_MAXIMUM_LENGTH = 100;
export const MEDICATION_INSTRUCTIONS_MAXIMUM_LENGTH = 500;
export const NOTE_ITEMS_MAXIMUM = 20;
