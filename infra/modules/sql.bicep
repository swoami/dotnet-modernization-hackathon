// Azure SQL module for the hackathon platform track: AAD-only server + single database.

param serverName string
param databaseName string = 'ContosoInsuranceDb'
param location string
param tags object = {}
param tenantId string
param aadAdminPrincipalId string
param aadAdminLogin string
param skuName string = 'GP_S_Gen5_1'
param skuTier string = 'GeneralPurpose'
param skuFamily string = 'Gen5'
param skuCapacity int = 1
param allowAzureServices bool = true

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    // This makes the platform user-assigned managed identity the server's AAD-only admin so Container Apps can reach the database with passwordless auth immediately; it avoids a separate contained-user post-deploy step for the hackathon, but production should prefer a narrower contained database user with only db_datareader/db_datawriter granted by post-deploy SQL.
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: aadAdminLogin
      sid: aadAdminPrincipalId
      tenantId: tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource allowAllWindowsAzureIps 'Microsoft.Sql/servers/firewallRules@2023-08-01' = if (allowAzureServices) {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
    family: skuFamily
    capacity: skuCapacity
  }
}

output sqlServerId string = sqlServer.id
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
