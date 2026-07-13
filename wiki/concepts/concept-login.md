# Concept: Login

> Sources: `ContosoInsurance.Web/Login.aspx.cs`, `ContosoInsurance.Data/UserRepository.cs`, `ContosoInsurance.Web/Web.config`.

## Summary

Agents authenticate with username/password via ASP.NET Forms Authentication; passwords are verified against SHA1+salt hashes.

## Current flow

1. Agent submits username/password on `Login.aspx`.
2. `SignInBtn_Click` calls `UserRepository.FindByUsername(username)`.
3. `UserRepository.VerifyPassword` compares `SHA1(password + salt)` to the stored hash.
4. On success, `FormsAuthentication.SetAuthCookie` is set and the request is redirected to the return URL; on failure an error label is shown and a warning is logged.

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Web/Login.aspx.cs` | Login handler, Forms Auth cookie |
| `ContosoInsurance.Data/UserRepository.cs` | User lookup + SHA1 verify |
| `ContosoInsurance.Web/Web.config` | Forms Auth config, deny anonymous |

## Rules / behavior

- Anonymous users are denied (`<deny users="?" />`).
- Failed logins are logged via `AppLogger.Warn`.
- `SetAuthCookie(user.Username, false)` stores only the username; the user's `Role` is **not** embedded in the ticket and is not used for authorization.

## Risks / legacy issues

- SHA1 password hashing (weak). See [[arch-security]].
- Forms Auth (no modern identity provider).
- No lockout/throttling observed.

## Resolved (2026-07-13)

- **No logout flow** exists anywhere — no `FormsAuthentication.SignOut` call and no logout page/link. Confirmed.
- **`Role` does not affect authorization** — read from the DB but never enforced (no `IsInRole`, `[Authorize]`, `Roles.*`, or role-scoped `<authorization>`). Confirmed. See [[arch-security]].

## Related pages
- [[entity-user]]
- [[arch-security]]
- [[arch-webforms]]
