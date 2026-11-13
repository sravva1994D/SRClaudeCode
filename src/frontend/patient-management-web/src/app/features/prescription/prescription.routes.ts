import { Routes } from '@angular/router';

import { placeholderRoutes } from '../../shared/routing/placeholder-routes';

/** The printable prescription for a visit. The bare prescription page stays a placeholder until its own item. */
export const PRESCRIPTION_ROUTES: Routes = [
  {
    path: 'visits/:visitId',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/prescription-page.component').then(
        (module) => module.PrescriptionPageComponent,
      ),
  },
  ...placeholderRoutes('nav.prescription'),
];
