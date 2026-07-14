// Creates the Track C storage account and Blob containers described in docs/task-briefs.md, where uploads go to `claim-docs` and worker exports go to `claim-exports`.
param name string
param location string
param tags object = {}
param containerNames array = [
  'claim-docs'
  'claim-exports'
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: name
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  tags: tags
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = [for containerName in containerNames: {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}]

output id string = storageAccount.id
output name string = storageAccount.name
output primaryEndpointsBlob string = storageAccount.properties.primaryEndpoints.blob
