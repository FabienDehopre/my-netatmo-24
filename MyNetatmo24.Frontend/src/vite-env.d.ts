/* eslint-disable @typescript-eslint/naming-convention */

/// <reference types="vite/client" />

type Nullable<T> = T | null | undefined;

interface ImportMetaEnv {
  readonly OTEL_RESOURCE_ATTRIBUTES?: Nullable<string>;
  readonly OTEL_EXPORTER_OTLP_HEADERS?: Nullable<string>;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
