import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import {provideHttpClient, withInterceptors} from "@angular/common/http";
import {authHttpInterceptorFn, provideAuth0} from "@auth0/auth0-angular";

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideAuth0({
      domain: 'auth.dehopre.dev',
      clientId: 'mNlNx1rQbJRMGW49fYrQWpBsuGrMe2RW',
      authorizationParams: {
        redirect_uri: window.location.origin,
        // audience: 'https://fabdeh.eu.auth0.com/api/v2/',
        audience: 'https://my-netatmo24-api',
        scope: 'profile email read:weatherdata read:current_user',
      },
      cacheLocation: 'localstorage',
      httpInterceptor: {
        allowedList: [
          {
            uri: 'https://fabdeh.eu.auth0.com/api/v2/*',
            tokenOptions: {
              authorizationParams: {
                audience: 'https://fabdeh.eu.auth0.com/api/v2/',
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
