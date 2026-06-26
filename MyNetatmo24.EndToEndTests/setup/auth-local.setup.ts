import { existsSync, statSync } from 'node:fs';

import { expect, test } from '@playwright/test';

import { getConfig } from '../utils/env';

const STORAGE_STATE = '.state/auth-state.json';

test('authenticate user (local)', async ({ page }) => {
  // Load and validate configuration
  const config = getConfig();

  // eslint-disable-next-line playwright/no-skipped-test -- no need to re-authenticate if already connected
  test.skip(
    isRecentlyAuthenticated(STORAGE_STATE),
    'Skipping authentication test because user is already authenticated'
  );

  await page.goto('/bff/login');

  const usernameInput = page.getByRole('textbox', { name: /Email Address/i });
  await usernameInput.fill(config.username);
  await usernameInput.press('Enter');

  const passwordInput = page.getByRole('textbox', { name: /Password/i });
  await passwordInput.fill(config.password);
  await passwordInput.press('Enter');

  const continueButton = page.getByRole('button', { name: /Continue without passkeys/i });
  await continueButton.click();

  await expect(page.getByText(new RegExp(config.username, 'i'))).toBeVisible();

  await page.context().storageState({ path: STORAGE_STATE });
});

function isRecentlyAuthenticated(filePath: string): boolean {
  try {
    if (!existsSync(filePath)) {
      return false;
    }

    const stats = statSync(filePath);
    const fileAge = Date.now() - stats.mtime.getTime();
    const maxAge = 3_600_000; // 1 hour in milliseconds

    return fileAge < maxAge;
  } catch {
    return false;
  }
}
