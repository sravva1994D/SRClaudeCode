import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { PagedResponse } from '../../../core/models/patient.model';
import { PatientVisit, VisitDateRange } from '../../../core/models/patient-visit.model';

/** Reads a patient's visit history, one page at a time, optionally filtered by a date range. */
@Injectable({ providedIn: 'root' })
export class PatientVisitsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/patients`;

  list(
    patientId: number,
    range: VisitDateRange,
    page: number,
    pageSize: number,
  ): Observable<PagedResponse<PatientVisit>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (range.from) {
      params = params.set('from', range.from);
    }
    if (range.to) {
      params = params.set('to', range.to);
    }
    return this.http.get<PagedResponse<PatientVisit>>(`${this.url}/${patientId}/visits`, {
      params,
    });
  }
}
