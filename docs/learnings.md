# Learnings

Team log — captured live during the hackathon. Each entry is a short story:
what you were trying to do, what prompt/tool you used, what happened, what
you'd do differently.

Keep entries short. Bullets are fine. One-liners are fine.

---

## Track B — Worker & Storage modernization (`track/b-worker-storage`)

### Tooling used
`modernize plan create "<description>"` → `modernize plan execute` (GitHub Copilot app modernization CLI)

### Prompts that worked

- **`modernize plan create` with a detailed multi-goal description** — providing all four goals in one
  prompt (SDK upgrade, BackgroundService, appsettings.json, ILogger) produced a well-structured 4-task
  plan with correct dependency ordering. The CLI broke it into discrete tasks automatically.

- **`modernize plan execute` without arguments** — auto-discovered the single plan in
  `.github/modernize/` and ran all tasks in dependency order with no manual steering needed.

- **Asking Copilot Chat "is App.config still used?"** while the CLI was running → instantly identified
  that `ContosoInsurance.Data` repositories call `ConfigHelper.GetConnectionString()` via
  `System.Configuration.ConfigurationManager`. Saved time vs. manually tracing the dependency.

### Prompts that needed a nudge

- **Task 002 stale state across sessions** — the CLI completed the BackgroundService rewrite and wrote
  a consistency-check report, but the session ended before it marked the task `success`. On the next
  `modernize plan execute` run the CLI detected the stale task and resumed cleanly, but it was
  not obvious at first why the task was still showing `started`.

- **App.config → appsettings.json gap not covered by the plan** — the 4-task plan migrated
  `<appSettings>` to `appsettings.json` (task 002) but did not address `<connectionStrings>` because
  the Data repositories still used `ConfigHelper`. The plan stopped short of full App.config removal.
  Required a manual follow-up: add a `ClaimsRepository(string connectionString)` constructor overload,
  inject `IConfiguration` into `ClaimsExporterService`, and then delete `App.config`.

### Gotchas

- **Plaintext password flagged by the CLI's own consistency check** — the CLI added the DB connection
  string (including `****** to `appsettings.json` automatically as part of task 002,
  then immediately flagged it as a Major security issue in its own consistency-check report, and
  replaced it with a placeholder in the same task. The plan correctly treats credentials as a hard
  fail — useful reminder not to accept the first draft of `appsettings.json` blindly.

- **`App.config` survives task 002 by design** — the log4net `<configSections>` block kept
  `App.config` alive as an intermediate state (task 003 removed it). This was correct but created
  a dual-config situation (`App.config` + `appsettings.json`) that was confusing mid-migration.
  The consistency-check report calls this out explicitly, which was helpful.

- **`ContosoInsurance.Data` was not in scope for the Worker-focused plan** — the plan targeted
  `ContosoInsurance.Worker` and `ContosoInsurance.Common`, but the Worker pulls in the Data project
  which still uses `System.Configuration.ConfigurationManager` and the old static `ConfigHelper`.
  The build passes, but the runtime still reads from `ConfigurationManager` unless you explicitly
  thread the connection string through. Worth noting when scoping plans: declare ALL projects that
  need changing, not just the entry point.

- **`[SupportedOSPlatform("windows")]` needed after SDK upgrade** — several CA1416 warnings appeared
  after upgrading to net9.0 because Windows-Service APIs are platform-specific. The CLI added the
  attribute automatically; they all disappeared in task 002 when the Windows Service code was replaced
  entirely.

- **Monitoring a running `modernize plan execute` session** — the CLI runs in its own terminal and
  writes to `.github/modernize/<plan>/` as it goes. The most reliable way to track real-time progress
  is watching `tasks.json` (`status` fields) and checking for new task folders appearing.
  `git diff --stat` after each commit also gives a clean summary of what changed.

### Follow-ups

- **`ContosoInsurance.Data` connection string injection** — `ClaimsRepository`, `PolicyRepository`,
  and `UserRepository` still have parameterless constructors that call `ConfigHelper.GetConnectionString()`.
  The Worker project was fixed (manual step, not covered by the plan), but Web and Services still rely
  on `Web.config`. Full clean-up requires injecting the connection string (or an EF Core `DbContext`)
  via DI across all three projects — Track A owns `DbContext`, coordinate before changing Data.

- **`ExportRoot` default is `C:\Exports`** — cross-platform default; will break on Linux containers.
  Override via `ConnectionStrings__ContosoDb` env var or mount path before containerising (Track C).

- **`Export()` uses synchronous file I/O wrapped in `Task.Run`** — functional but not idiomatic for
  `BackgroundService`. Replace with `File.WriteAllTextAsync` and async ADO.NET when refactoring further.

- **`AppLogger` is now `[Obsolete]`** — `ClaimsRepository` still calls it (CS0618 warning). Task 003
  left this intentionally as a migration signal. Clean it up when the Data layer gets its DI overhaul.

---

## Track C — Platform & infra (`track/c-platform`)

### Prompts that worked

- **Track A** — *"…"* → produced *…*. Kept.
- **Track C — overall approach**: three things made the infra work land cleanly rather than as guesswork:
  1. **Assessment as source of truth** — every non-obvious infra decision (Services deployed as its own Container App vs. merged into Web, port numbers, the SQL DB name, the WCF/port-8080 detail) was justified by re-reading `.github/modernize/assessment/reports-*/facts/*.md` (`architecture-diagram.md`, `api-service-contracts.md`) rather than assumed from the task brief alone. Copilot was explicitly pointed at that folder in the prompt (`@dmh\.github\modernize\assessment\`) instead of being asked to "figure it out."
  2. **Cross-track branch sync** — infra/bicep was drafted from Track C alone first, but every later "make it consistent" pass (env var names, DB name, Dockerfile ports) required diffing against `track/a-web-api` and `track/b-worker-storage`'s actual app code/csproj/appsettings, not just Track C's own docs. Track branches only sync with `main` on push/dispatch, and pushes to `track/*` never auto-refresh each other — so a manual `git fetch`/`git show origin/track/x:<path>` cross-check was necessary each time, it wasn't automatic.
  3. **Fleet mode** (`gh copilot cli` background sub-agent dispatch via the `task` tool, invoked in this session by the literal prompt "You are now in fleet mode. Dispatch sub-agents... in parallel") — used for the 5 independent Bicep modules and again for the KV/OIDC pair, each with an explicit param/output contract up front so the branch could take concurrent pushes with zero merge conflicts. Reserved for genuinely parallel, independent-file work; sequential/dependent work (e.g. README-DEPLOY.md needing the other two done first) was still gated via SQL `todo_deps`, not force-parallelized.
  4. **Specialized upgrade agent for modernization prep** — the GH Copilot CLI environment ships a dedicated `upgrade-agent:upgrade` custom agent (distinct from the generic `general-purpose` agent) purpose-built for the "upgrade and modernize an app" workflow. For modernization-prep tasks that fit its focus — e.g. drafting per-project Dockerfiles for a legacy-to-.NET-9 migration — reach for that specialized agent instead of `general-purpose` first; it carries workflow-specific structure (assess → plan → apply loop) the generic agent has to be told about by hand each time. Worth trying on the next round of Dockerfile/porting prep rather than defaulting straight to `general-purpose`.
- **Track C** — *"Fleet deployed: prepare Draft infra/main.bicep provisioning: [full resource list] … you can use info @dmh\.github\modernize\assessment\ and work from other track branches"* → gave a fleet of 5 background agents an explicit, locked-down param/output contract per module up front (exact file names, param names, output names) so each could draft its own `.bicep` file independently without seeing the others' work, then did the `main.bicep` integration by hand. Zero merge conflicts even though all 5 pushed to the same branch — each agent did `git pull --rebase` before its final push. Kept.
- **Track C** — *"make sure infra is consistent with what is being developed there"* (after track a/b branches appeared) → diffing the actual app code (`ConfigHelper.GetConnectionString("ContosoDb")` in `ContosoInsurance.Data`) against the infra's env var names caught a real bug: `ConnectionStrings__ContosoInsurance` vs the app's expected `ConnectionStrings__ContosoDb`. Cross-branch consistency checks need to read the *app code*, not just assume names — asking Copilot to "make it consistent" only works if you point it at the source of truth.
- **Track C** — *"finish infra tasks: Key Vault secret references · GitHub Actions deploy (OIDC) · README-DEPLOY.md"* → ran 2 independent background agents in parallel (KV secret wiring, OIDC workflow — genuinely independent files) and gated a 3rd (README-DEPLOY.md) as a SQL `todo_deps` dependency on both, since an accurate deploy doc needs to read the *actual* generated workflow/secret names rather than guess them ahead of time. The dependent agent's prompt explicitly said "read `.github/workflows/deploy.yml` once it exists... if not yet present when you start, note TODO and re-check before finishing" as a safety net. Worked cleanly.
- **Track C** — mid-flight scope change: *"azd deployment should be configured for [specific RG/subscription]... make no deployment yet"* arrived while the GitHub Actions agent was still running → used `write_agent` to inject the new requirement into its live conversation instead of waiting for it to finish and re-prompting from scratch. It incorporated the change in its next turn and re-pushed. Much faster than a second full dispatch.

### Prompts that needed a nudge

- **Track B** — first prompt was too broad; broke into three; then it worked.
- **Track C** — asked the azure.yaml sub-agent to set `docker.path`/`docker.context` from a plausible-sounding guess; double-checked against the actual azd JSON schema (`https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json`) afterward and found `path` (not `dockerfile`) is the correct field, and both `path` and `context` resolve relative to the service's `project` folder — not the repo root or the azure.yaml folder. Fixed by hand after the agent's first pass. Moral: for less-common tool config schemas, verify against the authoritative schema/docs rather than trusting a plausible first draft, even your own.

### Gotchas

- Something surprising you hit and how you got past it.
- `shell: pwsh` steps in GitHub Actions run as `pwsh -Command ". 'script.ps1'"` (dot-sourced). A stale `$LASTEXITCODE` from an intentionally-nonzero `git` call inside the script can leak out as the *step's* exit code, failing the job even though the script's own logic succeeded. Fix: end the script's success path with an explicit `exit 0`. Could not reproduce locally on Windows pwsh — only showed up on the Linux runner; fixed and verified via `workflow_dispatch` rather than local repro.
- GitHub Pages 404s at the site root if `docs/` has no `index.html` — `docs/tracker.html` alone wasn't enough even though the workflow uploads the whole `docs/` folder as the Pages artifact.
- `az bicep build` intermittently failed locally with `WinError 32` (file lock) on this machine — transient, not a real Bicep error; retrying (or just trusting a clean run from another agent) resolved it.
- Bicep's `listKeys()` requires an argument calculable at the *start* of deployment — passing a module's `output ... resourceId` doesn't qualify (BCP181), even though the resource obviously exists by the time it's called. Fix: rebuild the same `resourceId()` locally from the deterministic resource *name* instead of reusing the module output.
- Container Apps env vars can only have `value` OR `secretRef`, never both — switching the SQL connection string from a plaintext `value` to a Key Vault-backed `secretRef` meant removing the old `value:` line entirely, not just adding `secretRef:` alongside it.
- `gh pr create` silently targeted the wrong repo (`heisenberg-alt/...` fork set as `upstream` remote) instead of `origin` (`swoami/...`), failing with a confusing "No commits between main and <branch>" error since it compared branches that didn't exist on that fork. Fix: pass `--repo swoami/dotnet-modernization-hackathon` explicitly rather than relying on `gh`'s remote auto-detection whenever a repo has more than one remote configured.
- A previously-merged, well-intentioned workflow tweak (`push: branches: [main, 'track/*']` on `tracker-pages.yml`) silently made every track-branch push run a doomed `deploy` job, since the `github-pages` environment's branch-protection rule only allows deploys from `main` — the *build* job succeeded (making it look "half-working"), which made the real problem (a wasteful, always-red `deploy` job on every track push) easy to miss at a glance. Worth periodically diffing a shared workflow file against what it looked like a few PRs ago, not just checking whether the latest run passed.

### Follow-ups

- Things we did not finish; who wants to pick them up.
- `A1`/`B1` ("Assess") tasks in `docs/tracker.html` are still hardcoded `manual: true` checkboxes — `scan-progress.ps1` could auto-detect them from `.github/modernize/assessment/reports-*` presence on each branch, but this hasn't been implemented. Whoever owns the tracker next could pick this up.
- No live `azd provision`/`az deployment group create` run has been done against `infra/main.bicep` yet — only compile-time `az bicep build` validation, and every KV/OIDC/RG-targeting change so far has deliberately stayed config-only per explicit instruction ("make no deployment yet"). Needs a real first deploy pass into `rg-swo-gh-hackathon-team3` before Track C can call infra "done" — see `docs/README-DEPLOY.md` for the exact commands.
- The Azure AD federated credential (OIDC trust) for `.github/workflows/deploy.yml` has not been configured on the Azure side yet — the workflow will fail to authenticate until that's set up, independent of anything in this repo.

**Resolved this session** (kept for context — remove once the team has read this):
- ~~`azure.yaml` only has a `contosoinsurance-worker` entry~~ — fixed: `contosoinsurance-web`/`contosoinsurance-services` added with correct `docker.path`/`docker.context` (see Gotchas).
- ~~`docs/README-DEPLOY.md` hasn't been written yet~~ — fixed: covers `azd up`, CI/CD secrets/vars, redeploy/rollback, and post-deploy verification.
- ~~SQL connection string passed as a plaintext Container Apps env var~~ — fixed: now a Key Vault secret (`sql-connection-string`) referenced via `secretRef` + managed identity.
