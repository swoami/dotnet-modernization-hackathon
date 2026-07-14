# Security & Authentication

> Sources: `ContosoInsurance.Web/Login.aspx.cs`, `ContosoInsurance.Data/UserRepository.cs`, `Web.config`, `Services/Web.config`, `Worker/App.config`, `db/002-seed.sql`.

## Summary

Authentication is ASP.NET Forms Auth backed by SHA1+salt password hashing. Secrets (SQL credentials) are stored in plaintext in config files. Several intentional vulnerabilities exist.

## Current state

- **Login** ([[concept-login]]): `Login.aspx.cs` looks up the user via `UserRepository.FindByUsername`, verifies with `VerifyPassword`, then `FormsAuthentication.SetAuthCookie`.
- **Password hashing**: `UserRepository.HashSha1` computes `SHA1(password + salt)` as a hex string; compared case-insensitively.
- **Authorization**: `Web.config` denies anonymous users (`<deny users="?" />`) — authenticated-only, no role gating. `User.Role` is read by `UserRepository` but **never** enforced anywhere (no `IsInRole`, `[Authorize]`, `Roles.*`, or role-scoped `<authorization>`). The auth cookie stores only the username.
- **Logout**: none. There is no `FormsAuthentication.SignOut` call and no logout page/link anywhere in the app.
- **Secrets**: SQL connection strings with `User Id=contoso_app;Password=P@ssw0rd!` appear in `Web/Web.config`, `Services/Web.config`, `Worker/App.config`.
- **`machineKey`** in `Web/Web.config` uses `validation="SHA1"`.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Web/Login.aspx.cs` | Forms Auth login | Sets auth cookie |
| `ContosoInsurance.Data/UserRepository.cs` | Password verify + SHA1 | Weak KDF |
| `ContosoInsurance.Web/Web.config` | Auth + secrets + machineKey | Plaintext password |
| `db/002-seed.sql` | Seeded SHA1 hashes | "Password1"-based test creds |

## Key dependencies

- `System.Web.Security.FormsAuthentication`
- `System.Security.Cryptography.SHA1`

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| SHA1 password hashing | Weak, crackable credentials | `UserRepository.HashSha1` |
| Plaintext SQL password in config | Secret leak in source | all three config files |
| SQL injection | Data breach | `ClaimsRepository.SearchByClaimant` |
| Path traversal on upload | Arbitrary file write | `Upload.aspx.cs` |
| `customErrors Off` / fault detail on | Info disclosure | `Web/Web.config`, `Services/Web.config` |
| Forms Auth | No modern identity provider | `Login.aspx.cs`, `Web.config` |

## Resolved (2026-07-13)

- **No role-based authorization** exists anywhere. `Role` (Agent/Adjuster/Admin) is stored and read but never enforced. Confirmed.
- **No logout flow** exists (no `FormsAuthentication.SignOut`). Confirmed.

## Related pages
- [[concept-login]]
- [[entity-user]]
- [[arch-configuration]]
- [[concept-document-upload]]
