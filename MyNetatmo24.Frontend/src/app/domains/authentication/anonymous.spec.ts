import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { render, screen } from '@testing-library/angular';
import { describe, expect, test } from 'vitest';

import { Anonymous } from './anonymous';

async function setup() {
  const { fixture } = await render('<div *appAnonymous>I am anonymous</div>', {
    imports: [Anonymous],
  });
  const mock = TestBed.inject(HttpTestingController);

  return {
    mockRequest: async (response: object) => {
      const request = mock.expectOne('/bff/user');
      request.flush(response);

      await fixture.whenStable();
      fixture.detectChanges();

      return response;
    },
  };
}

describe('anonymous Directive', () => {
  test('displays content when the user is unauthenticated', async () => {
    const { mockRequest } = await setup();
    await mockRequest({ isAuthenticated: false, name: null, claims: [] });

    expect(await screen.findByText('I am anonymous')).toBeInTheDocument();
  });

  test('does not display content when the user is authenticated', async () => {
    const { mockRequest } = await setup();
    await mockRequest({ isAuthenticated: true, name: 'Tester', claims: [] });

    expect(screen.queryByText('I am anonymous')).not.toBeInTheDocument();
  });
});
