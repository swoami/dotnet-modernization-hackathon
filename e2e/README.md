# ContosoInsurance E2E

Standalone Playwright smoke-test scaffold for the deployed ContosoInsurance Claims Portal.

## Setup

```bash
npm install
npx playwright install chromium --with-deps
```

On Windows, if `--with-deps` is not supported or fails, use:

```bash
npx playwright install chromium
```

## Running tests

Run against the default deployed environment:

```bash
npm test
```

Point to a different environment:

```bash
E2E_BASE_URL=https://your-app.example.com npm test
```

PowerShell equivalent:

```powershell
$env:E2E_BASE_URL='https://your-app.example.com'; npm test
```

## Overriding credentials

The shared login helper defaults to:

- `E2E_USERNAME=admin`
- `E2E_PASSWORD=Password1`

Override them with environment variables before running tests.
