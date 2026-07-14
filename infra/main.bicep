// Track C platform provisioning for ContosoInsurance (docs/task-briefs.md).
// Deploy target: an EXISTING shared resource group (this template does NOT
// create/manage the resource group itself). Run with e.g.
//   az deployment group create -g <shared-rg-name> -f infra/main.bicep -p infra/main.bicepparam
// or via `azd provision` / `azd up` once azure.yaml points environment
// config at that resource group.
targetScope = 'resourceGroup'

@minLength(1)
@description('Name of the azd environment; used to derive resource names and as the azd-env-name tag.')
param environmentName string

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Deploy ContosoInsurance.Services as its own Container App. Set to false if Track A merges Services into Web.')
param deployServicesApp bool = true

@description('Container image reference for the web app. azd deploy overrides this after the first `azd up`.')
param imageWeb string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image reference for the services app. azd deploy overrides this after the first `azd up`.')
param imageServices string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image reference for the worker app. azd deploy overrides this after the first `azd up`.')
param imageWorker string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

var tags = {
  'azd-env-name': environmentName
}

// Short, (mostly) globally-unique token for resources that need a
// subscription-wide unique name (ACR, Storage, Key Vault, SQL server).
var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))

var logAnalyticsName = '${environmentName}-law'
var appInsightsName = '${environmentName}-appi'
var containerAppsEnvName = '${environmentName}-cae'
var identityName = '${environmentName}-id'
var acrName = 'acr${resourceToken}'
var keyVaultName = 'kv${resourceToken}'
var storageAccountName = 'st${resourceToken}'
var sqlServerName = 'sql-${resourceToken}'

// ---------- Observability ----------

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights'
  params: {
    name: appInsightsName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// ---------- Identity, registry, secrets, storage ----------

module identity 'modules/managed-identity.bicep' = {
  name: 'identity'
  params: {
    name: identityName
    location: location
    tags: tags
  }
}

module containerRegistry 'modules/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    name: acrName
    location: location
    tags: tags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
    tenantId: subscription().tenantId
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    sqlDatabaseName: sql.outputs.sqlDatabaseName
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: storageAccountName
    location: location
    tags: tags
  }
}

// ---------- Data tier ----------

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    serverName: sqlServerName
    location: location
    tags: tags
    tenantId: subscription().tenantId
    aadAdminPrincipalId: identity.outputs.principalId
    aadAdminLogin: identity.outputs.name
  }
}

// ---------- Container Apps environment ----------
// listKeys() is called here (not inside the module) because it needs to act
// on the Log Analytics workspace's resourceId + api-version directly.
module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    name: containerAppsEnvName
    location: location
    tags: tags
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    // Built from the deterministic name (not the module output) because
    // listKeys() requires an argument calculable at the start of the
    // deployment; a module output resourceId doesn't qualify.
    logAnalyticsSharedKey: listKeys(
      resourceId('Microsoft.OperationalInsights/workspaces', logAnalyticsName),
      '2022-10-01'
    ).primarySharedKey
  }
}

// ---------- RBAC ----------
// Runs after identity/storage/keyVault/containerRegistry all exist, since
// rbac.bicep looks them up via the "existing resource" pattern by name.
module rbac 'modules/rbac.bicep' = {
  name: 'rbac'
  params: {
    principalId: identity.outputs.principalId
    storageAccountName: storage.outputs.name
    keyVaultName: keyVault.outputs.name
    containerRegistryName: containerRegistry.outputs.name
  }
}

// ---------- Container Apps (web, services, worker) ----------
module containerApps 'modules/container-apps.bicep' = {
  name: 'container-apps'
  params: {
    environmentId: containerAppsEnvironment.outputs.id
    location: location
    tags: tags
    userAssignedIdentityId: identity.outputs.id
    userAssignedIdentityClientId: identity.outputs.clientId
    containerRegistryLoginServer: containerRegistry.outputs.loginServer
    imageWeb: imageWeb
    imageServices: imageServices
    imageWorker: imageWorker
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlConnectionStringSecretUri: keyVault.outputs.sqlConnectionStringSecretUri
    storageAccountName: storage.outputs.name
    keyVaultUri: keyVault.outputs.uri
    deployServicesApp: deployServicesApp
  }
  dependsOn: [
    rbac
  ]
}

// ---------- Outputs (consumed by azd / README-DEPLOY.md) ----------
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_RESOURCE_GROUP string = resourceGroup().name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.outputs.connectionString
output AZURE_SQL_SERVER_FQDN string = sql.outputs.sqlServerFqdn
output AZURE_SQL_DATABASE_NAME string = sql.outputs.sqlDatabaseName
output AZURE_STORAGE_ACCOUNT_NAME string = storage.outputs.name
output AZURE_KEY_VAULT_URI string = keyVault.outputs.uri
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output WEB_URI string = 'https://${containerApps.outputs.webFqdn}'
output SERVICES_URI string = deployServicesApp ? 'https://${containerApps.outputs.servicesFqdn}' : ''
output WORKER_APP_NAME string = containerApps.outputs.workerAppName
