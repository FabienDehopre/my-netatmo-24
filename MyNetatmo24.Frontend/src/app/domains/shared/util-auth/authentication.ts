import { httpResource } from '@angular/common/http';
import { computed, DOCUMENT, inject, Injectable } from '@angular/core';

import { parse } from '@app/shared/util-common/parse';

import { USER_SCHEMA } from './user';

@Injectable({ providedIn: 'root' })
export class Authentication {
  readonly #document = inject(DOCUMENT);
  readonly #userResource = httpResource(() => '/bff/user', {
    parse: parse(USER_SCHEMA),
  });

  readonly user = computed(() => this.#userResource.value());

  login(redirectUrl: string): void {
    this.#document.location.href = `/bff/login?returnUrl=${encodeURIComponent(redirectUrl)}`;
  }

  logout(redirectUrl: string): void {
    this.#document.location.href = `/bff/logout?returnUrl=${encodeURIComponent(redirectUrl)}`;
  }
}
