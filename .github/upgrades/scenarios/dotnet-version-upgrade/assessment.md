# .NET Version Upgrade Assessment

## Target Framework
net9.0

## Projects in Scope

### ContosoInsurance.Common
- **Current TFM**: net461 (legacy non-SDK csproj)
- **Output**: Library
- **Packages**: log4net 2.0.8, Newtonsoft.Json 11.0.2 (via packages.config + HintPath references)
- **Legacy references**: System, System.Configuration, System.Core (auto-included in SDK-style)
- **Issues**: packages.config must be removed; Properties/AssemblyInfo.cs must be removed; System.Configuration.ConfigurationManager NuGet needed for ConfigHelper.cs

### ContosoInsurance.Worker
- **Current TFM**: net461 (legacy non-SDK csproj)
- **Output**: Exe (Windows Service)
- **Packages**: (only log4net in packages.config, not referenced in csproj)
- **Legacy references**: System, System.Configuration, System.Configuration.Install, System.Core, System.ServiceProcess
- **Issues**: System.Configuration.Install does not exist on .NET 9 — ProjectInstaller.cs uses it; System.ServiceProcess.ServiceController NuGet needed for ServiceBase; packages.config must be removed; Properties/AssemblyInfo.cs must be removed

## Risks
- **ProjectInstaller.cs** uses `System.Configuration.Install` which has no .NET 9 equivalent → must be removed
- **ConfigHelper.cs** uses `System.Configuration.ConfigurationManager` → need `System.Configuration.ConfigurationManager` NuGet package
- **AppLogger.cs** uses log4net → need `log4net` PackageReference
- **ClaimsExporterService.cs, Program.cs** use `System.ServiceProcess.ServiceBase` → need `System.ServiceProcess.ServiceController` NuGet

## Package Mapping
| Old (packages.config) | New (PackageReference) |
|---|---|
| log4net 2.0.8 | log4net (latest compatible) |
| Newtonsoft.Json 11.0.2 | Newtonsoft.Json (latest compatible) |
| (implicit) System.Configuration | System.Configuration.ConfigurationManager |
| (implicit) System.ServiceProcess | System.ServiceProcess.ServiceController |
