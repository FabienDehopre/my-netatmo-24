import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, CUSTOM_ELEMENTS_SCHEMA, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { createAngularTable, createColumnHelper, FlexRenderDirective, getCoreRowModel } from '@tanstack/angular-table';
import * as z from 'zod/mini';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
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

// eslint-disable-next-line @typescript-eslint/naming-convention -- function
const columnHelper = createColumnHelper<WeatherForecast>();
const WEATHER_FORECAST_COLUMNS = [
  columnHelper.accessor('date', {
    header: 'Date',
  }),
  columnHelper.accessor('temperatureC', {
    header: 'Temp. (C)',
  }),
  columnHelper.accessor('temperatureF', {
    header: 'Temp. (F)',
  }),
  columnHelper.accessor('summary', {
    header: 'Summary',
  }),
];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Anonymous, Authenticated, DatePipe, HlmButtonImports, HlmDropdownMenuImports, NgIcon, FlexRenderDirective],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class App {
  readonly #authentication = inject(Authentication);
  readonly #weatherForecasts = httpResource<WeatherForecast[]>(
    () => '/api/weatherforecast',
    {
      parse: parse(z.array(WEATHER_FORECAST_SCHEMA)),
      defaultValue: [],
    }
  );

  protected readonly title = signal('My Netatmo 24').asReadonly();
  protected readonly user = this.#authentication.user;
  protected readonly table = createAngularTable(() => ({
    data: this.#weatherForecasts.value(),
    columns: WEATHER_FORECAST_COLUMNS,
    getCoreRowModel: getCoreRowModel(),
  }));

  protected login(): void {
    this.#authentication.login('/');
  }

  protected logout(): void {
    this.#authentication.logout('/');
  }

  protected loadWeatherForecast(): void {
    this.#weatherForecasts.reload();
  }
}
