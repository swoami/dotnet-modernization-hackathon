# Copilot instructions — Legacy Breakers modernization hackathon

Context for any Copilot session (chat, agent, or CLI) working in this repo.
This is `ContosoInsurance`: a .NET Framework 4.6.1 solution (WebForms + WCF +
Windows Service + ADO.NET + SQL Server) being modernized to .NET 9 + Azure
Container Apps by three parallel tracks. Full background: `docs/pre-read.md`,
`docs/task-briefs.md`, `docs/facilitator-guide.md`, `docs/rubric.md`.

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

Chain the two extension flavors in order: **Upgrade first, then Azure
migration** (containerization, Managed Identity, Key Vault, Blob, App
Insights, Bicep).

## Branch & ownership rules

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
- `docs/progress-data.js` is generated by `docs/scan-progress.ps1` on every
  push to `main` (or manual dispatch) — never hand-edit it or commit it
  (it's git-ignored by design).
