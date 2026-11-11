import { Routes } from '@angular/router';

import { ShellComponent } from './layout/shell/shell.component';

/** Every route sits under the shell. Each feature is lazy-loaded from its own routes file (CLAUDE.md 8.5). */
export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
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
