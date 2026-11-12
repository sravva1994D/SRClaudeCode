import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PatientAppointment } from '../../../core/models/appointment.model';
import {
  PagedResponse,
  Patient,
  PatientListItem,
  PatientRequest,
  PatientSummary,
} from '../../../core/models/patient.model';

/** One page of the patient list. The search text matches names or phone digits (see the API). */
export interface PatientListQuery {
  search: string;
  page: number;
  pageSize: number;
}

/** Reads and saves patients. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class PatientsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/patients`;

  list(query: PatientListQuery): Observable<PagedResponse<PatientListItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search.trim() !== '') {
      params = params.set('search', query.search.trim());
    }
    return this.http.get<PagedResponse<PatientListItem>>(this.url, { params });
  }

  /** One page of the patient's appointments, newest first, with every status. */
  appointmentHistory(
    patientId: number,
    page: number,
    pageSize: number,
  ): Observable<PagedResponse<PatientAppointment>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<PatientAppointment>>(
      `${this.url}/${patientId}/appointments`,
      { params },
    );
  }

  /** The patients the signed-in user opened most recently, newest first. */
  recent(): Observable<PatientListItem[]> {
    return this.http.get<PatientListItem[]>(`${this.url}/recent`);
  }

  /** Records that the signed-in user opened this patient's profile. */
  markViewed(patientId: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${patientId}/viewed`, null);
  }

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
