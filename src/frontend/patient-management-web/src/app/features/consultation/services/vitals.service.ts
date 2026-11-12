import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import {
  RecordVitalsRequest,
  RecordVitalsResponse,
  Vitals,
} from '../../../core/models/vitals.model';

/** Records and reads a visit's vitals. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class VitalsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/visits`;

  /**
   * Reads the vitals for a visit. A visit with no vitals yet answers 404, which the page treats as "not recorded",
   * so the failure is not shown as an error.
   */
  get(visitId: number): Observable<Vitals> {
    return this.http.get<Vitals>(`${this.baseUrl}/${visitId}/vitals`, { context: silently() });
  }

  /** Saves the vitals. A failure is shown inline by the page (for example, a blocked value), not as a snackbar. */
  record(visitId: number, request: RecordVitalsRequest): Observable<RecordVitalsResponse> {
    return this.http.post<RecordVitalsResponse>(`${this.baseUrl}/${visitId}/vitals`, request, {
      context: silently(),
    });
  }
}

function silently(): HttpContext {
  return new HttpContext().set(SUPPRESS_ERROR_NOTIFICATION, true);
}
