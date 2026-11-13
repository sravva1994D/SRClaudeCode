import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SUPPRESS_ERROR_NOTIFICATION } from '../../../core/http/silent-errors';
import {
  Prescription,
  PrescriptionDocument,
  PrescriptionPrint,
  PrescriptionPrintSummary,
} from '../../../core/models/prescription.model';

/**
 * Issues, reads and prints a visit's prescription. A visit with no prescription answers 404 and a visit that is not
 * completed answers 422; both are shown by the page, so the error snackbar is suppressed here.
 */
@Injectable({ providedIn: 'root' })
export class PrescriptionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/visits`;

  get(visitId: number): Observable<PrescriptionDocument> {
    return this.http.get<PrescriptionDocument>(`${this.baseUrl}/${visitId}/prescription`, {
      context: silently(),
    });
  }

  issue(visitId: number): Observable<Prescription> {
    return this.http.post<Prescription>(`${this.baseUrl}/${visitId}/prescription`, null, {
      context: silently(),
    });
  }

  /** Records a print with its content, and returns the content that was printed. */
  recordPrint(visitId: number): Observable<PrescriptionPrint> {
    return this.http.post<PrescriptionPrint>(
      `${this.baseUrl}/${visitId}/prescription/prints`,
      null,
      {
        context: silently(),
      },
    );
  }

  listPrints(visitId: number): Observable<PrescriptionPrintSummary[]> {
    return this.http.get<PrescriptionPrintSummary[]>(
      `${this.baseUrl}/${visitId}/prescription/prints`,
      {
        context: silently(),
      },
    );
  }

  /** Reads one earlier print from its snapshot, for a reprint. */
  getPrint(visitId: number, prescriptionPrintId: number): Observable<PrescriptionPrint> {
    return this.http.get<PrescriptionPrint>(
      `${this.baseUrl}/${visitId}/prescription/prints/${prescriptionPrintId}`,
      { context: silently() },
    );
  }
}

function silently(): HttpContext {
  return new HttpContext().set(SUPPRESS_ERROR_NOTIFICATION, true);
}
