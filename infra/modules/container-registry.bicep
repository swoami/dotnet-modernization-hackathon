// Creates an Azure Container Registry for platform container images.
param name string
param location string
param tags object = {}
param sku string = 'Basic'
param adminUserEnabled bool = false

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
  }
}

output id string = registry.id
output loginServer string = registry.properties.loginServer
output name string = registry.name
