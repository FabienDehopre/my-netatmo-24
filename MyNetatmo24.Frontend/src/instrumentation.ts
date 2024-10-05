import {
  BatchSpanProcessor,
  ConsoleSpanExporter,
  SimpleSpanProcessor,
  WebTracerProvider
} from "@opentelemetry/sdk-trace-web";
import {OTLPTraceExporter} from "@opentelemetry/exporter-trace-otlp-proto";
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import {registerInstrumentations} from "@opentelemetry/instrumentation";
import { ZoneContextManager } from '@opentelemetry/context-zone';
import {isDevMode} from "@angular/core";
import {Resource} from "@opentelemetry/resources";
import {ATTR_SERVICE_NAME} from "@opentelemetry/semantic-conventions";

function parseDelimitedValues(s: string): Record<string, string> {
  const headers = s.split(","); // Split by comma
  const o: Record<string, string> = {};

  headers.forEach((header) => {
    const [key, value] = header.split("="); // Split by equal sign
    o[key.trim()] = value.trim(); // Add to the object, trimming spaces
  });

  return o;
}

export function initializeTelemetry(otlpUrl: string,
                                    headers: string,
                                    resourceAttributes: string,
                                    serviceName: string): void {
  const provider = new WebTracerProvider({
    resource: new Resource({
      ...parseDelimitedValues(resourceAttributes),
      [ATTR_SERVICE_NAME]: serviceName,
    }),
  });

  if (isDevMode()) {
    provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
  }

  provider.addSpanProcessor(
    new BatchSpanProcessor(
      new OTLPTraceExporter({
        url: `${otlpUrl}/v1/traces`,
        headers: parseDelimitedValues(headers),
      })
    )
  );

  provider.register({
    contextManager: new ZoneContextManager(),
  });

  registerInstrumentations({
    instrumentations: [
      getWebAutoInstrumentations({
        '@opentelemetry/instrumentation-document-load': {},
        '@opentelemetry/instrumentation-user-interaction': {
          eventNames: ["click", "submit"],
          shouldPreventSpanCreation: (_, element) => {
            return (
              element.tagName === "A" ||
              (element.tagName === "BUTTON" &&
                element.getAttribute("type") === "submit")
            );
          },
        },
        '@opentelemetry/instrumentation-fetch': {
          propagateTraceHeaderCorsUrls: [new RegExp(`\\/api\\/*`)]
        },
        '@opentelemetry/instrumentation-xml-http-request': {
          propagateTraceHeaderCorsUrls: [new RegExp(`\\/api\\/*`)]
        },
      })
    ]
  });
}
