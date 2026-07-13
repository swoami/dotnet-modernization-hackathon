# Entity: Policy

> Source: `ContosoInsurance.Data/Models/Policy.cs`, `ContosoInsurance.Data/PolicyRepository.cs`
> Config/Mapping: manual ADO.NET mapping (no ORM)
> Table/Storage: `dbo.Policies` (`db/001-schema.sql`)

## Properties

| Property | Type | Constraints | Notes |
|---|---|---|---|
| PolicyId | INT | PK, IDENTITY(1,1) | — |
| PolicyNumber | NVARCHAR(32) | NOT NULL, UNIQUE | e.g. `POL-1001` |
| HolderName | NVARCHAR(128) | NOT NULL | — |
| ProductLine | NVARCHAR(32) | NOT NULL | Auto, Home, Life |
| CoverageAmount | DECIMAL(18,2) | NOT NULL | — |
| EffectiveDate | DATETIME2 | NOT NULL | — |
| ExpirationDate | DATETIME2 | NOT NULL | — |

## Relationships

- One `Policy` → many `Claims` (`Claims.PolicyId` FK → `Policies.PolicyId`).

## Repository / Access

- `PolicyRepository.GetAll()` — reads all policies (no filter/paging).

## Key behaviors

- `Claim` model carries `PolicyNumber` populated via JOIN, not a `Policies` column duplication.

## Unknowns

- No caller of `PolicyRepository.GetAll` observed in Web/Services/Worker. `Pendiente/Unknown`.

## Related pages
- [[entity-claim]]
- [[arch-database]]
