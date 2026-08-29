import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    loadComponent: () => import('./features/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent),
  },

  // Student
  {
    path: 'quizzes',
    canActivate: [roleGuard('Student')],
    loadComponent: () => import('./features/student/quiz-list.component').then((m) => m.QuizListComponent),
  },
  {
    path: 'quizzes/:id/take',
    canActivate: [roleGuard('Student')],
    loadComponent: () => import('./features/student/take-quiz.component').then((m) => m.TakeQuizComponent),
  },
  {
    path: 'history',
    canActivate: [roleGuard('Student')],
    loadComponent: () => import('./features/student/history.component').then((m) => m.HistoryComponent),
  },
  {
    path: 'attempts/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/student/attempt-result.component').then((m) => m.AttemptResultComponent),
  },

  // Instructor
  {
    path: 'teach',
    canActivate: [roleGuard('Instructor')],
    loadComponent: () =>
      import('./features/instructor/my-quizzes.component').then((m) => m.MyQuizzesComponent),
  },
  {
    path: 'teach/new',
    canActivate: [roleGuard('Instructor')],
    loadComponent: () =>
      import('./features/instructor/quiz-editor.component').then((m) => m.QuizEditorComponent),
  },
  {
    path: 'teach/:id',
    canActivate: [roleGuard('Instructor')],
    loadComponent: () =>
      import('./features/instructor/quiz-editor.component').then((m) => m.QuizEditorComponent),
  },
  {
    path: 'teach/:id/results',
    canActivate: [roleGuard('Instructor')],
    loadComponent: () =>
      import('./features/instructor/quiz-results.component').then((m) => m.QuizResultsComponent),
  },

  { path: '**', redirectTo: '' },
];
