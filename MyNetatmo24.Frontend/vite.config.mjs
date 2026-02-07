import angular from '@analogjs/vite-plugin-angular';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import viteTsConfigPaths from 'vite-tsconfig-paths';

export default defineConfig({
  resolve: {
    mainFields: ['module'],
  },
  plugins: [
    angular(),
    tailwindcss(),
    viteTsConfigPaths(),
  ],
  server: {
    port: 4200,
    open: false,
  },
  build: {
    target: ['es2022'],
    outDir: 'dist',
  },
});
