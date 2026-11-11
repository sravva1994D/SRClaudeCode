import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ClinicProfile } from '../../../core/models/clinic-profile.model';

/** Reads and updates the clinic profile. Errors reach callers already normalized by the HTTP interceptors. */
@Injectable({ providedIn: 'root' })
export class ClinicProfileService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/clinic-profile`;

  get(): Observable<ClinicProfile> {
    return this.http.get<ClinicProfile>(this.url);
  }

  update(profile: ClinicProfile): Observable<ClinicProfile> {
    return this.http.put<ClinicProfile>(this.url, profile);
  }
}
