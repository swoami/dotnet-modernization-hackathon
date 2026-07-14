# Topology Graph: dotnet-modernization-hackathon

Analyzed 5 service(s) across 1 repository(ies) for application "dotnet-modernization-hackathon" on 2026-07-14 08:27 UTC.

## Services

```mermaid
flowchart TD
    ciWeb["ContosoInsurance.Web\n(Frontend)"]
    ciServices["ContosoInsurance.Services\n(WebApi)"]
    ciWorker["ContosoInsurance.Worker\n(Worker)"]
    ciData["ContosoInsurance.Data\n(DataService)"]
    ciCommon["ContosoInsurance.Common\n(SharedLibrary)"]

    ciWeb -->|"DirectReference"| ciServices
    ciWeb -->|"DirectReference"| ciData
    ciWeb -->|"DirectReference"| ciCommon
    ciServices -->|"DirectReference"| ciData
    ciServices -->|"DirectReference"| ciCommon
    ciWorker -->|"DirectReference"| ciData
    ciWorker -->|"DirectReference"| ciCommon
    ciData -->|"DirectReference"| ciCommon
```

## Service Details

| Service | Role | Language | Source Repository | Warnings |
|---------|------|----------|-------------------|----------|
| ContosoInsurance.Web | Frontend | dotnet | dotnet-modernization-hackathon.src.ContosoInsurance | — |
| ContosoInsurance.Services | WebApi | dotnet | dotnet-modernization-hackathon.src.ContosoInsurance | — |
| ContosoInsurance.Worker | Worker | dotnet | dotnet-modernization-hackathon.src.ContosoInsurance | — |
| ContosoInsurance.Data | DataService | dotnet | dotnet-modernization-hackathon.src.ContosoInsurance | — |
| ContosoInsurance.Common | SharedLibrary | dotnet | dotnet-modernization-hackathon.src.ContosoInsurance | — |
