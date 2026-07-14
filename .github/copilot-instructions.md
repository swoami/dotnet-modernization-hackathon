# Copilot instructions — Legacy Breakers modernization hackathon

Context for any Copilot session (chat, agent, or CLI) working in this repo.
This is `ContosoInsurance`: a real legacy .NET Framework 4.6.1 solution
(WebForms + WCF + Windows Service + ADO.NET + SQL Server) being modernized to
a **.NET 9 + Azure Container Apps** stack in one day by three parallel tracks,
using the GitHub Copilot app modernization tooling, the Modernization CLI
(`appcat` / `dotnet-appmod`), `azd`, and regular Copilot Chat/Agent mode. Full
background: `docs/pre-read.md`, `docs/task-briefs.md`,
`docs/facilitator-guide.md`, `docs/rubric.md`.

The legacy app does **not** need to build locally to be assessed — the
modernization tools work from source. Nothing in this repo should require
Visual Studio, SQL Server, or IIS to be installed locally.

## The two extensions, chained in order

Two separate installs make up "GitHub Copilot app modernization for .NET":

1. **Upgrade for .NET** — .NET Fx → .NET (n). Handles project file conversion,
   NuGet migration, API surface changes, `Web.config` → `appsettings.json`,
   WebForms/WCF transformations.
2. **Azure migration for .NET** — code- and infra-level cloud readiness:
   Managed Identity, Key Vault, Blob Storage, App Insights, containerization,
   Bicep generation, GitHub Actions.

Always run **Upgrade first, then Azure migration** — don't reach for
Azure-migration changes (Managed Identity, Blob, containerization, Bicep)
before the corresponding upgrade task has landed.

Each extension's core loop: **Assess** (scan the solution, produce a grouped
findings/plan: framework upgrade, package/CVE issues, config, data, hosting,
cloud readiness) then **Apply** (execute one task at a time — read the plan,
propose a diff, open it for review, move to the next task).

## Commit discipline (read this first)

- **One task = one commit.** If a proposed diff spans more than one concern
  (e.g. an SDK-style project conversion *and* a config change *and* a package
  bump), split it into separate commits/diffs. Push back on any change that
  bundles unrelated work.
- **Small, reviewable diffs.** Prefer several small commits over one large one.
  Push often to the feature branch — don't hoard local commits.
- **Commit messages reference the concrete task or concern**, ideally
  Conventional Commits style, e.g.:
  `chore(common): migrate to SDK-style project targeting net9.0 (appmod)`
  A commit message like "fixes" or "wip" is not acceptable — say what task or
  concern it addresses.
- **Never rewrite git history** on a shared branch once someone else may have
  branched from it or pulled it.

## Workflow loop (per track)

1. Run **Assess** (GitHub Copilot app modernization extension) scoped to the
   track's folder(s) — not the whole repo unless intentional.
2. Review the generated plan before executing anything.
3. Execute tasks **one at a time**, reviewing every diff.
4. Ask Copilot for the **plan first**, then say "apply that" — don't let it
   jump straight to a multi-file rewrite. This avoids silent rewrites.
5. Commit after each accepted task (see commit discipline above); push.
6. Run `dotnet build` after every accepted diff; `dotnet test` after every
   meaningful accept if tests exist. Don't accept an unverified build.
7. Fall back to Copilot Chat / Agent mode only for what the extension can't
   cover (one-off refactors, glue code). Hand-editing is a last resort — note
   it in `docs/learnings.md` when it happens.
8. **Show context explicitly.** Open the relevant file(s) rather than relying
   on active-editor pickup, especially when re-issuing a prompt after a wrong
   answer.

## Ground rules

- **Copilot-first.** Use the modernization extension for anything it
  supports. Fall back to Copilot Chat / Agent mode for freeform code changes.
  Hand-editing is a last resort — and note it in `docs/learnings.md`.
- **Diffs are reviewed.** Every agent-generated diff gets a real look before
  accept. If Copilot produces something wrong, capture the prompt + output in
  `docs/learnings.md`.
- **Small commits.** One task = one commit ideally. Push often to the
  feature branch.
- **Sync at every checkpoint.** Rebase / merge `main` at the times listed on
  the agenda — don't let a track drift for hours without syncing.
- **Ask before rewriting.** Don't rewrite another track's project without
  confirming with the pair that owns it.
- **Time-box.** Stretch goals (WebForms → Blazor, WCF → CoreWCF, Entra ID
  auth) only after the core "done" checklist for the track is green.

## Branch & ownership rules

| Track | Pair | Owns (folders) |
| --- | --- | --- |
| **A — Web + API tier** | 2 people | `ContosoInsurance.Web/`, `ContosoInsurance.Services/`, `ContosoInsurance.Data/` (jointly with B) |
| **B — Worker + Storage** | 2 people | `ContosoInsurance.Worker/`, Blob wiring, `ContosoInsurance.Data/` (shared) |
| **C — Platform** | 2 people | Containerization, Bicep, `azd`, Managed Identity, Key Vault, App Insights, GitHub Actions |

- Tracks work on `track/a-web-api`, `track/b-worker-storage`,
  `track/c-platform`. Rebase against `main` at each checkpoint; never rebase
  a branch after someone else has branched from it.
- `integration` branch is used at Checkpoint 2 to merge all three tracks;
  `main` receives `integration` after a successful smoke test.
- **Ask before rewriting another track's project.** `ContosoInsurance.Common`
  and `ContosoInsurance.Data` are shared:
  - `Common` — Track A owns the `log4net` → `ILogger` swap (lands first);
    Tracks B and C only consume the resulting `AddContosoLogging()` extension.
  - `Data` — Tracks A and B jointly own `ContosoDbContext`. Track A owns
    `DbSet<Claim>`, `DbSet<Policy>`, `DbSet<User>` + repository rewrites.
    Track B only adds `DbSet<ExportLog>` + its migration. Never generate a
    second, competing `DbContext`.
- `ContosoInsurance.Services` may end up merged into `Web` by Track A — check
  before assuming it's still a separate deployable (e.g. before writing a
  Dockerfile or Container App resource for it).

## Non-negotiables (hard fails in the rubric)

- **No secrets, ever.** No password, connection string with credentials, or
  storage key committed anywhere — not in Bicep params, `azure.yaml`,
  GitHub Actions, or app config. Use Managed Identity / `DefaultAzureCredential`
  and Key Vault references instead.
- **No hard-coded local paths.** Nothing should require `C:\ClaimsFiles`,
  `C:\Exports`, or any other `C:\` path to function — those become Azure Blob
  Storage.
- **Every agent-generated diff is reviewed before accepting** — don't accept
  blind. Spot checks happen at the retro.
- `azd up` must work end-to-end at demo time — don't leave infra half-wired.

## What "done" looks like (target state)

| Concern | Before | After |
| --- | --- | --- |
| Framework | .NET Fx 4.6.1 | .NET 9, SDK-style `.csproj` |
| Packages | `packages.config`, `log4net`, old `Newtonsoft.Json` | `PackageReference`, no known CVEs, `log4net` removed |
| Config | `Web.config` / `App.config` | `appsettings.json` + `IConfiguration` / strongly-typed options |
| Data access | Raw ADO.NET | EF Core 9 (`ContosoDbContext`) |
| Logging | `log4net` + `Trace.*` | `ILogger<T>` + Application Insights |
| Local files | `C:\ClaimsFiles`, `C:\Exports` | Azure Blob Storage (`claim-docs`, `claim-exports`) |
| Secrets | Plain SQL user/password | Managed Identity + Key Vault |
| Web hosting | IIS + WebForms | Kestrel + ASP.NET Core (Razor Pages, stretch: Blazor) |
| Worker hosting | Windows Service (`ServiceBase`) | `BackgroundService` on the Generic Host |
| WCF SOAP | `System.ServiceModel` | ASP.NET Core minimal API (stretch: CoreWCF) |
| Container | none | Dockerfile per deployable |
| Infra | manual | Bicep via `azd` |
| CI/CD | none | GitHub Actions (OIDC, no long-lived secrets) |

## Docs to keep in sync

- Capture prompt/response lessons (what worked, what needed a nudge, gotchas,
  follow-ups) in `docs/learnings.md` as you go — don't leave it for the end.
- `docs/README-DEPLOY.md` (Track C) must document `azd up` / rollback /
  redeploy steps once infra lands.
- `docs/tracker-status.js` is generated by `docs/scan-progress.ps1` on every
  push to `main` (or manual dispatch) — never hand-edit it or commit it
  (it's git-ignored by design).
