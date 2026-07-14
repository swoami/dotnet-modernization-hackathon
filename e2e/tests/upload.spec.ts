import { expect, test, type Page } from '@playwright/test';
import { login } from './support/auth';

const sampleFilePath = `${process.cwd()}\\fixtures\\sample-claim-doc.txt`;
const maxUploadBytes = 10 * 1024 * 1024;
const historicalCrashSnippets = [
  'circuit will be terminated',
  'No interop methods are registered',
  'Unhandled exception'
];

function attachRegressionGuards(page: Page) {
  const issues: string[] = [];

  page.on('console', message => {
    if (message.type() !== 'error') {
      return;
    }

    const text = message.text();
    if (historicalCrashSnippets.some(snippet => text.includes(snippet))) {
      issues.push(`console: ${text}`);
    }
  });

  page.on('pageerror', error => {
    issues.push(`pageerror: ${error.message}`);
  });

  return issues;
}

async function openUploadPage(page: Page) {
  await login(page);
  await page.goto('/upload');
  await expect(page.getByRole('heading', { name: 'Upload claim document' })).toBeVisible();
}

function fileInput(page: Page) {
  return page.locator('input[type="file"]');
}

test('uploads a claim document without historical circuit crashes', async ({ page }) => {
  await openUploadPage(page);
  const regressionIssues = attachRegressionGuards(page);

  await page.getByLabel('Claim ID:').fill('1');
  await fileInput(page).setInputFiles(sampleFilePath);
  await page.getByRole('button', { name: 'Upload' }).click();

  await expect(async () => {
    expect(regressionIssues).toEqual([]);
  }).toPass({ timeout: 5_000 });

  await expect(page.getByText('Uploaded sample-claim-doc.txt for claim 1.')).toBeVisible({
    timeout: 30_000
  });
});

test('shows an error when claim id is missing', async ({ page }) => {
  await openUploadPage(page);

  await fileInput(page).setInputFiles(sampleFilePath);
  await page.getByRole('button', { name: 'Upload' }).click();

  await expect(page.getByText('Invalid claim ID.')).toBeVisible({ timeout: 10_000 });
});

test('shows an error when no file is selected', async ({ page }) => {
  await openUploadPage(page);

  await page.getByLabel('Claim ID:').fill('1');
  await page.getByRole('button', { name: 'Upload' }).click();

  await expect(page.getByText('No file selected.')).toBeVisible({ timeout: 10_000 });
});

test('shows a not found error for an unknown claim id', async ({ page }) => {
  await openUploadPage(page);

  await page.getByLabel('Claim ID:').fill('999999');
  await fileInput(page).setInputFiles(sampleFilePath);
  await page.getByRole('button', { name: 'Upload' }).click();

  await expect(page.getByText('Claim 999999 not found.')).toBeVisible({ timeout: 30_000 });
});

test('rejects files larger than the configured upload limit', async ({ page }) => {
  await openUploadPage(page);

  await page.getByLabel('Claim ID:').fill('1');
  await fileInput(page).setInputFiles({
    name: 'oversized-claim-doc.txt',
    mimeType: 'text/plain',
    buffer: Buffer.alloc(maxUploadBytes + 1, 'a')
  });
  await page.getByRole('button', { name: 'Upload' }).click();

  await expect(page.getByText('File too large.')).toBeVisible({ timeout: 10_000 });
});
