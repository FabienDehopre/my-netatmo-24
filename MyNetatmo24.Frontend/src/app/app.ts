import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLogOut } from '@ng-icons/lucide';

import { Anonymous } from '@app/shared/ui-auth/anonymous';
import { Authenticated } from '@app/shared/ui-auth/authenticated';
import { Authentication } from '@app/shared/util-auth/authentication';
import { AppStore } from '@app/shared/util-state/app.store';
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
  readonly #appStore = inject(AppStore);
  protected readonly title = signal('My Netatmo 24');
  protected readonly user = this.#authentication.user;
  protected readonly showLayout = computed(() => {
    const fullScreenPage = this.#appStore.fullScreenPage();
    return !fullScreenPage;
  });

  protected login(): void {
    this.#authentication.login('/');
  }

  protected logout(): void {
    this.#authentication.logout('/');
  }
}
