import { Routes } from '@angular/router';

import { placeholderRoutes } from '../../shared/routing/placeholder-routes';

const patientFormRoute = () =>
  import('./pages/patient-form-page.component').then((module) => module.PatientFormPageComponent);

export const PATIENTS_ROUTES: Routes = [
  { path: 'new', loadComponent: patientFormRoute },
  { path: ':patientId/edit', loadComponent: patientFormRoute },
  // The patient list and profile screens arrive in later items; until then the list route shows a placeholder.
  ...placeholderRoutes('nav.patients'),
];
