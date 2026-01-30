import {EnvironmentProviders, isDevMode, provideAppInitializer} from '@angular/core';
import {resourceFromAttributes} from "@opentelemetry/resources";
import {ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION} from "@opentelemetry/semantic-conventions";
import {
  BatchSpanProcessor,
  ConsoleSpanExporter,
  SimpleSpanProcessor,
  WebTracerProvider
} from "@opentelemetry/sdk-trace-web";
import {OTLPTraceExporter} from "@opentelemetry/exporter-trace-otlp-proto";
import {ZoneContextManager} from "@opentelemetry/context-zone";
import {registerInstrumentations} from "@opentelemetry/instrumentation";
import {getWebAutoInstrumentations} from "@opentelemetry/auto-instrumentations-web";

function parseDelimitedValues(s: string): Record<string, string> {
  const headers = s.split(','); // Split by comma
  const result: Record<string, string> = {};

  headers.forEach(header => {
    const [key, value] = header.split('='); // Split by equal sign
    result[key.trim()] = value.trim(); // Add to the object, trimming spaces
  });

  return result;
}

export function provideInstrumentation(otlpUrl: string, headers: string, resourceAttributes: string, serviceName: string): EnvironmentProviders  {
  return provideAppInitializer(() => {
    const resource = resourceFromAttributes({
      ...parseDelimitedValues(resourceAttributes),
      [ATTR_SERVICE_NAME]: serviceName,
      [ATTR_SERVICE_VERSION]: '1.0.0',
    });

    const provider = new WebTracerProvider({
      resource,
      spanProcessors: [
        ...(isDevMode() ? [new SimpleSpanProcessor(new ConsoleSpanExporter())] : []),
        new BatchSpanProcessor(
          new OTLPTraceExporter({
            url: `${otlpUrl}/v1/traces`,
            headers: parseDelimitedValues(headers),
          })
        ),
      ]
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
            shouldPreventSpanCreation: (_, element) => {
              return (element.tagName === 'A' || (element.tagName === 'BUTTON' && element.getAttribute('type') === 'submit'));
            }
          },
          '@opentelemetry/instrumentation-fetch': {
            propagateTraceHeaderCorsUrls: [new RegExp(`\\/api\\/.*`)],
          },
          '@opentelemetry/instrumentation-xml-http-request': {
            propagateTraceHeaderCorsUrls: [new RegExp(`\\/api\\/.*`)],
          },
        }),
      ],
    });
  });
}
