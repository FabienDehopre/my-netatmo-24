import type { ApplicationConfig } from '@angular/core';

import { DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import { provideHttpClient, withXsrfConfiguration } from '@angular/common/http';
import { provideBrowserGlobalErrorListeners } from '@angular/core';
import {
  PreloadAllModules,
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withPreloading
} from '@angular/router';

import { provideEventPlugins } from '@app/event-managers';
import { provideOpenTelemetryInstrumentation } from '@app/opentelemetry';

import { ROUTES } from './app.routes';

export const APP_CONFIG: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      ROUTES,
      withComponentInputBinding(),
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
      }),
      withPreloading(PreloadAllModules)
    ),
    provideHttpClient(
      withXsrfConfiguration({
        cookieName: '__MyNetatmo24-X-XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN',
      })
    ),
    {
      provide: DATE_PIPE_DEFAULT_OPTIONS,
      useValue: { dateFormat: 'dd-MM-yyyy' },
    },
    provideEventPlugins(),
    provideOpenTelemetryInstrumentation(),
  ],
};
