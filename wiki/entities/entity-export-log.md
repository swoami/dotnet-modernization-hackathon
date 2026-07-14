# Entity: ExportLog

> Source: `db/001-schema.sql` (table `dbo.ExportLog`)
> Config/Mapping: none — no model or repository in code
> Table/Storage: `dbo.ExportLog`

## Properties

| Property | Type | Constraints | Notes |
|---|---|---|---|
| ExportId | INT | PK, IDENTITY(1,1) | — |
| ExportedAt | DATETIME2 | NOT NULL, DEFAULT SYSUTCDATETIME() | — |
| FilePath | NVARCHAR(512) | NOT NULL | — |
| RowCount | INT | NOT NULL | — |

## Relationships

- None defined.

## Repository / Access

- No C# model or repository exists for this table. The worker export flow does **not** write to it.

## Key behaviors

- Defined in schema to record export audit rows, but currently unused by application code. See [[arch-worker]].

## Resolved (2026-07-13)

- `ExportLog` is **never** populated by the exporter (or any code). No C# model or repository exists and the worker writes only a CSV file. Confirmed unused. See [[concept-claim-export]].

## Related pages
- [[concept-claim-export]]
- [[arch-worker]]
- [[arch-database]]
