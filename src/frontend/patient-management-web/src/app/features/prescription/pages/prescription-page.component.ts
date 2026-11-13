import { DatePipe } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { catchError, of, throwError } from 'rxjs';

import { ApiRequestError } from '../../../core/http/api-request-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import {
  PrescriptionDocument,
  PrescriptionPrint,
  PrescriptionPrintSummary,
} from '../../../core/models/prescription.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { PrescriptionService } from '../services/prescription.service';

/**
 * Shows a visit's prescription in a print layout and prints it. Each print is recorded with its content, so the reprint list
 * shows what was printed earlier, not the current records (plan Sprint 9, item 41). The print dialog opens once the printed
 * content is on screen.
 */
@Component({
  selector: 'app-prescription-page',
  imports: [RouterLink, DatePipe, MatButtonModule, MatProgressBarModule, TranslatePipe],
  templateUrl: './prescription-page.component.html',
  styleUrl: './prescription-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrescriptionPageComponent implements OnInit {
  // Bound from the route (withComponentInputBinding).
  readonly visitId = input.required<string>();

  private readonly prescriptions = inject(PrescriptionService);
  private readonly notifications = inject(NotificationService);
  private readonly translations = inject(TranslationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  /** The content on screen: the visit's current content, or the content of an earlier print. */
  readonly document = signal<PrescriptionDocument | null>(null);
  /** When the content on screen was printed; null when it is the visit's current content. */
  readonly printedAt = signal<string | null>(null);
  readonly prints = signal<readonly PrescriptionPrintSummary[]>([]);
  readonly busy = signal(false);
  readonly issueError = signal<string | null>(null);
  readonly issued = computed(() => this.document() !== null);

  ngOnInit(): void {
    this.load();
  }

  /** Issues the prescription for the visit. A visit that is not completed or has no medication is refused inline. */
  issue(): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.issueError.set(null);
    this.prescriptions
      .issue(Number(this.visitId()))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.load();
        },
        error: (error: unknown) => {
          this.busy.set(false);
          this.issueError.set(this.errorMessage(error));
        },
      });
  }

  /** Records a print of the visit's current content, shows what was printed, and opens the print dialog. */
  print(): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.prescriptions
      .recordPrint(Number(this.visitId()))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (printed) => {
          this.busy.set(false);
          this.show(printed);
          this.refreshPrints();
        },
        error: (error: unknown) => {
          this.busy.set(false);
          this.notifications.showError(this.errorMessage(error));
        },
      });
  }

  /** Reprints an earlier print from its snapshot: the content that was printed then, not the current records. */
  reprint(summary: PrescriptionPrintSummary): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.prescriptions
      .getPrint(Number(this.visitId()), summary.prescriptionPrintId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (printed) => {
          this.busy.set(false);
          this.show(printed);
        },
        error: (error: unknown) => {
          this.busy.set(false);
          this.notifications.showError(this.errorMessage(error));
        },
      });
  }

  /** Shows the visit's current content again, after an earlier print was on screen. */
  showCurrent(): void {
    this.load();
  }

  private load(): void {
    const visitId = Number(this.visitId());
    this.loading.set(true);
    this.loadFailed.set(false);
    this.prescriptions
      .get(visitId)
      .pipe(
        catchError((error: unknown) =>
          this.isNotIssued(error) ? of(null) : throwError(() => error),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (document) => {
          this.loading.set(false);
          this.document.set(document);
          this.printedAt.set(null);
          if (document === null) {
            this.prints.set([]);
          } else {
            this.refreshPrints();
          }
        },
        error: () => {
          this.loading.set(false);
          this.loadFailed.set(true);
        },
      });
  }

  private refreshPrints(): void {
    this.prescriptions
      .listPrints(Number(this.visitId()))
      .pipe(
        catchError(() => of([] as PrescriptionPrintSummary[])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((prints) => this.prints.set(prints));
  }

  /** Puts a print's content on screen, then opens the print dialog once that content has rendered. */
  private show(printed: PrescriptionPrint): void {
    this.document.set(printed.document);
    this.printedAt.set(printed.printedAt);
    afterNextRender(() => window.print(), { injector: this.injector });
  }

  private isNotIssued(error: unknown): boolean {
    return error instanceof ApiRequestError && error.status === 404;
  }

  private errorMessage(error: unknown): string {
    return error instanceof ApiRequestError
      ? error.message
      : this.translations.translate('errors.unknown');
  }
}
