import { readFileSync } from 'node:fs';
import { expect, test, type Page } from '@playwright/test';
import { login } from './support/auth';

const authTimeout = 15_000;
const uploadTimeout = 30_000;
const sampleFilePath = `${process.cwd()}\\fixtures\\sample-claim-doc.txt`;
const sampleFileBuffer = readFileSync(sampleFilePath);
const expectedPolicies = ['POL-1001', 'POL-1002', 'POL-1003', 'POL-1004', 'POL-1005'];
const accounts = [
  { username: 'agent1', role: 'Agent', claimId: 2 },
  { username: 'adjuster', role: 'Adjuster', claimId: 3 },
  { username: 'admin', role: 'Admin', claimId: 4 }
] as const;

async function expectSharedAuthenticatedNav(page: Page, username: string) {
  await expect(page.getByRole('link', { name: 'Claims', exact: true })).toBeVisible({
    timeout: authTimeout
  });
  await expect(page.getByRole('link', { name: 'Upload', exact: true })).toBeVisible({
    timeout: authTimeout
  });
  await expect(page.getByRole('link', { name: 'Logout', exact: true })).toBeVisible({
    timeout: authTimeout
  });
  await expect(page.getByText(`Signed in as ${username}`)).toBeVisible({ timeout: authTimeout });
}

for (const { username, role, claimId } of accounts) {
  test(`${role} account has the same authenticated claims and upload access`, async ({ page }) => {
    const uploadFileName = `${username}-claim-${claimId}-${Date.now()}.txt`;

    await page.goto('/login');

    await login(page, username, 'Password1');
    await page.waitForURL(url => url.pathname === '/', { timeout: authTimeout });
    await expect(page.getByRole('heading', { name: 'Recent Claims' })).toBeVisible({
      timeout: authTimeout
    });

    // Update this expectation if role-based navigation or authorization is added later.
    await expectSharedAuthenticatedNav(page, username);

    await expect(page.locator('table')).toBeVisible({ timeout: authTimeout });
    await expect(page.locator('table tbody tr')).toHaveCount(5, { timeout: authTimeout });

    for (const policyNumber of expectedPolicies) {
      await expect(page.getByRole('cell', { name: policyNumber })).toBeVisible({
        timeout: authTimeout
      });
    }

    await expect(page.getByRole('link', { name: 'Upload', exact: true })).toHaveAttribute(
      'href',
      '/upload'
    );
    await page.goto('/upload');
    await page.waitForURL(url => url.pathname === '/upload', { timeout: authTimeout });
    await expect(page.getByRole('heading', { name: 'Upload claim document' })).toBeVisible({
      timeout: authTimeout
    });

    await expectSharedAuthenticatedNav(page, username);
    await page.getByLabel('Claim ID:').fill(String(claimId));
    await page.getByLabel('File:').setInputFiles({
      name: uploadFileName,
      mimeType: 'text/plain',
      buffer: sampleFileBuffer
    });
    await page.getByRole('button', { name: 'Upload' }).click();

    await expect(page.getByText(`Uploaded ${uploadFileName} for claim ${claimId}.`)).toBeVisible({
      timeout: uploadTimeout
    });

    await page.getByRole('link', { name: 'Logout', exact: true }).click();
    await page.waitForURL(url => url.pathname === '/login', { timeout: authTimeout });
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible({
      timeout: authTimeout
    });

    await page.goto('/');
    await page.waitForURL(url => url.pathname === '/login', { timeout: authTimeout });
  });
}
