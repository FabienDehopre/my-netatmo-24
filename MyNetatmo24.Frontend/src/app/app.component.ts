import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {HttpClient} from "@angular/common/http";

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  private readonly http = inject(HttpClient);
  readonly title = signal('MyNetatmo24').asReadonly();
  readonly forecasts = signal<WeatherForecast[]>([]);

  constructor() {
    this.http.get<WeatherForecast[]>('api/WeatherForecast').subscribe({
      next: (result) => {
        this.forecasts.set(result);
      },
      error: (error) => console.error(error)
    });
  }
}
