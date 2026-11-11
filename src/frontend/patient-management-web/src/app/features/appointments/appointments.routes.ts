import { Routes } from '@angular/router';

export const APPOINTMENTS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./calendar/calendar-page.component').then((module) => module.CalendarPageComponent),
  },
];
