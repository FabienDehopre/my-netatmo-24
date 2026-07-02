import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLogOut } from '@ng-icons/lucide';

import { Anonymous } from '@app/shared/ui-auth/anonymous';
import { Authenticated } from '@app/shared/ui-auth/authenticated';
import { Authentication } from '@app/shared/util-auth/authentication';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Anonymous, Authenticated, HlmButtonImports, HlmDropdownMenuImports, NgIcon],
  templateUrl: './app.html',
  viewProviders: [provideIcons({ lucideLogOut })],
})
export class App {
  readonly #authentication = inject(Authentication);
  protected readonly title = signal('My Netatmo 24').asReadonly();
  protected readonly user = this.#authentication.user;

  protected login(): void {
    this.#authentication.login('/');
  }

  protected logout(): void {
    this.#authentication.logout('/');
  }
}
