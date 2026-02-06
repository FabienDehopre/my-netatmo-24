import { defineConfig } from '@fabdeh/eslint-config';

export default defineConfig({
  angular: {
    banDeveloperPreviewApi: false,
    banExperimentalApi: false,
  },
  tailwindcss: {
    entryPoint: 'src/styles.css',
  },
});
