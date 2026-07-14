# Learnings

Team log — captured live during the hackathon. Each entry is a short story:
what you were trying to do, what prompt/tool you used, what happened, what
you'd do differently.

Keep entries short. Bullets are fine. One-liners are fine.

---

## Track B — Worker & Storage modernization (`track/b-worker-storage`)

### Tooling used
`modernize plan create "<description>"` → `modernize plan execute` (GitHub Copilot app modernization CLI)

---

### Prompts that worked

- **`modernize plan create` with a detailed multi-goal description** — providing all four goals in one
  prompt (SDK upgrade, BackgroundService, appsettings.json, ILogger) produced a well-structured 4-task
  plan with correct dependency ordering. The CLI broke it into discrete tasks automatically.

- **`modernize plan execute` without arguments** — auto-discovered the single plan in
  `.github/modernize/` and ran all tasks in dependency order with no manual steering needed.

- **Asking Copilot Chat "is App.config still used?"** while the CLI was running → instantly identified
  that `ContosoInsurance.Data` repositories call `ConfigHelper.GetConnectionString()` via
  `System.Configuration.ConfigurationManager`. Saved time vs. manually tracing the dependency.

---

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

---

### Gotchas

- **Plaintext password flagged by the CLI's own consistency check** — the CLI added the DB connection
  string (including `Password=P@ssw0rd!`) to `appsettings.json` automatically as part of task 002,
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

---

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
