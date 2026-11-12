import { Routes } from '@angular/router';

import { placeholderRoutes } from '../../shared/routing/placeholder-routes';

/** Visit screens come first; the bare consultation page stays a placeholder until its item is built. */
export const CONSULTATION_ROUTES: Routes = [
  {
    path: 'visits/:visitId/vitals',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/visit-vitals-page.component').then(
        (module) => module.VisitVitalsPageComponent,
      ),
  },
  {
    path: 'visits/:visitId/notes',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/visit-notes-page.component').then((module) => module.VisitNotesPageComponent),
  },
  ...placeholderRoutes('nav.consultation'),
];
