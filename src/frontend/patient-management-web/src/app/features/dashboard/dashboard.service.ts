import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppointmentListItem } from '../../core/models/appointment.model';

/**
 * Reads the appointments the dashboard shows. It makes the same request as the appointments calendar, rather than
 * importing that feature's service, because a feature must not import another feature's internals (CLAUDE.md 3.3).
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/appointments`;

  /** Appointments whose slot starts in [from, to). Local times are converted to UTC for the request. */
  appointments(from: Date, to: Date): Observable<AppointmentListItem[]> {
    const params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
    return this.http.get<AppointmentListItem[]>(this.url, { params });
  }
}
