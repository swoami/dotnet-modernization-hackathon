# Scenario Instructions — dotnet-version-upgrade

## Scenario Parameters

- **Scenario**: dotnet-version-upgrade (includes SDK-style conversion)
- **Solution**: `src/ContosoInsurance/ContosoInsurance.sln`
- **Target framework**: `net9.0` (STS, EOL 2026-11-10 — explicitly chosen by user)
- **Projects (5)**: Common, Data, Services, Web, Worker

## Strategy

**Selected**: Bottom-Up (Dependency-First)
**Rationale**: 5 .NET Framework projects, 4-tier dependency graph (Common → Data → Services/Worker → Web), all legacy non-SDK csproj.

### Execution Constraints

- Strict tier ordering: Common → Data → Services + Worker → Web; complete and validate each tier before the next.
- Reduced-scope validation: `dotnet restore` must succeed for every converted project; compile success required only for Common and Data (no unsupported APIs there).
- Services, Worker, Web: build failures caused by System.Web / WCF hosting / ServiceBase are ACCEPTED known limitations — do not fix code.
- Convert packages.config → PackageReference during conversion; add only packages strictly required by the conversion itself.
- Commit after each task on branch `track/a-web-api`.

## Preferences

- **Commit Strategy**: After Each Task

### Flow Mode

- **Automatic** — run end-to-end, only pause when blocked.

### Source Control

- **Use the current branch (`track/a-web-api`) — do NOT create a new working branch.** Commit changes on the same branch.

### Technical Preferences

- Convert all `.csproj` files to SDK-style project format.
- Retarget all projects to `net9.0`.
- Fix all build warnings (treat warnings as errors); never suppress.

### Scope Limitation (user-confirmed 2026-07-14)

- **Scope is limited to converting the 5 `.csproj` files to SDK-style format with `<TargetFramework>net9.0</TargetFramework>`.**
- Convert `packages.config` → `PackageReference` where present.
- **NO code rewrites**: no Web Forms → Razor Pages, no CoreWCF migration, no Windows Service → BackgroundService, no broad package modernization. Only changes strictly required for the csproj conversion itself.
- **It is accepted that the solution will NOT fully build** afterward due to unsupported .NET Framework APIs (Web Forms, WCF server hosting, ServiceBase, etc.). Attempt a build; report those failures as known limitations — do NOT fix code.
- The warning-free build goal is superseded by this scope limitation for projects that cannot build.

## Decisions

- 2026-07-14: User rejected creation of a new branch; work happens on `track/a-web-api` with commits on that branch.
- 2026-07-14: **Scope change** — user said "let's only upgrade csproj for now". Overrides earlier selections in upgrade-options.md: "Rewrite to modern equivalents" is deferred; csproj-only conversion is the confirmed scope. Build success is not a completion criterion.
