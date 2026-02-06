import type { ApplicationConfig } from '@angular/core';

import { provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideOpenTelemetryInstrumentation } from '@opentelemetry';

import { ROUTES } from './app.routes';

export const APP_CONFIG: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(ROUTES),
    provideOpenTelemetryInstrumentation(),
  ],
};
