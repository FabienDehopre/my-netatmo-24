import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';
import { createAngularTable, createColumnHelper, FlexRenderDirective, getCoreRowModel } from '@tanstack/angular-table';
import * as z from 'zod/mini';

import { Anonymous } from '@app/shared/ui-auth/anonymous';
import { Authenticated } from '@app/shared/ui-auth/authenticated';
import { parse } from '@app/shared/util-shared/parse';
import { HlmButton } from '@spartan-ng/helm/button';

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
  selector: 'app-home',
  imports: [
    FlexRenderDirective,
    HlmButton,
    Authenticated,
    Anonymous,
  ],
  templateUrl: './home.html',
})
export class Home {
  readonly #weatherForecasts = httpResource<WeatherForecast[]>(
    () => '/api/weatherforecast',
    {
      parse: parse(z.array(WEATHER_FORECAST_SCHEMA)),
      defaultValue: [],
    }
  );

  protected readonly table = createAngularTable(() => ({
    data: this.#weatherForecasts.value(),
    columns: WEATHER_FORECAST_COLUMNS,
    getCoreRowModel: getCoreRowModel(),
  }));

  protected loadWeatherForecast(): void {
    this.#weatherForecasts.reload();
  }
}
