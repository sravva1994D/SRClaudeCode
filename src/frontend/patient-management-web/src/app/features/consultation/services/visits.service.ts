import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { Visit } from '../../../core/models/visit.model';

/** Reads visits (consultations). Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class VisitsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/visits`;

  get(visitId: number): Observable<Visit> {
    return this.http.get<Visit>(`${this.url}/${visitId}`);
  }
}
