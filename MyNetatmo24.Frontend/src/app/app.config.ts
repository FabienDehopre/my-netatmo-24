import {
  APP_INITIALIZER,
  ApplicationConfig,
  importProvidersFrom,
  provideExperimentalZonelessChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import {HTTP_INTERCEPTORS, HttpRequest, provideHttpClient, withInterceptorsFromDi} from "@angular/common/http";
import {KeycloakService, KeycloakAngularModule, KeycloakBearerInterceptor} from "keycloak-angular";

interface KeycloakConfig {
  url?: string;
  realm: string;
  clientId: string;
}

export function initKeycloak(keycloakService: KeycloakService): () => Promise<void> {
  return async (): Promise<void> => {
    const config = await fetch('/keycloak.json').then(r => r.json() as Promise<KeycloakConfig>);
    await keycloakService.init({
      config,
      initOptions: {
        onLoad: 'login-required',
        // silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
        scope: 'profile email',
        enableLogging: true,
      },
      shouldAddToken: (request: HttpRequest<unknown>): boolean => {
        return request.url.startsWith('api/') || request.url.startsWith('/api/');
      },
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideExperimentalZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: APP_INITIALIZER,
      useFactory: initKeycloak,
      deps: [KeycloakService],
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: KeycloakBearerInterceptor,
      multi: true
    },
    importProvidersFrom(KeycloakAngularModule)
  ]
};
