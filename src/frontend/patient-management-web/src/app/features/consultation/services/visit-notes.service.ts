import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import { DiagnosisEntry, VisitNotes } from '../../../core/models/visit-notes.model';

/** Reads and saves a visit's complaints and diagnoses. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class VisitNotesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/visits`;

  get(visitId: number): Observable<VisitNotes> {
    return this.http.get<VisitNotes>(`${this.baseUrl}/${visitId}/notes`);
  }

  /**
   * Saves the complaints as a draft: the list replaces the saved complaints. A failure is shown inline by the page,
   * so the error snackbar is suppressed.
   */
  saveComplaints(visitId: number, complaints: string[]): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/${visitId}/complaints`,
      { complaints },
      { context: silently() },
    );
  }

  /** Saves the diagnoses as a draft: the list replaces the saved diagnoses. Errors are shown inline, as above. */
  saveDiagnoses(visitId: number, diagnoses: DiagnosisEntry[]): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/${visitId}/diagnoses`,
      { diagnoses },
      { context: silently() },
    );
  }
}

function silently(): HttpContext {
  return new HttpContext().set(SUPPRESS_ERROR_NOTIFICATION, true);
}
