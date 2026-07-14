# Modernization Summary: 003-replace-file-write-with-blob-upload

- **finalStatus**: success
- **successCriteriaStatus**: { passBuild: true, generateNewUnitTests: true, passUnitTests: true }
- **summary**: Replaced `ExportRoot` property in `ExportOptions` with `ContainerName` (default `claim-exports`). Rewrote synchronous `Export()` method as `async ExportAsync(CancellationToken)` that builds CSV into a `MemoryStream` and calls `IClaimDocumentStore.UploadAsync`. `Directory.CreateDirectory`, `File.WriteAllText`, and all local path usage removed from the export path. `ClaimsExporterService` now injects `IClaimDocumentStore` via constructor. `Program.cs` updated with `AddClaimDocumentStore()`. `appsettings.json` updated with `AzureStorage:AccountUri` placeholder and `ContainerName`. Build passes.
