import { NgOptimizedImage } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';

import { AppStore } from '@app/shared/util-state/app.store';

@Component({
  selector: 'app-not-found',
  imports: [NgOptimizedImage],
  templateUrl: './not-found.html',
})
export class NotFound {
  readonly #appState = inject(AppStore);
  readonly #destroyRef = inject(DestroyRef);

  constructor() {
    this.#appState.enterFullScreenPage();
    this.#destroyRef.onDestroy(() => {
      this.#appState.leaveFullScreenPage();
    });
  }
}
