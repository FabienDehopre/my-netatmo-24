import { defineProjectConfig } from '@fabdeh/eslint-config';

import BASE_CONFIG from '../../eslint.config.mjs';

export default defineProjectConfig(
  BASE_CONFIG,
  {
    type: 'app',
    tailwindcss: {
      entryPoint: 'projects/frontend-app/src/styles.css',
    },
    angular: {
      banDeveloperPreviewApi: false,
      banExperimentalApi: false,
    }
  }
);
