import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Anonymous } from './authentication/anonymous';
import { Authenticated } from './authentication/authenticated';
import { Authentication } from './authentication/authentication';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Anonymous, Authenticated],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly authentication = inject(Authentication);
  protected readonly title = signal('MyNetatmo24').asReadonly();
  protected readonly user = this.authentication.user;

  protected login() {
    this.authentication.login('/');
  }

  protected logout() {
    this.authentication.logout('/');
  }
}
