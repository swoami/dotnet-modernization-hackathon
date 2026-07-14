import { expect, test, type Page } from '@playwright/test';
import { login } from './support/auth';

const authTimeout = 15_000;

async function expectAuthenticatedHome(page: Page) {
  await expect(page.getByRole('heading', { name: 'Recent Claims' })).toBeVisible({ timeout: authTimeout });
  await expect(page.getByText('Signed in as admin')).toBeVisible({ timeout: authTimeout });
}

test('unauthenticated users are redirected to login with a returnUrl', async ({ page }) => {
  await page.goto('/');
  await page.waitForURL(url => {
    return (
      url.pathname === '/login' &&
      (url.searchParams.has('returnUrl') || url.searchParams.has('ReturnUrl'))
    );
  }, { timeout: authTimeout });

  const redirectedUrl = new URL(page.url());
  expect(redirectedUrl.pathname).toBe('/login');
  expect(
    redirectedUrl.searchParams.get('returnUrl') ?? redirectedUrl.searchParams.get('ReturnUrl')
  ).toBeTruthy();
});

test('valid credentials sign the user in', async ({ page }) => {
  await page.goto('/login');

  await login(page);

  await page.waitForURL(url => url.pathname === '/', { timeout: authTimeout });
  await expectAuthenticatedHome(page);
});

test('invalid credentials show an error and keep the user on login', async ({ page }) => {
  await page.goto('/login');

  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('WrongPassword1!');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await page.waitForURL(url => url.pathname === '/login' && url.searchParams.get('error') === '1', {
    timeout: authTimeout
  });
  await expect(page.getByText('Invalid credentials.')).toBeVisible({ timeout: authTimeout });

  const currentUrl = new URL(page.url());
  expect(currentUrl.pathname).toBe('/login');
  expect(currentUrl.searchParams.get('error')).toBe('1');
});

test('logout ends the authenticated session', async ({ page }) => {
  await page.goto('/login');

  await login(page);
  await expectAuthenticatedHome(page);

  await page.getByRole('link', { name: 'Logout' }).click();
  await page.waitForURL(url => url.pathname === '/login', { timeout: authTimeout });
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible({ timeout: authTimeout });

  await page.goto('/');
  await page.waitForURL(url => {
    return (
      url.pathname === '/login' &&
      (url.searchParams.has('returnUrl') || url.searchParams.has('ReturnUrl'))
    );
  }, { timeout: authTimeout });
});

test('session persists across reloads', async ({ page }) => {
  await page.goto('/login');

  await login(page);
  await expectAuthenticatedHome(page);

  await page.reload();
  await page.waitForURL(url => url.pathname === '/', { timeout: authTimeout });
  await expectAuthenticatedHome(page);
});
