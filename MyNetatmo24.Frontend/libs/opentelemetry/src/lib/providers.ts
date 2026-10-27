import type { EnvironmentProviders } from '@angular/core';

import { provideAppInitializer } from '@angular/core';
import { context, propagation, trace } from '@opentelemetry/api';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { CompositePropagator, W3CBaggagePropagator, W3CTraceContextPropagator } from '@opentelemetry/core';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BatchSpanProcessor, TracerProvider } from '@opentelemetry/sdk-trace';
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

function getCookie(name: string): string | undefined {
  const prefix = `${name}=`;
  const found = document.cookie.split('; ').find((c) => c.startsWith(prefix));
  return found ? decodeURIComponent(found.slice(prefix.length)) : undefined;
}

export function provideOpenTelemetryInstrumentation(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const resource = resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'angular-frontend',
      [ATTR_SERVICE_VERSION]: '1.0.0',
      ...parseDelimitedValues(import.meta.env.OTEL_RESOURCE_ATTRIBUTES ?? ''),
    });

    const staticHeaders = parseDelimitedValues(import.meta.env.OTEL_EXPORTER_OTLP_HEADERS ?? '');
    const provider = new TracerProvider({
      resource,
      spanProcessors: [
        new BatchSpanProcessor({
          exporter: new OTLPTraceExporter({
            url: `${window.origin}/v1/traces`,
            headers: () => {
              const xsrf = getCookie('__MyNetatmo24-X-XSRF-TOKEN');
              return Promise.resolve({
                ...staticHeaders,
                ...(xsrf ? { 'X-XSRF-TOKEN': xsrf } : {}),
              });
            },
          }),
        }),
      ],
    });

    // sdk-trace 3.x dropped WebTracerProvider.register(), so wire up the globals it used to set.
    context.setGlobalContextManager(new ZoneContextManager().enable());
    propagation.setGlobalPropagator(
      new CompositePropagator({
        propagators: [new W3CTraceContextPropagator(), new W3CBaggagePropagator()],
      })
    );
    trace.setGlobalTracerProvider(provider);

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
