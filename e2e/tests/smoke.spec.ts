import { expect, test } from '@playwright/test';

test('login page renders for unauthenticated users', async ({ page }) => {
  await page.goto('/login');

  await expect(page).toHaveTitle('Sign in');
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
});
