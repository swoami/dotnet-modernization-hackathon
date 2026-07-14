# Deploy guide — Track C

## Overview

This stack deploys the ContosoInsurance platform into the fixed, pre-existing resource group `rg-swo-gh-hackathon-team3` in subscription `3b77307b-4382-43d7-8075-7704bf73196f`. The exact resource definitions live in [`../infra/main.bicep`](../infra/main.bicep) and [`../infra/modules/`](../infra/modules/): Log Analytics, Application Insights, a Container Apps environment, a user-assigned managed identity, Azure Container Registry, Key Vault, Storage (with `claim-docs` and `claim-exports`), Azure SQL Server + `ContosoInsurance` database, and Container Apps for `web`, optional `services` (`deployServicesApp=true`), and `worker`. `infra/main.bicep` is `targetScope = 'resourceGroup'`, so it deploys into that existing RG; it does not create the RG. SQL is Azure AD-only, and the app connection string is stored as the Key Vault secret `sql-connection-string`, then referenced from Container Apps via a secret + `secretRef` rather than a plaintext env var.

- **Track A** owns the Web + Services app code and their Dockerfiles (`track/a-web-api`).
- **Track B** owns the Worker app code and its Dockerfile (`track/b-worker-storage`).
- **Track C** owns Azure infrastructure (`infra/`) and the deploy workflow (`.github/workflows/deploy.yml`).

## Prerequisites

- Azure CLI (`az`)
- Azure Developer CLI (`azd`)
- Access to the `softwareone.com` tenant and subscription `3b77307b-4382-43d7-8075-7704bf73196f`

```text
az login --tenant softwareone.com
```

```text
azd auth login
```

## First-time setup (local, manual)

Run local setup from `src/ContosoInsurance`. The suggested azd environment name is `contoso-insurance-team3`, which matches the fallback in `infra/main.bicepparam`, but the live environment name still comes from `AZURE_ENV_NAME`.

```text
cd src/ContosoInsurance
```

```text
azd env new contoso-insurance-team3 --location <region> --subscription 3b77307b-4382-43d7-8075-7704bf73196f --no-prompt
```

```text
azd env set AZURE_RESOURCE_GROUP rg-swo-gh-hackathon-team3
```

```text
azd env set AZURE_SUBSCRIPTION_ID 3b77307b-4382-43d7-8075-7704bf73196f
```

```text
azd up
```

`azd up` is the one-shot path: it runs infrastructure provisioning and app deployment together. If you want to split that flow:

```text
azd provision
```

```text
azd deploy
```

## azure.yaml service coverage

Today `src/ContosoInsurance/azure.yaml` defines only one service:

- `contosoinsurance-worker`
  - `project: ContosoInsurance.Worker`
  - `host: containerapp`
  - `language: dotnet`
  - resource port `8080`

**Known gap:** `contosoinsurance-web` and `contosoinsurance-services` are not defined in `azure.yaml` right now. Until those entries are added, a full `azd deploy` cannot build/push/deploy all three apps from azd.

What still needs to be added:

- `contosoinsurance-web` with `project: ContosoInsurance.Web`, `host: containerapp`, and Docker settings that use the repo root as build context (`../..` from `src/ContosoInsurance/`) with `ContosoInsurance.Web/Dockerfile`.
- `contosoinsurance-services` with `project: ContosoInsurance.Services`, `host: containerapp`, and Docker settings that use the repo root as build context (`../..` from `src/ContosoInsurance/`) with `ContosoInsurance.Services/Dockerfile`.

The checked-in Dockerfiles explicitly require repo-root build context because they copy sibling projects during `dotnet restore` / `dotnet publish`.

## CI/CD (GitHub Actions)

`.github/workflows/deploy.yml` runs on:

- push to `main`
- `workflow_dispatch`

Workflow shape:

1. Check out the repo
2. Install .NET 9 SDK
3. Run `dotnet build` on `src/ContosoInsurance/ContosoInsurance.sln`
4. Run `dotnet test` on the same solution
5. Log into Azure with `azure/login@v2` using OIDC (`id-token: write`; no client secret)
6. Install `azd`
7. Select or create the azd environment, then set `AZURE_LOCATION`, `AZURE_RESOURCE_GROUP`, and `AZURE_SUBSCRIPTION_ID`
8. Run `azd provision --no-prompt` only when `workflow_dispatch` is used with `provision: true`
9. Run `azd deploy --no-prompt`

Required **Settings → Secrets and variables → Actions** entries:

Secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Variables:

- `AZURE_ENV_NAME` (suggested value: `contoso-insurance-team3`)
- `AZURE_LOCATION` (set this to the region for the shared RG)
- `AZURE_RESOURCE_GROUP` (expected value: `rg-swo-gh-hackathon-team3`)

Use the manual `provision` input for the first infrastructure bootstrap or an explicit infra reprovision. Regular pushes to `main` are set up to skip provisioning and just run `azd deploy`.

## Redeploy / update flows

**Fast path: app-only changes**

```text
cd src/ContosoInsurance
azd deploy
```

Use this for normal code/image refreshes. It is the intended day-to-day path once the base infrastructure already exists.

**Infra-only changes**

```text
cd src/ContosoInsurance
azd provision
```

Use this after editing `infra/main.bicep`, `infra/main.bicepparam`, or any file under `infra/modules/`.

**Provision + deploy together**

```text
cd src/ContosoInsurance
azd up
```

Use this when you want both steps in one command.

**Rollback to an older image**

List available tags in ACR:

```text
az acr repository show-tags --name <acrName> --repository <repository> --output table
```

Then point the Container App back at a known-good image tag (repeat per app as needed; `services` only applies when `deployServicesApp=true`):

```text
az containerapp update --name contosoinsurance-web --resource-group rg-swo-gh-hackathon-team3 --image <acrLoginServer>/<repository>:<tag>
```

```text
az containerapp update --name contosoinsurance-services --resource-group rg-swo-gh-hackathon-team3 --image <acrLoginServer>/<repository>:<tag>
```

```text
az containerapp update --name contosoinsurance-worker --resource-group rg-swo-gh-hackathon-team3 --image <acrLoginServer>/<repository>:<tag>
```

**Full teardown / rebuild (destructive)**

```text
cd src/ContosoInsurance
azd down
```

This is a last resort. Because this stack targets the shared team resource group, treat `azd down` as high-blast-radius and only use it with explicit team awareness; it deletes deployed resources before you re-provision them.

## Post-deploy verification

**Inspect azd outputs and app endpoints**

```text
cd src/ContosoInsurance
azd show
```

`azd show` should surface outputs such as `WEB_URI`, `SERVICES_URI` (when `deployServicesApp=true`), registry info, Key Vault info, SQL info, and the worker app name. `web` is the only external ingress by default; `services` is internal-only on port `8080`, and `worker` has no ingress and stays at `minReplicas: 1`.

```text
curl https://<web-fqdn>
```

**Observability**

Open the Azure portal and check Application Insights Live Metrics for the deployed app workload.

**SQL AAD-only admin**

```text
az sql server ad-admin list --resource-group rg-swo-gh-hackathon-team3 --server-name <sqlServerName>
```

Confirm that the platform user-assigned managed identity is the server's Azure AD admin and that SQL password auth is not part of this setup. The database name should be `ContosoInsurance`.

**Role assignments for the managed identity**

```text
az role assignment list --assignee <managedIdentityPrincipalId> --all --output table
```

From the checked-in Bicep, this should show the three Azure RBAC assignments created by `infra/modules/rbac.bicep`:

- Storage Blob Data Contributor
- Key Vault Secrets User
- AcrPull

SQL server Azure AD admin is verified separately by the `az sql server ad-admin list` command above; it is not created by `rbac.bicep`.

**Key Vault secret presence**

```text
az keyvault secret show --vault-name <kvName> --name sql-connection-string --query id --output tsv
```

This confirms that the secret exists and that your identity can resolve it without printing the connection string value. The Container Apps module wires that secret into the apps via a `secrets` entry (`keyVaultUrl` + `identity`) and a `secretRef` on the env var:

- Key Vault secret name: `sql-connection-string`
- Container Apps secret name: `sql-connection-string`
- App env var name: `ConnectionStrings__ContosoDb`

## Known gaps / follow-ups

- `azure.yaml` still only defines `contosoinsurance-worker`; `contosoinsurance-web` and `contosoinsurance-services` still need to be added before a full azd-driven deploy can build/push all three images.
- No live deployment has been run yet in this environment; this guide is based on the checked-in Bicep, azd, and workflow files and should be re-checked after the first real deploy.
- The GitHub Actions OIDC path assumes the Azure side federated credential is already configured for this GitHub repo/environment; without that precondition, `azure/login@v2` cannot authenticate even if the repo secrets/variables are present.
