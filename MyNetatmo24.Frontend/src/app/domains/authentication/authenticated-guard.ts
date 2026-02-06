import type { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot } from '@angular/router';

import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { filterNullish } from '../shared/operators/filter-nullish';
import { Authentication } from './authentication';

// eslint-disable-next-line @typescript-eslint/naming-convention -- this is a convention for guards in Angular
export const authenticatedGuard: CanActivateFn = (_next: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const authenticationService = inject(Authentication);

  return toObservable(authenticationService.user).pipe(
    filterNullish(),
    map((user) => {
      if (user.isAuthenticated) {
        return true;
      }
      authenticationService.login(state.url);
      return false;
    })
  );
};
