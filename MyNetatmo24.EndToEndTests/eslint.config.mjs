// @ts-check

import { defineConfig } from '@fabdeh/eslint-config';
import playwright from 'eslint-plugin-playwright';

export default defineConfig(
  { ignores: ['playwright-report', 'test-results', 'aspire-export'], typescript: { useRelaxedNamingConventionForCamelAndPascalCases: true } },
  playwright.configs['flat/recommended']
);
