# .NET Version Upgrade Progress

## Overview

Converting the 5 ContosoInsurance projects (Common, Data, Services, Web, Worker) from legacy .NET Framework 4.6.1 csproj format to SDK-style projects targeting net9.0, bottom-up through the dependency graph. Scope is csproj conversion only — no code rewrites; build failures from unsupported Framework APIs are accepted known limitations.

**Progress**: 5/5 tasks complete <progress value="100" max="100"></progress> 100%

## Tasks

- ✅ 01-prerequisites: Verify toolchain and source control state ([Content](tasks/01-prerequisites/task.md), [Progress](tasks/01-prerequisites/progress-details.md))
- ✅ 02-foundation-libs: Convert Common and Data class libraries to SDK-style net9.0 ([Content](tasks/02-foundation-libs/task.md), [Progress](tasks/02-foundation-libs/progress-details.md))
- ✅ 03-service-hosts: Convert Services and Worker projects to SDK-style net9.0 ([Content](tasks/03-service-hosts/task.md), [Progress](tasks/03-service-hosts/progress-details.md))
- ✅ 04-web-app: Convert Web project to SDK-style net9.0 ([Content](tasks/04-web-app/task.md), [Progress](tasks/04-web-app/progress-details.md))
- ✅ 05-final-validation: Attempt solution build and document known limitations ([Content](tasks/05-final-validation/task.md), [Progress](tasks/05-final-validation/progress-details.md))
