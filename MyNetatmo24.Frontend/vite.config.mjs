/// <reference types="vitest/config" />
import angular from '@analogjs/vite-plugin-angular';
import tailwindcss from '@tailwindcss/vite';
import { playwright } from '@vitest/browser-playwright';
import { defineConfig } from 'vite';
import { nodePolyfills } from 'vite-plugin-node-polyfills';
import viteTsConfigPaths from 'vite-tsconfig-paths';

// Patch @protobufjs/inquire library to work in Vite.
function protobufPatch() {
  return {
    name: 'protobuf-patch',
    transform(code, id) {
      // https://github.com/protobufjs/protobuf.js/issues/1754
      if (id.endsWith('@protobufjs/inquire/index.js')) {
        return {
          code: code.replace(`eval("quire".replace(/^/,"re"))`, 'require'),
          // eslint-disable-next-line unicorn/no-null
          map: null,
        };
      }
    },
  };
}

export default defineConfig({
  resolve: {
    mainFields: ['module', 'browser'],
  },
  plugins: [
    angular(),
    viteTsConfigPaths(),
    tailwindcss(),
    // Polyfill Node.js built-in modules for browser compatibility.
    nodePolyfills(),
    protobufPatch(),
  ],
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
    rollupOptions: {
      onLog(level, log, handler) {
        // Remove warning about source map errors, which we don't care about.
        if (log.cause?.message === `Can't resolve original location of error.`) {
          return;
        }

        handler(level, log);
      },
    },
  },
  optimizeDeps: {
    include: ['node:module'],
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
