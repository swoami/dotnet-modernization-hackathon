# Database

> Sources: `db/001-schema.sql`, `db/002-seed.sql`, `ContosoInsurance.Data/` (repositories + `Models/`).

## Summary

A single SQL Server database `ContosoInsurance` with four tables, accessed exclusively via raw ADO.NET repositories (no ORM).

## Current state

- Schema created by `db/001-schema.sql`; sample data by `db/002-seed.sql`.
- Tables: `Users`, `Policies`, `Claims`, `ExportLog`. See [[entity-user]], [[entity-policy]], [[entity-claim]], [[entity-export-log]].
- `Claims.PolicyId` references `Policies.PolicyId`. Index `IX_Claims_FiledOn` on `Claims(FiledOn DESC)`.
- Access is synchronous ADO.NET (`SqlConnection`/`SqlCommand`/`SqlDataReader`), one repository per aggregate.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `db/001-schema.sql` | DDL for 4 tables | — |
| `db/002-seed.sql` | Seed users/policies/claims | SHA1 hashes in seed |
| `ContosoInsurance.Data/ClaimsRepository.cs` | Claim CRUD + search | Contains SQL injection |
| `ContosoInsurance.Data/PolicyRepository.cs` | `GetAll` policies | Parameterless read |
| `ContosoInsurance.Data/UserRepository.cs` | User lookup + password verify | SHA1 |
| `ContosoInsurance.Data/Models/` | POCO models | `Claim`, `Policy`, `User` |

## Key dependencies

- `System.Data.SqlClient`
- Connection string `ContosoDb` from config via `ConfigHelper.GetConnectionString`

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| Raw ADO.NET, synchronous | No async, verbose mapping | all repositories |
| SQL injection | Data breach | `ClaimsRepository.SearchByClaimant` |
| `AddWithValue` | Potential type inference issues | repositories |
| No `ExportLog` model/repository | Table unused by code | `db/001-schema.sql` |

## Resolved (2026-07-13)

- No EF/ORM; no migrations. Confirmed absent.
- **`ExportLog` is never surfaced in code** — no model/repository and no INSERT anywhere. Confirmed unused.
- **`SearchByClaimant` has no caller** — confirmed dead code (still an injection risk if ever wired up).

## Related pages
- [[entity-claim]]
- [[entity-policy]]
- [[entity-user]]
- [[entity-export-log]]
- [[arch-security]]
