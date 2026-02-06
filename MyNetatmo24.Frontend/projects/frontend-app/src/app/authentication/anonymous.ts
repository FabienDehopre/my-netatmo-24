import { Directive, effect, inject, TemplateRef, ViewContainerRef } from '@angular/core';

import { Authentication } from './authentication';

@Directive({
  selector: '[appAnonymous]',
})
export class Anonymous {
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly templateRef = inject(TemplateRef);
  private readonly authenticationService = inject(Authentication);

  constructor() {
    effect(() => {
      if (this.authenticationService.user()?.isAuthenticated === false) {
        this.viewContainerRef.createEmbeddedView(this.templateRef);
      } else {
        this.viewContainerRef.clear();
      }
    });
  }
}
