/// <reference types="vitest/config" />
import angular from '@analogjs/vite-plugin-angular';
import tailwindcss from '@tailwindcss/vite';
import { playwright } from '@vitest/browser-playwright';
import { defineConfig } from 'vite';

export default defineConfig({
  resolve: {
    mainFields: ['module', 'browser'],
    tsconfigPaths: true,
  },
  plugins: [
    angular(),
    tailwindcss(),
  ],
  define: {
    /* eslint-disable n/prefer-global/process */
    'import.meta.env.VITE_OTEL_RESOURCE_ATTRIBUTES': JSON.stringify(process.env.OTEL_RESOURCE_ATTRIBUTES),
    'import.meta.env.VITE_OTEL_EXPORTER_OTLP_HEADERS': JSON.stringify(process.env.OTEL_EXPORTER_OTLP_HEADERS),
    /* eslint-enable n/prefer-global/process */
  },
  server: {
    port: 4200,
    open: false,
    hmr: {
      protocol: 'ws',
    },
  },
  build: {
    sourceMap: true,
    target: ['es2022'],
    outDir: 'dist',
    chunkSizeWarningLimit: 750,
    rolldownOptions: {
      onLog(level, log, handler) {
        // Remove warning about source map errors, which we don't care about.
        if (log.cause?.message === `Can't resolve original location of error.`) {
          return;
        }

        // Remove warning about @protobufjs/inquire using an eval expression as the code will never be hit.
        if (log.code === 'EVAL' && log.id?.includes('@protobufjs/inquire')) {
          return;
        }

        handler(level, log);
      },
    },
  },
  test: {
    setupFiles: ['./src/setup-angular.ts', './src/test-setup.ts'],
    browser: {
      enabled: true,
      provider: playwright(),
      instances: [{ browser: 'chromium' }],
      headless: true,
    },
    coverage: {
      provider: 'v8',
      reporter: ['json-summary', 'json'],
    },
  },
});
