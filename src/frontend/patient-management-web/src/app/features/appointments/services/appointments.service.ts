import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  AppointmentDetails,
  AppointmentListItem,
  ScheduleAppointmentRequest,
} from '../../../core/models/appointment.model';
import { Visit } from '../../../core/models/visit.model';

/** Reads and books appointments. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class AppointmentsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/appointments`;

  /** Appointments whose slot starts in [from, to). Local times are converted to UTC for the request. */
  list(from: Date, to: Date): Observable<AppointmentListItem[]> {
    const params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
    return this.http.get<AppointmentListItem[]>(this.url, { params });
  }

  /** Books the slot for the patient. The slot is a local Date; it is sent as UTC. */
  schedule(patientId: number, slotStart: Date): Observable<AppointmentDetails> {
    const body: ScheduleAppointmentRequest = {
      patientId,
      slotStart: slotStart.toISOString(),
    };
    return this.http.post<AppointmentDetails>(this.url, body);
  }

  /** Starts the visit for a scheduled appointment. The API answers 201 with the new visit. */
  startVisit(appointmentId: number): Observable<Visit> {
    return this.http.post<Visit>(`${this.url}/${appointmentId}/start-visit`, null);
  }
}
