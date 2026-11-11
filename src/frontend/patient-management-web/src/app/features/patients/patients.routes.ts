import { Routes } from '@angular/router';

const patientFormRoute = () =>
  import('./pages/patient-form-page.component').then((module) => module.PatientFormPageComponent);

export const PATIENTS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/patient-list-page.component').then(
        (module) => module.PatientListPageComponent,
      ),
  },
  { path: 'new', loadComponent: patientFormRoute },
  { path: ':patientId/edit', loadComponent: patientFormRoute },
];
