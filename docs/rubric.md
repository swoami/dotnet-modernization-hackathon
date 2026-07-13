# Evaluation rubric

Since the team of 6 is collaborating on one repo (not competing), this rubric
is used **at the final demo** to grade the *outcome*, not to rank people. It
also tells the team what to optimize for during the day.

Total: **100 points**. Aim for ≥ 70 to call the hackathon a success.

---

## 1. Framework & package modernization — 15 pts

- 5 pts — All projects target `net9.0`, SDK-style
- 4 pts — `packages.config` fully removed
- 3 pts — `dotnet list package --vulnerable` reports zero High/Critical
- 3 pts — `Newtonsoft.Json`, `log4net` removed from direct references

## 2. Configuration & data access — 15 pts

- 4 pts — No `Web.config` / `App.config` remaining
- 4 pts — `IConfiguration` + strongly-typed options in use
- 5 pts — EF Core `DbContext` replaces ADO.NET across `Data/`
- 2 pts — SQL injection removed from `SearchByClaimant`

## 3. Hosting & runtime — 15 pts

- 5 pts — Web + API run on Kestrel (ASP.NET Core)
- 5 pts — Worker runs as `BackgroundService` on the Generic Host
- 5 pts — All three run as containers locally (`docker compose up` or
  `docker run` per service)

## 4. Storage & I/O — 10 pts

- 5 pts — Uploads write to Blob container `claim-docs`
- 5 pts — Worker exports write to Blob container `claim-exports`

## 5. Identity & secrets — 10 pts

- 5 pts — No SQL password anywhere; Azure SQL accessed via Managed Identity
- 3 pts — Managed Identity used for Blob + ACR pull
- 2 pts — Any remaining secrets referenced from Key Vault (or a clear
  statement that no secrets remain)

## 6. Observability — 10 pts

- 4 pts — `ILogger<T>` used throughout; `log4net` and `Trace.*` gone
- 4 pts — App Insights connection string wired via config
- 2 pts — Requests / traces / logs visible in App Insights after smoke test

## 7. Infrastructure as code — 10 pts

- 4 pts — Bicep provisions the full target stack in one `azd up`
- 3 pts — Bicep is parameterized, has no secrets, no hard-coded resource names
- 3 pts — Role assignments are in Bicep (not hand-clicked in the portal)

## 8. CI/CD — 5 pts

- 3 pts — GitHub Actions workflow builds and deploys via OIDC
- 2 pts — Workflow gates on `dotnet test` (even if there are no meaningful tests)

## 9. Team artifacts — 10 pts

- 3 pts — `docs/learnings.md` captures ≥ 6 prompt/response lessons
- 2 pts — Every commit references its appmod task or a concrete concern
- 3 pts — Team demo covers all three tracks in ≤ 20 minutes
- 2 pts — Stretch goals *listed as follow-up issues* (even if not done)

---

## Bonus (uncapped, but a max of +15 counts)

- +5 — WebForms fully converted to Blazor Server (not just Razor Pages)
- +5 — WCF fully converted to CoreWCF **or** gRPC, with a working client
- +5 — Entra ID authentication (`Microsoft.Identity.Web`) replaces cookie auth
- +3 — Health checks + readiness/liveness probes wired in Bicep
- +3 — OpenTelemetry export configured (in addition to App Insights)
- +3 — Cost check: `azd show` cost breakdown captured in `learnings.md`

---

## Anti-goals (deduct)

- −5 — A secret (password, connection string with credentials, storage key) is
       committed anywhere in the repo
- −5 — `azd up` fails at final demo time
- −3 — Any Copilot-generated diff was accepted without review (spot check
       during retro)
- −3 — Any part of the app still requires `C:\` paths to work

---

## Success signal (informal)

If, at the end of the day, a new hire could clone `main`, run `azd up`, and
have a working modernized system in the shared RG **within 15 minutes** —
the hackathon worked.
