import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { ApiRequestError } from '../../../core/http/api-request-error';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ClinicProfile } from '../../../core/models/clinic-profile.model';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { ClinicProfileService } from '../services/clinic-profile.service';

const MAXIMUM_NAME_LENGTH = 200;
const MAXIMUM_REGISTRATION_LENGTH = 50;
const MAXIMUM_ADDRESS_LENGTH = 500;
const MAXIMUM_PHONE_LENGTH = 20;
const MAXIMUM_FOOTER_LENGTH = 1000;
const MINIMUM_PASSWORD_LENGTH = 8;
const MAXIMUM_PASSWORD_LENGTH = 256;

/**
 * Clinic profile (prescription header and footer) and own-password change. The server checks every rule again;
 * these validators only give quick feedback.
 */
@Component({
  selector: 'app-settings-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslatePipe,
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPageComponent implements OnInit {
  private readonly clinicProfiles = inject(ClinicProfileService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly changingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);

  readonly profileForm = new FormGroup({
    clinicName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_NAME_LENGTH)],
    }),
    doctorName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_NAME_LENGTH)],
    }),
    qualifications: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_NAME_LENGTH)],
    }),
    registrationNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_REGISTRATION_LENGTH)],
    }),
    address: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_ADDRESS_LENGTH)],
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_PHONE_LENGTH), Validators.pattern(/^[0-9 +()-]*$/)],
    }),
    footerNotes: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(MAXIMUM_FOOTER_LENGTH)],
    }),
  });

  readonly passwordForm = new FormGroup({
    currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(MINIMUM_PASSWORD_LENGTH),
        Validators.maxLength(MAXIMUM_PASSWORD_LENGTH),
      ],
    }),
  });

  ngOnInit(): void {
    this.clinicProfiles
      .get()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profileForm.reset(toFormValue(profile));
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  saveProfile(): void {
    if (this.profileForm.invalid || this.savingProfile()) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.savingProfile.set(true);
    this.clinicProfiles
      .update(this.profileForm.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profileForm.reset(toFormValue(profile));
          this.savingProfile.set(false);
          this.notifications.showSuccess('settings.profileSaved');
        },
        error: () => this.savingProfile.set(false),
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid || this.changingPassword()) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    this.changingPassword.set(true);
    this.passwordError.set(null);
    this.auth
      .changePassword(currentPassword, newPassword)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Every session ended on the server, so the user signs in again with the new password.
        next: () => void this.router.navigate(['/login']),
        error: (error: unknown) => {
          this.changingPassword.set(false);
          this.passwordError.set(error instanceof ApiRequestError ? error.message : null);
        },
      });
  }
}

function toFormValue(profile: ClinicProfile) {
  return {
    clinicName: profile.clinicName ?? '',
    doctorName: profile.doctorName ?? '',
    qualifications: profile.qualifications ?? '',
    registrationNumber: profile.registrationNumber ?? '',
    address: profile.address ?? '',
    phoneNumber: profile.phoneNumber ?? '',
    footerNotes: profile.footerNotes ?? '',
  };
}
