import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage),
  },
  {
    path: 'jobs',
    loadComponent: () =>
      import('./features/job-search/job-search.page').then((m) => m.JobSearchPage),
  },
  {
    path: 'jobs/:id',
    loadComponent: () =>
      import('./features/job-detail/job-detail.page').then((m) => m.JobDetailPage),
  },
  {
    path: 'tailor/:jobId',
    loadComponent: () =>
      import('./features/resume-tailor/resume-tailor.page').then((m) => m.ResumeTailorPage),
  },
  {
    path: 'resume',
    loadComponent: () =>
      import('./features/resume/resume.page').then((m) => m.ResumePage),
  },
  {
    path: '**',
    redirectTo: '/dashboard',
  },
];
