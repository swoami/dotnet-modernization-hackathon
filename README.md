# Legacy Breakers — .NET App Modernization Hackathon

Full-day hackathon for a team of 6 to modernize a real legacy .NET Framework app
using **GitHub Copilot app modernization for .NET** and the **Modernization CLI**.

## What's in this repo

| Path | Purpose |
| --- | --- |
| `src/ContosoInsurance/` | The **legacy** .NET Framework 4.6.1 sample app to modernize |
| `docs/pre-read.md` | Read before the event — tools, install, mental model |
| `docs/agenda.md` | Run-of-show with timings |
| `docs/task-briefs.md` | Per-track task briefs (3 pair-tracks) |
| `docs/facilitator-guide.md` | Facilitator prompts, checkpoints, gotchas |
| `docs/rubric.md` | Demo / evaluation rubric |
| `docs/reference-solution.md` | Answer key — target modernized state and expected artifacts |
| `docs/tracker.html` | **Live progress tracker** — open in a browser during the event |

## Target state (post-hackathon)

- .NET 9 across all projects
- Azure Container Apps hosting Web + WCF-successor + Worker
- Azure SQL (Managed Identity, no passwords)
- Azure Blob Storage replaces local file I/O
- Azure Key Vault for anything that must remain a secret
- App Insights + `ILogger` replaces log4net + `Trace`
- `appsettings.json` + `IConfiguration` replaces `Web.config`
- Bicep infrastructure via `azd`
- GitHub Actions CI/CD pipeline

## Start here

1. Facilitators: read `docs/facilitator-guide.md` end-to-end.
2. Participants: read `docs/pre-read.md` **before** the event.
3. On the day: follow `docs/agenda.md` and open `docs/tracker.html` in a browser
   to check tasks off as they land. Use its **Export** button and commit
   `docs/tracker-state.json` if you want to sync progress across laptops via git.

> This repository intentionally contains outdated packages, insecure patterns, and
> anti-patterns. **Do not deploy the legacy app to production.**
