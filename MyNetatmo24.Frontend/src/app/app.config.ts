import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import {provideHttpClient, withInterceptors} from "@angular/common/http";
import {authHttpInterceptorFn, provideAuth0} from "@auth0/auth0-angular";
import {provideInstrumentation} from "../instrumentation";

declare const OTEL_EXPORTER_OTLP_ENDPOINT: string;
declare const OTEL_EXPORTER_OTLP_HEADERS: string;
declare const OTEL_RESOURCE_ATTRIBUTES: string;
declare const OTEL_SERVICE_NAME: string;

export const appConfig: ApplicationConfig = {
  providers: [
    provideInstrumentation(OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_HEADERS, OTEL_RESOURCE_ATTRIBUTES, OTEL_SERVICE_NAME),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideAuth0({
      domain: 'auth.dehopre.dev',
      clientId: 'mNlNx1rQbJRMGW49fYrQWpBsuGrMe2RW',
      authorizationParams: {
        redirect_uri: window.location.origin,
        // audience: 'https://auth.dehopre.dev/api/v2/',
        audience: 'https://my-netatmo24-api',
        scope: 'profile email read:weatherdata read:current_user',
      },
      cacheLocation: 'localstorage',
      httpInterceptor: {
        allowedList: [
          {
            uri: 'https://auth.dehopre.dev/api/v2/*',
            tokenOptions: {
              authorizationParams: {
                audience: 'https://auth.dehopre.dev/api/v2/',
                scope: 'read:current_user',
              }
            }
          },
          {
            uri: '/api/*',
            tokenOptions: {
              authorizationParams: {
                audience: 'https://my-netatmo24-api',
                scope: 'read:weatherdata',
              },
            }
          }
        ]
      }
    })
  ]
};
