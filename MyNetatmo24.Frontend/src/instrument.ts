import {
  BatchSpanProcessor,
  ConsoleSpanExporter,
  SimpleSpanProcessor,
  WebTracerProvider
} from "@opentelemetry/sdk-trace-web";
import {OTLPTraceExporter} from "@opentelemetry/exporter-trace-otlp-http";
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import {registerInstrumentations} from "@opentelemetry/instrumentation";
import { ZoneContextManager } from '@opentelemetry/context-zone';
import {isDevMode} from "@angular/core";

const provider = new WebTracerProvider();

if (isDevMode()) {
  provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
}

provider.addSpanProcessor(
  new BatchSpanProcessor(
    new OTLPTraceExporter({
      url: 'http://localhost:19123'
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
      '@opentelemetry/instrumentation-user-interaction': {},
      '@opentelemetry/instrumentation-fetch': {},
      '@opentelemetry/instrumentation-xml-http-request': {},
    })
  ]
});
