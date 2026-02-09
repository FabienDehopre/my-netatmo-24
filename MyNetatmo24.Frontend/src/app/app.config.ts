import type { ApplicationConfig } from '@angular/core';

import { DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import { provideHttpClient, withFetch, withXsrfConfiguration } from '@angular/common/http';
import { provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';

import { provideEventPlugins } from '~domains/shared/event-managers';
import { provideOpenTelemetryInstrumentation } from '~domains/shared/opentelemetry';

import { ROUTES } from './app.routes';

export const APP_CONFIG: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(
      ROUTES,
      withComponentInputBinding(),
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
      })
    ),
    provideHttpClient(
      withXsrfConfiguration({
        cookieName: '__MyNetatmo24-X-XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN',
      }),
      withFetch()
    ),
    {
      provide: DATE_PIPE_DEFAULT_OPTIONS,
      useValue: { dateFormat: 'dd-MM-yyyy' },
    },
    provideEventPlugins(),
    provideOpenTelemetryInstrumentation(),
  ],
};
