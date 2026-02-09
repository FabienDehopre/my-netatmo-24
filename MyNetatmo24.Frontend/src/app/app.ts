import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import * as z from 'zod/mini';

import { Anonymous } from '~domains/authentication/anonymous';
import { Authenticated } from '~domains/authentication/authenticated';
import { Authentication } from '~domains/authentication/authentication';
import { parse } from '~domains/shared/functions/parse';

const WEATHER_FORECAST_SCHEMA = z.strictObject({
  date: z.iso.date(),
  temperatureC: z.number(),
  temperatureF: z.number(),
  summary: z.string(),
});
type WeatherForecast = z.infer<typeof WEATHER_FORECAST_SCHEMA>;

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Anonymous, Authenticated, DatePipe],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly authentication = inject(Authentication);
  protected readonly title = signal('My Netatmo 24').asReadonly();
  protected readonly user = this.authentication.user;
  protected readonly weatherForecasts = httpResource<WeatherForecast[]>(
    () => '/api/weatherforecast',
    {
      parse: parse(z.array(WEATHER_FORECAST_SCHEMA)),
      defaultValue: [],
    }
  );

  protected login(): void {
    this.authentication.login('/');
  }

  protected logout(): void {
    this.authentication.logout('/');
  }

  protected loadWeatherForecast(): void {
    this.weatherForecasts.reload();
  }
}
