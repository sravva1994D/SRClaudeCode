/** An issued prescription as the API returns it. The prescription date is an ISO calendar date (yyyy-MM-dd). */
export interface Prescription {
  prescriptionId: number;
  visitId: number;
  prescriptionDate: string;
}

/** The clinic header printed on a prescription. */
export interface PrescriptionClinicHeader {
  clinicName: string | null;
  doctorName: string | null;
  qualifications: string | null;
  registrationNumber: string | null;
  address: string | null;
  phoneNumber: string | null;
  footerNotes: string | null;
}

export interface PrescriptionPatient {
  fullName: string;
  dateOfBirth: string;
  phoneNumber: string | null;
}

/** Vitals printed on a prescription. Each value is null when it was not measured. */
export interface PrescriptionVitals {
  temperature: number | null;
  bloodPressureSystolic: number | null;
  bloodPressureDiastolic: number | null;
  pulse: number | null;
}

export interface PrescriptionDiagnosis {
  text: string;
  code: string | null;
}

export interface PrescriptionMedication {
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  specialInstructions: string | null;
}

/** The content of a prescription: built from the visit when viewed, or the stored snapshot when reprinted. */
export interface PrescriptionDocument {
  prescription: Prescription;
  clinic: PrescriptionClinicHeader;
  patient: PrescriptionPatient;
  vitals: PrescriptionVitals | null;
  diagnoses: PrescriptionDiagnosis[];
  medications: PrescriptionMedication[];
}

/** One print of a prescription, with the content that was printed. The time is UTC (ISO 8601 with a Z suffix). */
export interface PrescriptionPrint {
  prescriptionPrintId: number;
  printedAt: string;
  document: PrescriptionDocument;
}

/** One print in the reprint list: when it was printed, without the content. */
export interface PrescriptionPrintSummary {
  prescriptionPrintId: number;
  printedAt: string;
}
