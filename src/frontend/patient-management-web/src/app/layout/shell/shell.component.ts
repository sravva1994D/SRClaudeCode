import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { idleTimeouts } from '../../core/auth/idle-timeout';
import { NotificationService } from '../../core/notifications/notification.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface NavigationItem {
  readonly path: string;
  readonly labelKey: string;
}

/** Order is the order of the side navigation. */
const NAVIGATION_ITEMS: readonly NavigationItem[] = [
  { path: '/dashboard', labelKey: 'nav.dashboard' },
  { path: '/patients', labelKey: 'nav.patients' },
  { path: '/appointments', labelKey: 'nav.appointments' },
  { path: '/consultation', labelKey: 'nav.consultation' },
  { path: '/prescription', labelKey: 'nav.prescription' },
  { path: '/history', labelKey: 'nav.history' },
  { path: '/export', labelKey: 'nav.export' },
  { path: '/settings', labelKey: 'nav.settings' },
];

/** Authenticated layout: toolbar, side navigation and the content area. Signs out after 30 minutes of inactivity. */
@Component({
  selector: 'app-shell',
  imports: [
    MatButtonModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    TranslatePipe,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly navigationItems = NAVIGATION_ITEMS;

  ngOnInit(): void {
    idleTimeouts(document)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.notifications.showSuccess('auth.idleSignedOut');
        this.endSession();
      });
  }

  signOut(): void {
    this.endSession();
  }

  private endSession(): void {
    const goToLogin = () => void this.router.navigate(['/login']);
    this.auth.logout().subscribe({ complete: goToLogin, error: goToLogin });
  }
}
