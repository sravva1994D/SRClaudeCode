/** The clinic profile as the API returns it, and as the settings form sends it. */
export interface ClinicProfile {
  clinicName: string | null;
  doctorName: string | null;
  qualifications: string | null;
  registrationNumber: string | null;
  address: string | null;
  phoneNumber: string | null;
  footerNotes: string | null;
}
