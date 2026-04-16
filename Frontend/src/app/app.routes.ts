import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'questionnaires', pathMatch: 'full' },
  {
    path: 'questionnaires',
    loadComponent: () => import('./questionnaires/questionnaires').then(m => m.Questionnaires),
  },
  {
    path: 'about',
    loadComponent: () => import('./about/about').then(m => m.About),
  },
];
