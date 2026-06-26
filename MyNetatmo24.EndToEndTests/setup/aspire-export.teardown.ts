import { execFileSync } from 'node:child_process';
import { mkdir } from 'node:fs/promises';

export default async function globalTeardown(): Promise<void> {
  await mkdir('./aspire-export', { recursive: true });

  try {
    execFileSync(
      'aspire',
      ['export', '--apphost', '../MyNetatmo24.AppHost', '--non-interactive', '-o', './aspire-export/all-resources.zip'],
      { stdio: 'inherit' }
    );
  } catch (error) {
    console.error('aspire export failed (non-fatal):', error);
  }
}
