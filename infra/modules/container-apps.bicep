// Creates the ContosoInsurance Container Apps for web, services, and worker workloads.
param environmentId string
param location string
param tags object = {}

param userAssignedIdentityId string
param userAssignedIdentityClientId string
param containerRegistryLoginServer string

// azd deploy will override these placeholder image references with the published app images.
param imageWeb string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
// azd deploy will override these placeholder image references with the published app images.
param imageServices string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
// azd deploy will override these placeholder image references with the published app images.
param imageWorker string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

param appInsightsConnectionString string
param sqlConnectionStringSecretUri string
param storageAccountName string
param keyVaultUri string
param deployServicesApp bool = true

var commonEnv = [
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    // Matches ConfigHelper.GetConnectionString("ContosoDb") used by
    // ContosoInsurance.Data (ClaimsRepository/PolicyRepository/UserRepository),
    // consumed by Web, Services, and (once DB-enabled) Worker on track a/b.
    name: 'ConnectionStrings__ContosoDb'
    secretRef: 'sql-connection-string'
  }
  {
    name: 'AZURE_STORAGE_ACCOUNT_NAME'
    value: storageAccountName
  }
  {
    name: 'AZURE_CLIENT_ID'
    value: userAssignedIdentityClientId
  }
  {
    name: 'KEYVAULT_URI'
    value: keyVaultUri
  }
]

var userAssignedIdentity = {
  '${userAssignedIdentityId}': {}
}

var registries = [
  {
    server: containerRegistryLoginServer
    identity: userAssignedIdentityId
  }
]

var secrets = [
  {
    name: 'sql-connection-string'
    keyVaultUrl: sqlConnectionStringSecretUri
    identity: userAssignedIdentityId
  }
]

resource web 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'contosoinsurance-web'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: userAssignedIdentity
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      secrets: secrets
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'web'
          image: imageWeb
          env: commonEnv
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
    }
  }
}

resource services 'Microsoft.App/containerApps@2024-03-01' = if (deployServicesApp) {
  name: 'contosoinsurance-services'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: userAssignedIdentity
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      secrets: secrets
      // Only Web should be public-facing; Services stays internal within the Container Apps environment.
      ingress: {
        external: false
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'services'
          image: imageServices
          env: commonEnv
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'contosoinsurance-worker'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: userAssignedIdentity
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: imageWorker
          env: commonEnv
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      // The worker runs a continuous timer loop, so it must stay warm instead of scaling to zero.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output webFqdn string = web.properties.configuration.ingress.fqdn
output servicesFqdn string = deployServicesApp ? (services.?properties.?configuration.?ingress.?fqdn ?? '') : ''
output workerAppName string = worker.name
