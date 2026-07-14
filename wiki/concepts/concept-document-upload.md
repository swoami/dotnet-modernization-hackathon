# Concept: Document Upload

> Sources: `ContosoInsurance.Web/Upload.aspx.cs`, `ContosoInsurance.Web/Web.config`.

## Summary

Agents upload a claim document, which is saved to the local filesystem under a per-claim folder.

## Current flow

1. `SubmitBtn_Click` validates that a file was selected and its size ≤ `MaxUploadBytes` (default 10 MB).
2. Reads `ClaimDocumentsRoot` (default `C:\ClaimsFiles`) and the entered claim id.
3. Creates folder `{root}\{claimId}` and saves the file to `{folder}\{FileUploadCtrl.FileName}`.
4. Logs the saved path and shows a status message.

## Important files

| Path | Role |
|---|---|
| `ContosoInsurance.Web/Upload.aspx.cs` | Upload handler |
| `ContosoInsurance.Web/Web.config` | `ClaimDocumentsRoot`, `MaxUploadBytes` |

## Rules / behavior

- Enforces a max upload size.
- Stores files on local disk, not in the database or object storage.

## Risks / legacy issues

- **Path traversal**: client-supplied filename is used directly in the path.
- Local `C:\` dependency (won't run in containers).
- `Claims.DocumentPath` is **not** updated by this flow.

## Resolved (2026-07-13)

- **`DocumentPath` is never persisted** by the upload flow. `Upload.aspx.cs` saves the file to disk (`PostedFile.SaveAs`) and makes **no** repository call. `ClaimsRepository.Insert` supports a `DocumentPath` parameter but is not invoked here. Confirmed via `Upload.aspx` markup (plain `FileUpload` + `TextBox` for claim id) and code-behind.

## Related pages
- [[entity-claim]]
- [[arch-webforms]]
- [[arch-security]]
