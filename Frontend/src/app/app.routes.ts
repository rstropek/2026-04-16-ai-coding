import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'questionnaires', pathMatch: 'full' },
  {
    path: 'questionnaires',
    loadComponent: () => import('./questionnaires/questionnaires').then(m => m.Questionnaires),
  },
  {
    path: 'questionnaires/new',
    loadComponent: () => import('./questionnaire-editor/questionnaire-editor').then(m => m.QuestionnaireEditor),
  },
  {
    path: 'questionnaires/:id/edit',
    loadComponent: () => import('./questionnaire-editor/questionnaire-editor').then(m => m.QuestionnaireEditor),
  },
  {
    path: 'questionnaires/:id/answer',
    loadComponent: () => import('./answer-questionnaire/answer-questionnaire').then(m => m.AnswerQuestionnaire),
  },
  {
    path: 'about',
    loadComponent: () => import('./about/about').then(m => m.About),
  },
];
