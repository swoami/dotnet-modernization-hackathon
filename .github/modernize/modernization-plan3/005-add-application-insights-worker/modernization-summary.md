# Modernization Summary: 005-add-application-insights-worker

- **finalStatus**: success
- **successCriteriaStatus**: { passBuild: true, generateNewUnitTests: false, passUnitTests: true }
- **summary**: Added `Microsoft.ApplicationInsights.WorkerService` (2.23.0) to `ContosoInsurance.Worker.csproj`. Called `AddApplicationInsightsTelemetryWorkerService()` in `Program.cs` reading the connection string from `builder.Configuration["ApplicationInsights:ConnectionString"]`. Added `ApplicationInsights:ConnectionString` placeholder (empty string) to `appsettings.json` — value is populated at runtime from environment variables or Key Vault. No instrumentation key hard-coded anywhere. Build passes.
