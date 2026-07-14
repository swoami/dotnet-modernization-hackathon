# Entity: Claim

> Source: `ContosoInsurance.Data/Models/Claim.cs`, `ContosoInsurance.Data/ClaimsRepository.cs`
> Config/Mapping: manual ADO.NET mapping (no ORM)
> Table/Storage: `dbo.Claims` (`db/001-schema.sql`)

## Properties

| Property | Type | Constraints | Notes |
|---|---|---|---|
| ClaimId | INT | PK, IDENTITY(1,1) | — |
| PolicyId | INT | NOT NULL, FK → Policies | — |
| ClaimantName | NVARCHAR(128) | NOT NULL | — |
| Amount | DECIMAL(18,2) | NOT NULL | — |
| Status | NVARCHAR(32) | NOT NULL, DEFAULT 'Pending' | Pending, Approved, Rejected, Paid |
| FiledOn | DATETIME2 | NOT NULL, DEFAULT SYSUTCDATETIME() | Indexed DESC (`IX_Claims_FiledOn`) |
| ClosedOn | DATETIME2 | NULL | — |
| DocumentPath | NVARCHAR(512) | NULL | e.g. `C:\ClaimsFiles\1\photo.jpg` |
| Score | INT | NULL | 0–1000 from scoring service |
| Notes | NVARCHAR(1024) | NULL | — |
| PolicyNumber | (model only) | — | Populated via JOIN, not a table column |

## Relationships

- Many `Claims` → one `Policy` (`PolicyId`).

## Repository / Access

- `ClaimsRepository.GetRecent(int top=50)` — parameterized TOP query, JOIN to Policies.
- `ClaimsRepository.GetById(int)` — parameterized single-row read.
- `ClaimsRepository.SearchByClaimant(string)` — **string-concatenated SQL (injection risk)**.
- `ClaimsRepository.Insert(Claim)` — parameterized insert, returns `SCOPE_IDENTITY`.
- `ClaimsRepository.UpdateScore(int, int)` — parameterized score update.

## Key behaviors

- Scored by the WCF service and written back via `UpdateScore` on `Default.aspx` load. See [[concept-claim-scoring]].
- Exported to CSV by the worker. See [[concept-claim-export]].

## Resolved (2026-07-13)

- `Insert` has **no** UI caller in the repo (upload does not call it). Confirmed.
- `DocumentPath` is **never** updated by the upload flow — `Upload.aspx.cs` writes the file to disk only. Confirmed. See [[concept-document-upload]].
- `SearchByClaimant` has **no** caller anywhere — confirmed dead code. See [[arch-database]].

## Related pages
- [[entity-policy]]
- [[concept-claim-listing]]
- [[concept-claim-scoring]]
- [[concept-claim-export]]
- [[arch-database]]
