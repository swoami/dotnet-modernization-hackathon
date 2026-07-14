# WCF Scoring Service

> Sources: `ContosoInsurance.Services/` (`IClaimScoringService.cs`, `ClaimScoringService.svc(.cs)`, `Web.config`).

## Summary

A WCF SOAP service (`basicHttpBinding`) that scores claims with deterministic rules. Consumed by the Web portal.

## Current state

- Contract `IClaimScoringService` exposes two operations:
  - `int ScoreClaim(int claimId)` — loads the claim via `ClaimsRepository.GetById`, computes a rules-based score in the range 0–1000, returns `-1` if not found.
  - `string GetModelVersion()` — returns the `ScoringModelVersion` app setting (default `v0`; configured `v1.3`).
- Scoring rules (in `ClaimScoringService.svc.cs`): base 500; ±150/+50 by amount vs 10,000; −25 if status ≠ Pending; ±75/+25 by claim age vs 30 days; clamped to [0, 1000].
- Hosted via `.svc` in IIS; endpoint `http://localhost:8080/ClaimScoringService.svc`.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Services/IClaimScoringService.cs` | `[ServiceContract]` | 2 operations |
| `ContosoInsurance.Services/ClaimScoringService.svc.cs` | Scoring logic | Rules-based, not ML |
| `ContosoInsurance.Services/Web.config` | WCF bindings + connection string | `includeExceptionDetailInFaults="true"` |

## Key dependencies

- `System.ServiceModel` (WCF)
- `ContosoInsurance.Data` (`ClaimsRepository`)
- `ContosoInsurance.Common` (config, logging)

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| WCF SOAP | Not supported by default in modern .NET | `Web.config` `system.serviceModel` |
| `includeExceptionDetailInFaults="true"` | Info disclosure | `Web.config` |
| Plaintext SQL credentials | Secret leak | `Web.config` connection string |
| New `ClaimsRepository()` per call | No DI, no async | `ClaimScoringService.svc.cs` |
| No log4net config; `AppLogger.Configure()` never called | Service-side logs (log4net) dropped | `Services/Web.config`, `ClaimScoringService.svc.cs` |
| Web consumes contract without a project reference | **Confirmed compile-time build break (CS0234)** | `ContosoInsurance.Web.csproj`, `Default.aspx.cs` |

## Resolved (2026-07-13)

- `.svc` inspected: a single `@ ServiceHost` directive (`Service="ContosoInsurance.Services.ClaimScoringService"`, `CodeBehind=...svc.cs`). No hidden logic; existing findings unchanged.
- `Services` has **no** log4net configuration and never calls `AppLogger.Configure()` — log4net output is dropped; only `Trace` emits. See [[arch-logging]].
- `ContosoInsurance.Web.csproj` has **no** `ProjectReference` (and no assembly reference) to this project despite `Default.aspx.cs` using `IClaimScoringService`.

## Confirmed build break (2026-07-13)

Baseline compile confirmed the Web→Services gap is a **real** build break, not an out-of-repo proxy situation:

```
Default.aspx.cs(7,24): error CS0234: The type or namespace name 'Services'
does not exist in the namespace 'ContosoInsurance' (are you missing an assembly reference?)
```

- No generated proxy / Service Reference / `Reference.cs` exists in `ContosoInsurance.Web/` — the only reference to `ContosoInsurance.Services.IClaimScoringService` outside code is the `<endpoint contract=...>` string in `Web.config`, which does not satisfy the compiler.
- After working around unrelated environment issues, this CS0234 is the **only** remaining build error in the solution. See [[build-and-test]].

## Related pages
- [[concept-claim-scoring]]
- [[arch-webforms]]
- [[arch-database]]
