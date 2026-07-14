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
    // Consumed by ContosoInsurance.Common.Storage.ClaimDocumentStoreExtensions.AddClaimDocumentStore
    // (Web and Worker) to build the BlobServiceClient via Managed Identity.
    name: 'AzureStorage__AccountUri'
    value: 'https://${storageAccountName}.blob.core.windows.net'
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

// Web calls the Services app's claim-scoring endpoint over the Container Apps
// environment's internal DNS. Falls back to localhost only when Services isn't deployed.
var scoringEndpoint = deployServicesApp ? 'https://${services.?properties.?configuration.?ingress.?fqdn}' : 'http://localhost:8080'

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
  // azd matches Container Apps to azure.yaml services by this tag, not by resource name.
  tags: union(tags, { 'azd-service-name': 'contosoinsurance-web' })
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
          env: concat(commonEnv, [
            {
              name: 'AppSettings__ClaimScoringEndpoint'
              value: scoringEndpoint
            }
          ])
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          // Web exposes an anonymous /health endpoint (MapHealthChecks in Program.cs).
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 12
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
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
  tags: union(tags, { 'azd-service-name': 'contosoinsurance-services' })
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
          // Services has no /health endpoint yet, so probe TCP reachability of the ingress port.
          probes: [
            {
              type: 'Startup'
              tcpSocket: {
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
              failureThreshold: 12
            }
            {
              type: 'Liveness'
              tcpSocket: {
                port: 8080
              }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              tcpSocket: {
                port: 8080
              }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
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
  tags: union(tags, { 'azd-service-name': 'contosoinsurance-worker' })
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
          // The worker registers health checks (DbContext + blob container) but hosts no HTTP
          // endpoint and has no ingress, so no HTTP/TCP probe target exists. Add probes here
          // once the worker exposes a /health listener.
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
