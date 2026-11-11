import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

type TranslationDictionary = Readonly<Record<string, string>>;

/**
 * Serves user-visible text from assets/i18n/en.json (CLAUDE.md 8.8). A missing key returns the key itself,
 * so a gap shows up on screen instead of failing silently.
 */
@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly http = inject(HttpClient);
  private readonly dictionary = signal<TranslationDictionary>({});

  /** Loads the English dictionary once at startup. A failed load leaves keys visible rather than blocking the app. */
  load(): Observable<void> {
    return this.http.get<TranslationDictionary>('assets/i18n/en.json').pipe(
      map((loaded) => this.dictionary.set(loaded)),
      catchError(() => of(undefined)),
    );
  }

  translate(key: string): string {
    return this.dictionary()[key] ?? key;
  }
}
