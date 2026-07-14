using 'main.bicep'

// azd populates these from environment values (azd env set / .azure/<env>/.env).
// For a plain `az deployment group create`, override with -p environmentName=... etc.
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'contosoinsurance')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')
param deployServicesApp = true
