import type { ActivatedRouteSnapshot, GuardResult, RouterStateSnapshot } from '@angular/router';
import type { Observable } from 'rxjs';

import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { describe, expect, test, vi } from 'vitest';

import { authenticatedGuard } from './authenticated-guard';
import { Authentication } from './authentication';

async function setup(isAuthenticated: boolean) {
  const authenticated = {
    user: signal({
      isAuthenticated,
    }),
    login: vi.fn(),
  };
  TestBed.configureTestingModule({
    providers: [
      {
        provide: Authentication,
        useValue: authenticated,
      },
    ],
  });

  const guard = (await TestBed.runInInjectionContext(() => {
    const routeSnapshot = {} as ActivatedRouteSnapshot;
    const stateSnapshot = { url: '/page' } as RouterStateSnapshot;

    return authenticatedGuard(routeSnapshot, stateSnapshot);
  })) as Observable<GuardResult>;
  return { guard, authenticated };
}

describe('authenticated Guard', () => {
  test('allows access when user is authenticated', async () => {
    const { guard } = await setup(true);

    const result = await firstValueFrom(guard);

    expect(result).toBeTruthy();
  });

  test('invokes the login when user is unauthenticated', async () => {
    const { guard, authenticated } = await setup(false);

    const result = await firstValueFrom(guard);

    expect(result).toBeFalsy();
    expect(authenticated.login).toHaveBeenCalledWith('/page');
  });
});
