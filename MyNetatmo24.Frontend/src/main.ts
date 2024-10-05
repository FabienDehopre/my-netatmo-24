import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { initializeTelemetry } from './instrumentation';

declare const OTEL_EXPORTER_OTLP_ENDPOINT: string;
declare const OTEL_EXPORTER_OTLP_HEADERS: string;
declare const OTEL_RESOURCE_ATTRIBUTES: string;
declare const OTEL_SERVICE_NAME: string;

initializeTelemetry(
  OTEL_EXPORTER_OTLP_ENDPOINT,
  OTEL_EXPORTER_OTLP_HEADERS,
  OTEL_RESOURCE_ATTRIBUTES,
  OTEL_SERVICE_NAME);

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
