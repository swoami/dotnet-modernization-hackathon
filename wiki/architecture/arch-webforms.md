# WebForms Portal

> Sources: `ContosoInsurance.Web/` (`Default.aspx(.cs)`, `Login.aspx(.cs)`, `Upload.aspx(.cs)`, `Global.asax`, `Web.config`).

## Summary

ASP.NET WebForms agent portal running under IIS. Three pages: claim list (`Default.aspx`), login (`Login.aspx`), and document upload (`Upload.aspx`).

## Current state

- **`Default.aspx`** loads the 50 most recent claims via `ClaimsRepository.GetRecent(50)`, then for each unscored claim calls the WCF scoring service and writes the score back with `UpdateScore`. Binds results to `ClaimsGrid`.
- **`Login.aspx`** verifies credentials via `UserRepository` and sets a Forms Auth cookie (`FormsAuthentication.SetAuthCookie`).
- **`Upload.aspx`** saves an uploaded file to `C:\ClaimsFiles\{claimId}\{filename}`.
- Access control via `<authorization><deny users="?" /></authorization>` in `Web.config`.

## Important files/folders

| Path | Purpose | Notes |
|---|---|---|
| `ContosoInsurance.Web/Default.aspx.cs` | Claim listing + inline scoring | Scores on every page load |
| `ContosoInsurance.Web/Login.aspx.cs` | Forms Auth login | See [[concept-login]] |
| `ContosoInsurance.Web/Upload.aspx.cs` | File upload to disk | See [[concept-document-upload]] |
| `ContosoInsurance.Web/Global.asax(.cs)` | App lifecycle | — |
| `ContosoInsurance.Web/Web.config` | Config + auth + WCF client | Plaintext secrets |

## Key dependencies

- `System.Web` (WebForms, `FormsAuthentication`)
- `System.ServiceModel` (WCF client to scoring service)
- `ContosoInsurance.Data`, `ContosoInsurance.Common`

## Legacy patterns / issues

| Issue | Impact | Evidence |
|---|---|---|
| WebForms | No ASP.NET Core forward path | `*.aspx` |
| Scoring runs synchronously on page load | Performance/coupling | `Default.aspx.cs` Page_Load |
| Direct `new ClaimsRepository()` | No DI, hard to test | `Default.aspx.cs` |
| `customErrors mode="Off"` | Info disclosure | `Web.config` |
| Path traversal on upload | Arbitrary file write | `Upload.aspx.cs` |

## Unknowns

- No logout page/flow observed. `Pendiente/Unknown`.
- `Default.aspx`/`Login.aspx`/`Upload.aspx` markup not fully inspected. `Pendiente/Unknown`.

## Related pages
- [[concept-login]]
- [[concept-claim-listing]]
- [[concept-document-upload]]
- [[arch-wcf-service]]
- [[arch-security]]
