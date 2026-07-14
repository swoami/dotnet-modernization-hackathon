# .NET Version Upgrade Plan: net461 → net9.0

## Target Framework
net9.0

## Strategy
Direct upgrade — convert both projects to SDK-style targeting net9.0 in one pass.

## Tasks

## Task 001-upgrade-dotnet-to-net9
**Description**: Convert ContosoInsurance.Worker and ContosoInsurance.Common from legacy non-SDK .csproj files targeting .NET Framework 4.6.1 to SDK-style .csproj files targeting net9.0. Remove packages.config, Properties/AssemblyInfo.cs, and legacy assembly references (System.ServiceProcess, System.Configuration.Install) incompatible with .NET 9.

**Scope**:
- ContosoInsurance.Common: rewrite csproj to SDK-style, add PackageReferences for log4net and Newtonsoft.Json and System.Configuration.ConfigurationManager, delete packages.config and Properties/AssemblyInfo.cs
- ContosoInsurance.Worker: rewrite csproj to SDK-style, add PackageReference for System.ServiceProcess.ServiceController, remove System.ServiceProcess and System.Configuration.Install references, delete packages.config, Properties/AssemblyInfo.cs, and ProjectInstaller.cs

**Dependencies**: none

**Success Criteria**: passBuild=true, passUnitTests=true (no unit tests to run)
