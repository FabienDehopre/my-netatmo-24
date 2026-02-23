import { existsSync } from 'node:fs';
import { unlink } from 'node:fs/promises';

import { expect, test } from '@playwright/test';

const STORAGE_STATE = '.state/auth-state.json';

test('cleanup auth state', async () => {
  try {
    await unlink(STORAGE_STATE);
  } catch {
    // File might not exist, which is fine
  }

  expect(existsSync(STORAGE_STATE)).toBe(false);
});
