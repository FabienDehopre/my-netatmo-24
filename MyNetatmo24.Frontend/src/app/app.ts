import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLogOut } from '@ng-icons/lucide';
import { createAngularTable, createColumnHelper, FlexRenderDirective, getCoreRowModel } from '@tanstack/angular-table';
import * as z from 'zod/mini';

import { Anonymous } from '@app/shared/ui-auth/anonymous';
import { Authenticated } from '@app/shared/ui-auth/authenticated';
import { Authentication } from '@app/shared/util-auth/authentication';
import { parse } from '@app/shared/util-common/parse';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';

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
  imports: [RouterOutlet, Anonymous, Authenticated, HlmButtonImports, HlmDropdownMenuImports, NgIcon, FlexRenderDirective],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  viewProviders: [provideIcons({ lucideLogOut })],
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
