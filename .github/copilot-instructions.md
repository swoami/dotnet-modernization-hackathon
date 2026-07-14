# Copilot Instructions — ContosoInsurance

This is a deliberately-legacy .NET Framework 4.6.1 claims app used as a modernization hackathon target. It contains intentional insecure patterns and outdated packages. Do not treat it as production code, and do not reuse its secrets or file I/O patterns.

## Wiki Navigation

On every new session/task:
1. Read `wiki/index.md` first.
2. Load only the relevant wiki pages for the task.
3. Check `wiki/log.md` for recent decisions/changes.

Operations:
- Query: read index → load relevant pages → work.
- Ingest: when new knowledge is discovered, update the relevant wiki page, update index if needed, and add a log entry.
- Lint: periodically check for contradictions, stale info, and orphan pages.

Session End Rule:
After any implementation change, update affected wiki pages, add a dated `wiki/log.md` entry, and update `wiki/index.md` if pages/status changed. This is mandatory.
