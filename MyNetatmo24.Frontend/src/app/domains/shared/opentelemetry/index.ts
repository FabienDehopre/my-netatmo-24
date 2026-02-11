import type { EnvironmentProviders } from '@angular/core';

import { provideAppInitializer } from '@angular/core';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';

function parseDelimitedValues(s: string) {
  const headers = s.split(','); // Split by comma
  const result: Record<string, string> = {};

  for (const header of headers) {
    const [key, value] = header.split('='); // Split by equal sign
    result[key.trim()] = value.trim(); // Add to the object, trimming spaces
  }

  return result;
}

export function provideOpenTelemetryInstrumentation(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const resource = resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'angular-frontend',
      [ATTR_SERVICE_VERSION]: '1.0.0',
      ...parseDelimitedValues(import.meta.env.OTEL_RESOURCE_ATTRIBUTES ?? ''),
    });

    const provider = new WebTracerProvider({
      resource,
      spanProcessors: [
        new BatchSpanProcessor(
          new OTLPTraceExporter({
            url: `${window.origin}/v1/traces`,
            headers: parseDelimitedValues(import.meta.env.OTEL_EXPORTER_OTLP_HEADERS ?? ''),
          })
        ),
      ],
    });

    provider.register({
      contextManager: new ZoneContextManager(),
    });

    registerInstrumentations({
      instrumentations: [
        getWebAutoInstrumentations({
          '@opentelemetry/instrumentation-document-load': {},
          '@opentelemetry/instrumentation-user-interaction': {
            eventNames: ['click', 'submit'],
          },
          '@opentelemetry/instrumentation-fetch': {},
          '@opentelemetry/instrumentation-xml-http-request': {},
        }),
      ],
    });
  });
}
