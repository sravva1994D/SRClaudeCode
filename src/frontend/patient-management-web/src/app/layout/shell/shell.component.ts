import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

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

/** Authenticated layout: toolbar, side navigation and the content area. */
@Component({
  selector: 'app-shell',
  imports: [
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
export class ShellComponent {
  readonly navigationItems = NAVIGATION_ITEMS;
}
