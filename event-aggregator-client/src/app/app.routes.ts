import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then(m => m.Login),
  },
  {
    path: 'login/2fa',
    loadComponent: () => import('./features/two-factor/two-factor').then(m => m.TwoFactor),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/register/register').then(m => m.Register),
  },
  {
    path: '',
    loadComponent: () => import('./features/feed/feed').then(m => m.Feed),
    canActivate: [authGuard],
  },
  {
    path: 'events/:id',
    loadComponent: () => import('./features/event-detail/event-detail').then(m => m.EventDetail),
    canActivate: [authGuard],
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile').then(m => m.Profile),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: '' },
];
