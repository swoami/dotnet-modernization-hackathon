# Entity: User

> Source: `ContosoInsurance.Data/Models/User.cs`, `ContosoInsurance.Data/UserRepository.cs`
> Config/Mapping: manual ADO.NET mapping (no ORM)
> Table/Storage: `dbo.Users` (`db/001-schema.sql`)

## Properties

| Property | Type | Constraints | Notes |
|---|---|---|---|
| UserId | INT | PK, IDENTITY(1,1) | — |
| Username | NVARCHAR(64) | NOT NULL, UNIQUE | — |
| PasswordHash | NVARCHAR(128) | NOT NULL | SHA1(password + salt) hex |
| Salt | NVARCHAR(64) | NOT NULL | Plaintext per-user salt |
| Role | NVARCHAR(32) | NOT NULL, DEFAULT 'Agent' | Agent, Adjuster, Admin |

## Relationships

- None defined in schema (no FKs to/from `Users`).

## Repository / Access

- `UserRepository.FindByUsername(string)` — parameterized SELECT.
- `UserRepository.VerifyPassword(User, string)` — compares `SHA1(password + salt)` to stored hash (case-insensitive).

## Key behaviors

- Passwords hashed with SHA1 (weak, intentional). See [[arch-security]].
- Seed users (`db/002-seed.sql`): `agent1`, `adjuster`, `admin` with SHA1 hashes.

## Resolved (2026-07-13)

- `Role` is **not enforced anywhere** beyond storage — read by `UserRepository` but never used for authorization (no `IsInRole`, `[Authorize]`, `Roles.*`, or role-scoped `<authorization>`). Confirmed. See [[arch-security]].

## Related pages
- [[concept-login]]
- [[arch-security]]
- [[arch-database]]
