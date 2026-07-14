// Creates the Track C Key Vault consumed by platform-managed workloads; see docs/task-briefs.md for the related Blob containers where claim uploads (`claim-docs`) and worker exports (`claim-exports`) land.
param name string
param location string
param tags object = {}
param tenantId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Acceptable for the one-day hackathon; production should use Disabled plus a private endpoint.
    publicNetworkAccess: 'Enabled'
  }
}

output id string = keyVault.id
output uri string = keyVault.properties.vaultUri
output name string = keyVault.name
