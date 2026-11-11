import { Routes } from '@angular/router';

/**
 * Routes for a feature screen that Sprint 1 only reserves. The page is loaded lazily and receives its
 * titles from route data through component input binding.
 */
export function placeholderRoutes(titleKey: string): Routes {
  return [
    {
      path: '',
      loadComponent: () =>
        import('../components/placeholder-page/placeholder-page.component').then(
          (module) => module.PlaceholderPageComponent,
        ),
      data: { titleKey, descriptionKey: 'placeholder.description' },
    },
  ];
}
