import { expect, test, type Page } from '@playwright/test';
import { login } from './support/auth';

const authTimeout = 15_000;
const uploadTimeout = 30_000;
const uiSettleDelayMs = 250;
const roundtripFileName = 'roundtrip-doc.txt';
const roundtripFilePath = `${process.cwd()}\\fixtures\\${roundtripFileName}`;
const historicalCrashSnippets = [
  'circuit will be terminated',
  'No interop methods are registered',
  'Unhandled exception'
];

const seededClaims = [
  { id: '1', policy: 'POL-1001', claimant: 'Alice Johnson' },
  { id: '2', policy: 'POL-1002', claimant: 'Bob Smith' },
  { id: '3', policy: 'POL-1003', claimant: 'Carol Diaz' },
  { id: '4', policy: 'POL-1004', claimant: 'David Nguyen' },
  { id: '5', policy: 'POL-1005', claimant: 'Eve Patel' }
] as const;

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

async function expectNoRegressionIssues(issues: string[]) {
  await expect(async () => {
    expect(issues).toEqual([]);
  }).toPass({ timeout: 5_000 });
}

async function uploadDocument(page: Page, claimId: number) {
  await page.getByRole('spinbutton', { name: 'Claim ID:' }).fill(String(claimId));
  await page.getByLabel('File:').setInputFiles(roundtripFilePath);
  await page.getByRole('button', { name: 'Upload' }).click();
  await page.waitForTimeout(uiSettleDelayMs);
  await expect(page.getByText(`Uploaded ${roundtripFileName} for claim ${claimId}.`)).toBeVisible({
    timeout: uploadTimeout
  });
}

test('re-uploading the same claim document keeps the app functional and preserves seeded claims', async ({
  page
}) => {
  await login(page);
  const regressionIssues = attachRegressionGuards(page);

  await page.goto('/upload');
  await expect(page.getByRole('heading', { name: 'Upload claim document' })).toBeVisible({
    timeout: authTimeout
  });

  await uploadDocument(page, 5);
  await expectNoRegressionIssues(regressionIssues);

  await uploadDocument(page, 5);
  await expectNoRegressionIssues(regressionIssues);

  await page.getByRole('link', { name: 'Claims', exact: true }).click();
  await page.waitForURL(url => url.pathname === '/', { timeout: authTimeout });
  await page.waitForTimeout(uiSettleDelayMs);

  await expect(page.getByRole('heading', { name: 'Recent Claims' })).toBeVisible({
    timeout: authTimeout
  });
  await expect(page.getByRole('table')).toBeVisible({ timeout: authTimeout });
  await expect(page.getByRole('table')).not.toContainText('¤');
  await expect(page.getByRole('row')).toHaveCount(6, { timeout: authTimeout });

  for (const claim of seededClaims) {
    await expect(page.getByRole('cell', { name: claim.id, exact: true })).toBeVisible({
      timeout: authTimeout
    });
    await expect(page.getByRole('cell', { name: claim.policy, exact: true })).toBeVisible({
      timeout: authTimeout
    });
    await expect(page.getByRole('cell', { name: claim.claimant, exact: true })).toBeVisible({
      timeout: authTimeout
    });
  }

  await expect(
    page.getByRole('row', { name: /5\s+POL-1005\s+Eve Patel\s+\$9,800\.00/i })
  ).toBeVisible({ timeout: authTimeout });
  await expectNoRegressionIssues(regressionIssues);
});
