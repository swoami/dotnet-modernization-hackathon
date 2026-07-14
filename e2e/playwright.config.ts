import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL:
      process.env.E2E_BASE_URL ||
      'https://contosoinsurance-web.gentlewave-af9468f0.germanywestcentral.azurecontainerapps.io',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
