import type { Page } from '@playwright/test';

export async function login(
  page: Page,
  username = process.env.E2E_USERNAME || 'admin',
  password = process.env.E2E_PASSWORD || 'Password1'
) {
  const isLoginPage = (() => {
    try {
      return new URL(page.url()).pathname.startsWith('/login');
    } catch {
      return false;
    }
  })();

  if (!isLoginPage) {
    await page.goto('/login');
  }

  await page.getByLabel('Username').fill(username);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(url => !url.pathname.startsWith('/login'));
}
