import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/feed/feed').then(m => m.Feed),
  },
  {
    path: 'events/:id',
    loadComponent: () => import('./features/event-detail/event-detail').then(m => m.EventDetail),
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile').then(m => m.Profile),
  },
  { path: '**', redirectTo: '' },
];
