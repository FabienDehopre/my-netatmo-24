/* eslint-disable @typescript-eslint/naming-convention */

/// <reference types="vite/client" />

type Nullable<T> = T | null | undefined;

interface ViteTypeOptions {
  // By adding this line, you can make the type of ImportMetaEnv strict
  // to disallow unknown keys.
  strictImportMetaEnv: unknown;
}

interface ImportMetaEnv {
  readonly VITE_OTEL_RESOURCE_ATTRIBUTES?: Nullable<string>;
  readonly VITE_OTEL_EXPORTER_OTLP_HEADERS?: Nullable<string>;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
