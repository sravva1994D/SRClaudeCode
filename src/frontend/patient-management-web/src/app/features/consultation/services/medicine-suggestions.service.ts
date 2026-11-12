import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

/** Medicine names from the doctor's own history, for the medication autocomplete. */
@Injectable({ providedIn: 'root' })
export class MedicineSuggestionsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/medicines/suggestions`;

  /** Names that start with the search text. The API returns at most ten, in alphabetical order. */
  suggest(search: string): Observable<string[]> {
    const params = new HttpParams().set('search', search).set('limit', 10);
    return this.http.get<string[]>(this.url, { params });
  }
}
