import {
  APP_INITIALIZER,
  ApplicationConfig,
  importProvidersFrom,
  provideExperimentalZonelessChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import {HttpRequest, provideHttpClient} from "@angular/common/http";
import {KeycloakService, KeycloakAngularModule } from "keycloak-angular";

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
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html'
      },
      shouldAddToken: (request: HttpRequest<unknown>): boolean => {
        return request.url.startsWith('api/') || request.url.startsWith('/api/');
      }
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideExperimentalZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(),
    {
      provide: APP_INITIALIZER,
      useFactory: initKeycloak,
      deps: [KeycloakService],
      multi: true
    },
    importProvidersFrom(KeycloakAngularModule)
  ]
};
