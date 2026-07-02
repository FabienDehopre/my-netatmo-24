import { defineConfig } from '@fabdeh/eslint-config';
import sheriff from '@softarc/eslint-plugin-sheriff';

export default defineConfig({
  ignores: ['libs/ui'],
  tailwindcss: {
    entryPoint: 'src/styles.css',
  },
}, {
  name: 'sheriff',
  files: ['src/**/*.ts'],
  extends: [sheriff.configs.all],
});
