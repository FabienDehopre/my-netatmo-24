import type { EnvironmentProviders } from '@angular/core';

import { isDevMode, provideAppInitializer } from '@angular/core';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BatchSpanProcessor, ConsoleSpanExporter, SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';

/**
 * Provides OpenTelemetry instrumentation for Angular applications.
 *
 * @returns An EnvironmentProviders array that sets up OpenTelemetry tracing.
 */
export function provideOpenTelemetryInstrumentation(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const resource = resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'angular-frontend',
      [ATTR_SERVICE_VERSION]: '1.0.0',
    });

    const provider = new WebTracerProvider({
      resource,
      spanProcessors: [
        ...(isDevMode() ? [new SimpleSpanProcessor(new ConsoleSpanExporter())] : []),
        new BatchSpanProcessor(
          new OTLPTraceExporter({
            url: `${window.origin}/v1/traces`,
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
          '@opentelemetry/instrumentation-user-interaction': {},
          '@opentelemetry/instrumentation-fetch': {},
          '@opentelemetry/instrumentation-xml-http-request': {},
        }),
      ],
    });
  });
}
