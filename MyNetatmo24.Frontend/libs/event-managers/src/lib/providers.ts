import type { Provider } from '@angular/core';

import { EVENT_MANAGER_PLUGINS } from '@angular/platform-browser';

import { PreventDefaultEventPlugin } from './prevent-default-event';

export function provideEventPlugins(): Provider[] {
  return [
    { provide: EVENT_MANAGER_PLUGINS, multi: true, useClass: PreventDefaultEventPlugin },
  ];
}
