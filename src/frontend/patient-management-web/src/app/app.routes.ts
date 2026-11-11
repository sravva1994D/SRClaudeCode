import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './layout/shell/shell.component';

/**
 * The sign-in page sits outside the shell. Everything else sits under the shell, behind the auth guard. Each feature
 * is lazy-loaded from its own routes file (CLAUDE.md 8.5).
 */
export const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.routes').then((module) => module.AUTH_ROUTES),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then((module) => module.DASHBOARD_ROUTES),
      },
      {
        path: 'patients',
        loadChildren: () =>
          import('./features/patients/patients.routes').then((module) => module.PATIENTS_ROUTES),
      },
      {
        path: 'appointments',
        loadChildren: () =>
          import('./features/appointments/appointments.routes').then(
            (module) => module.APPOINTMENTS_ROUTES,
          ),
      },
      {
        path: 'consultation',
        loadChildren: () =>
          import('./features/consultation/consultation.routes').then(
            (module) => module.CONSULTATION_ROUTES,
          ),
      },
      {
        path: 'prescription',
        loadChildren: () =>
          import('./features/prescription/prescription.routes').then(
            (module) => module.PRESCRIPTION_ROUTES,
          ),
      },
      {
        path: 'history',
        loadChildren: () =>
          import('./features/history/history.routes').then((module) => module.HISTORY_ROUTES),
      },
      {
        path: 'export',
        loadChildren: () =>
          import('./features/export/export.routes').then((module) => module.EXPORT_ROUTES),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then((module) => module.SETTINGS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
