import type { Plugin, PluginBuild } from 'esbuild';

const defineAspireOtelParamsPlugin: Plugin = {
  name: 'define-aspire-otel-params',
  setup(build: PluginBuild) {
    const options = build.initialOptions;
    options.define = {
        ...options.define ?? {},
        OTEL_EXPORTER_OTLP_ENDPOINT: JSON.stringify(process.env['OTEL_EXPORTER_OTLP_ENDPOINT']),
        OTEL_EXPORTER_OTLP_HEADERS: JSON.stringify(process.env['OTEL_EXPORTER_OTLP_HEADERS']),
        OTEL_RESOURCE_ATTRIBUTES: JSON.stringify(process.env['OTEL_RESOURCE_ATTRIBUTES']),
        OTEL_SERVICE_NAME: JSON.stringify(process.env['OTEL_SERVICE_NAME']),
    };
  }
};

export default [defineAspireOtelParamsPlugin];
