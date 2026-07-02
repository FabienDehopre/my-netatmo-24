import type { Routes } from '@angular/router';

import { Home } from './shell/home/home';
import { NotFound } from './shell/not-found/not-found';

export const ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'home',
  },
  {
    path: 'home',
    component: Home,
  },
  {
    path: '',
    resolve: {},
    children: [
      // TODO
      // This _needs_ to be the last route!!
      {
        path: '**',
        component: NotFound,
      },
    ],
  },
];
