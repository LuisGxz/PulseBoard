import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboards' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'dashboards',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboards/dashboards-list.component').then(m => m.DashboardsListComponent),
  },
  {
    path: 'dashboards/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboards/dashboard-view.component').then(m => m.DashboardViewComponent),
  },
  {
    path: 'datasets',
    canActivate: [authGuard],
    loadComponent: () => import('./features/datasets/datasets-list.component').then(m => m.DatasetsListComponent),
  },
  {
    path: 'datasets/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/datasets/dataset-detail.component').then(m => m.DatasetDetailComponent),
  },
  {
    path: 'about',
    canActivate: [authGuard],
    loadComponent: () => import('./features/about/about.component').then(m => m.AboutComponent),
  },
  { path: '**', redirectTo: 'dashboards' },
];
