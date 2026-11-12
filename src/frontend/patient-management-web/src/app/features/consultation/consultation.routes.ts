import { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';
import { placeholderRoutes } from '../../shared/routing/placeholder-routes';

/**
 * Visit screens come first; the bare consultation page stays a placeholder until its item is built. Both visit forms ask
 * before leaving with unsaved edits (CLAUDE.md 8.3).
 */
export const CONSULTATION_ROUTES: Routes = [
  {
    path: 'visits/:visitId/vitals',
    pathMatch: 'full',
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () =>
      import('./pages/visit-vitals-page.component').then(
        (module) => module.VisitVitalsPageComponent,
      ),
  },
  {
    path: 'visits/:visitId/notes',
    pathMatch: 'full',
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () =>
      import('./pages/visit-notes-page.component').then((module) => module.VisitNotesPageComponent),
  },
  ...placeholderRoutes('nav.consultation'),
];
