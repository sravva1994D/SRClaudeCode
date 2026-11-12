import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import { Visit } from '../../../core/models/visit.model';

/** Reads and completes visits (consultations). Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class VisitsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/visits`;

  get(visitId: number): Observable<Visit> {
    return this.http.get<Visit>(`${this.url}/${visitId}`);
  }

  /**
   * Completes the visit. The page shows a refusal (no vitals, or already completed) inline, so the error snackbar is
   * suppressed.
   */
  complete(visitId: number): Observable<Visit> {
    const context = new HttpContext().set(SUPPRESS_ERROR_NOTIFICATION, true);
    return this.http.post<Visit>(`${this.url}/${visitId}/complete`, null, { context });
  }
}
