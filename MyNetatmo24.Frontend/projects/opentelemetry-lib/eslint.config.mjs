import { defineProjectConfig } from '@fabdeh/eslint-config';

import BASE_CONFIG from '../../eslint.config.mjs';

export default defineProjectConfig(
  BASE_CONFIG,
  {
    type: 'lib',
    angular: {
      banDeveloperPreviewApi: false,
      banExperimentalApi: false,
    }
  }
);
