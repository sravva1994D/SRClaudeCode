/** Recorded vitals as the API returns them. Every value is null when it was not measured. */
export interface Vitals {
  vitalsId: number;
  visitId: number;
  temperature: number | null;
  bloodPressureSystolic: number | null;
  bloodPressureDiastolic: number | null;
  pulse: number | null;
}

/** Recording body. Only the values measured are sent; the others are null. */
export interface RecordVitalsRequest {
  temperature: number | null;
  bloodPressureSystolic: number | null;
  bloodPressureDiastolic: number | null;
  pulse: number | null;
}

/** The saved vitals and the names of the signs outside their normal range (saved anyway). */
export interface RecordVitalsResponse {
  vitals: Vitals;
  warnings: string[];
}

/** Error code the API returns when a value is outside its plausible range; nothing is saved. */
export const VITALS_IMPLAUSIBLE_CODE = 'vitals_implausible';
