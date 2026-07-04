import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { Temporal } from 'temporal-polyfill';

interface AppState {
  fullScreenPage: boolean;
  currentTime: string;
  /* eslint-disable @typescript-eslint/naming-convention -- private members */
  _intervalId: number | undefined;
  /* eslint-enable @typescript-eslint/naming-convention -- private members */
}

const INITIAL_STATE: AppState = {
  fullScreenPage: false,
  currentTime: Temporal.Now.plainDateTimeISO().toLocaleString(),
  _intervalId: undefined,
};

export const AppStore = signalStore(
  { providedIn: 'root' },
  withState(INITIAL_STATE),
  withMethods((state) => ({
    enterFullScreenPage(): void {
      patchState(state, { fullScreenPage: true });
    },
    leaveFullScreenPage(): void {
      patchState(state, { fullScreenPage: false });
    },
  })),
  withHooks({
    onInit(state): void {
      const intervalId = setInterval(() => {
        patchState(state, { currentTime: Temporal.Now.plainDateTimeISO().toLocaleString() });
      }, 1000) as unknown as number;
      patchState(state, { _intervalId: intervalId });
    },
    onDestroy(state): void {
      const intervalId = state._intervalId();
      if (intervalId !== undefined) {
        clearInterval(intervalId);
        patchState(state, { _intervalId: undefined });
      }
    },
  })
);
