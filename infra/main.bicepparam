using 'main.bicep'

// azd populates these from environment values (azd env set / .azure/<env>/.env).
// For a plain `az deployment group create`, override with -p environmentName=... etc.
//
// Deployment target: this stack deploys into the shared, pre-existing resource
// group `rg-swo-gh-hackathon-team3` (subscription 3b77307b-4382-43d7-8075-7704bf73196f).
// main.bicep's targetScope stays 'resourceGroup' (it does not create/manage the RG
// itself) -- azd must be pointed at that RG via `azd env set AZURE_RESOURCE_GROUP
// rg-swo-gh-hackathon-team3` (and `AZURE_SUBSCRIPTION_ID`) before provisioning.
// Suggested azd environment name: contoso-insurance-team3.
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'contoso-insurance-team3')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')
param deployServicesApp = true
