import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiRequestError } from '../../../core/http/api-request-error';
import { AuthService } from '../../../core/auth/auth.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

const MAXIMUM_USERNAME_LENGTH = 64;
const MAXIMUM_PASSWORD_LENGTH = 256;

/** Sign-in screen. Shows one generic message for every failure, so it never reveals whether a username exists. */
@Component({
  selector: 'app-login-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslatePipe,
  ],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = new FormGroup({
    username: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(MAXIMUM_USERNAME_LENGTH)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(MAXIMUM_PASSWORD_LENGTH)],
    }),
  });

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const { username, password } = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.auth
      .login(username, password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl(this.returnUrl()),
        error: (error: unknown) => {
          this.saving.set(false);
          this.form.controls.password.reset('');
          this.errorMessage.set(error instanceof ApiRequestError ? error.message : null);
        },
      });
  }

  /** Only in-app paths are accepted, so the return address cannot send the user to another site. */
  private returnUrl(): string {
    const requested = this.route.snapshot.queryParamMap.get('returnUrl');
    return requested !== null && requested.startsWith('/') && !requested.startsWith('//')
      ? requested
      : '/dashboard';
  }
}
