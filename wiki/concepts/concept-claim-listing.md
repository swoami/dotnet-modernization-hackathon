# Concept: Claim Listing

> Sources: `ContosoInsurance.Web/Default.aspx.cs`, `ContosoInsurance.Data/ClaimsRepository.cs`.

## Summary

The portal home page (`Default.aspx`) shows the 50 most recent claims and triggers scoring for any that are unscored.

## Current flow

1. `Page_Load` calls `ClaimsRepository.GetRecent(50)`.
2. For each claim without a `Score`, it calls the WCF scoring service (`CallScoringService`) and persists the result via `UpdateScore`.
3. Binds the claim list to `ClaimsGrid`.

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Web/Default.aspx.cs` | Loads + binds claims, triggers scoring |
| `ContosoInsurance.Data/ClaimsRepository.cs` | `GetRecent`, `UpdateScore` |

## Rules / behavior

- Only unscored claims are scored on load.
- Scoring failures are caught per-claim and logged; listing still renders.

## Risks / legacy issues

- Scoring runs synchronously on every page load (performance/coupling).
- Direct WCF `ChannelFactory` creation per claim (no pooling/DI).
- `Default.aspx.cs` references `ContosoInsurance.Services.IClaimScoringService`, but `ContosoInsurance.Web.csproj` has **no** `ProjectReference` to the Services project — a compile-time dependency gap. See [[arch-wcf-service]].

## Resolved (2026-07-13)

- `Default.aspx` markup inspected: a single `asp:GridView` (`ClaimsGrid`) with bound columns `ClaimId, PolicyNumber, ClaimantName, Amount, Status, FiledOn, Score` and a link to `Upload.aspx`. No hidden logic; existing findings unchanged.

## Related pages
- [[entity-claim]]
- [[concept-claim-scoring]]
- [[arch-webforms]]
