import { defineConfig } from '@fabdeh/eslint-config';

export default defineConfig({
  ignores: ['libs/ui'],
  tailwindcss: {
    entryPoint: 'src/styles.css',
  },
});
