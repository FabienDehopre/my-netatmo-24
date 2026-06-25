import { Directive, effect, inject, TemplateRef, ViewContainerRef } from '@angular/core';

import { Authentication } from './authentication';

@Directive({
  selector: '[appAuthenticated]',
})
export class Authenticated {
  readonly #viewContainerRef = inject(ViewContainerRef);
  readonly #templateRef = inject(TemplateRef);
  readonly #authenticationService = inject(Authentication);

  constructor() {
    effect(() => {
      if (this.#authenticationService.user()?.isAuthenticated) {
        this.#viewContainerRef.createEmbeddedView(this.#templateRef);
      } else {
        this.#viewContainerRef.clear();
      }
    });
  }
}
