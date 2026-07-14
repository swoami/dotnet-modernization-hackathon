import { expect, test } from '@playwright/test';
import { login } from './support/auth';

const expectedHeaders = [
  'ClaimId',
  'Policy.PolicyNumber',
  'ClaimantName',
  'Amount',
  'Status',
  'FiledOn',
  'Score'
];

const expectedPolicies = ['POL-1001', 'POL-1002', 'POL-1003', 'POL-1004', 'POL-1005'];
const expectedClaimants = ['Alice Johnson', 'Bob Smith', 'Carol Diaz', 'David Nguyen', 'Eve Patel'];
const dollarAmountPattern = /^\$[0-9,]+\.\d{2}$/;

test.beforeEach(async ({ page }) => {
  await login(page);
  await expect(page.getByRole('heading', { name: 'Recent Claims' })).toBeVisible({ timeout: 15_000 });
  await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });
});

test('recent claims page shows seeded claims, valid currency formatting, and upload navigation', async ({
  page
}) => {
  await expect(page.locator('thead th')).toHaveText(expectedHeaders, { timeout: 15_000 });

  for (const policyNumber of expectedPolicies) {
    await expect(page.getByRole('cell', { name: policyNumber })).toBeVisible({ timeout: 15_000 });
  }

  for (const claimantName of expectedClaimants) {
    await expect(page.getByRole('cell', { name: claimantName })).toBeVisible({ timeout: 15_000 });
  }

  const amountTexts = await page.locator('table tbody tr td:nth-child(4)').allTextContents();
  expect(amountTexts).toHaveLength(5);
  for (const amountText of amountTexts) {
    expect(amountText).toMatch(dollarAmountPattern);
    expect(amountText).not.toContain('¤');
  }

  await expect(page.getByRole('link', { name: 'Claims', exact: true })).toHaveAttribute('href', '/');
  await expect(page.getByRole('link', { name: 'Upload', exact: true })).toHaveAttribute('href', '/upload');

  await page.getByRole('link', { name: 'Upload', exact: true }).click();
  await expect(page).toHaveURL(/\/upload$/);

  await page.goto('/');
  await expect(page.locator('table')).toBeVisible({ timeout: 15_000 });

  await expect(page.getByRole('link', { name: 'Upload a claim document' })).toHaveAttribute('href', '/upload');
  await page.getByRole('link', { name: 'Upload a claim document' }).click();
  await expect(page).toHaveURL(/\/upload$/);
});
