import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { Patient, PatientRequest, PatientSummary } from '../../../core/models/patient.model';

/** Reads and saves patients. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class PatientsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/patients`;

  get(patientId: number): Observable<Patient> {
    return this.http.get<Patient>(`${this.url}/${patientId}`);
  }

  create(request: PatientRequest): Observable<Patient> {
    return this.http.post<Patient>(this.url, request);
  }

  update(patientId: number, request: PatientRequest): Observable<Patient> {
    return this.http.put<Patient>(`${this.url}/${patientId}`, request);
  }

  /** Active patients with the same name and date of birth. The patient being edited is left out. */
  findDuplicates(
    fullName: string,
    dateOfBirth: string,
    excludePatientId: number | null,
  ): Observable<PatientSummary[]> {
    let params = new HttpParams().set('fullName', fullName).set('dateOfBirth', dateOfBirth);
    if (excludePatientId !== null) {
      params = params.set('excludePatientId', excludePatientId);
    }
    return this.http.get<PatientSummary[]>(`${this.url}/duplicates`, { params });
  }
}
