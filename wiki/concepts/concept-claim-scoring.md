# Concept: Claim Scoring

> Sources: `ContosoInsurance.Services/ClaimScoringService.svc.cs`, `IClaimScoringService.cs`, `ContosoInsurance.Web/Default.aspx.cs`.

## Summary

A WCF SOAP service assigns each claim a deterministic, rules-based score in the range 0–1000. It is not machine learning.

## Current flow

1. Web calls `ScoreClaim(claimId)` over `BasicHttpBinding`.
2. The service loads the claim via `ClaimsRepository.GetById`; returns `-1` if not found.
3. Score = base 500, adjusted by amount, status, and age, clamped to [0, 1000]:
   - Amount > 10,000 → −150, else +50.
   - Status ≠ "Pending" → −25.
   - Filed > 30 days ago → −75, else +25.
4. Web persists the score via `ClaimsRepository.UpdateScore`.

`GetModelVersion()` returns the `ScoringModelVersion` setting (configured `v1.3`).

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Services/IClaimScoringService.cs` | Service contract |
| `ContosoInsurance.Services/ClaimScoringService.svc.cs` | Scoring rules |
| `ContosoInsurance.Web/Default.aspx.cs` | WCF client caller |

## Rules / behavior

- Deterministic; same inputs yield the same score.
- Not-found claims return `-1`.

## Risks / legacy issues

- WCF SOAP transport (legacy). See [[arch-wcf-service]].
- `includeExceptionDetailInFaults="true"` (info disclosure).
- New `ClaimsRepository` per call; synchronous.

## Unknowns

- None material; logic is fully visible in `ClaimScoringService.svc.cs`.

## Related pages
- [[entity-claim]]
- [[arch-wcf-service]]
- [[concept-claim-listing]]
