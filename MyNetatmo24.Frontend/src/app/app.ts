import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppStore } from '@app/shared/util-state/app.store';
import { HlmSidebarImports } from '@spartan-ng/helm/sidebar';

import { Sidebar } from './shell/sidebar/sidebar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Sidebar, HlmSidebarImports],
  templateUrl: './app.html',
})
export class App {
  readonly #appStore = inject(AppStore);
  protected readonly showLayout = computed(() => {
    const fullScreenPage = this.#appStore.fullScreenPage();
    return !fullScreenPage;
  });
}
