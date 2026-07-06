import type { Routes } from '@angular/router';

export const ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'home',
  },
  {
    path: 'home',
    loadComponent: () => import('./shell/home/home').then((m) => m.Home),
  },
  {
    path: '',
    resolve: {},
    children: [
      // TODO
      // This _needs_ to be the last route!!
      {
        path: '**',
        loadChildren: () => import('./shell/not-found/not-found').then((m) => m.NotFound),
      },
    ],
  },
];
