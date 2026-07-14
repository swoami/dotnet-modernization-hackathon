---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - wiki/index.md
  - wiki/overview.md
  - wiki/runbooks/build-and-test.md
  - docs/modernization-planning/hackathon-brief.md
  - docs/modernization-planning/prd.md
  - docs/modernization-planning/plan.md
  - docs/modernization-planning/backlog.md
  - docs/modernization-planning/epics-and-stories.md
---

# Implementation Readiness Assessment — ContosoInsurance Modernization

**Date:** 2026-07-13 · **Project:** dotnet-modernization-hackathon · **Assessor:** John (PM)
**Verdict up front: 🟢 GO** — conditional only on running the morning decision block (E0-S3) before tracks diverge. Details below; read this before 09:00 tomorrow.

---

## 1. Document Inventory

| Document | Role | Status |
|---|---|---|
| `wiki/index.md`, `wiki/overview.md`, `wiki/runbooks/build-and-test.md` | Source of truth (verified baseline) | ✅ Current (2026-07-13) |
| `hackathon-brief.md` | Mission, strategy, team focus | ✅ |
| `prd.md` | Requirements, scope, rubric, flows | ✅ |
| `plan.md` | Tracks, sprints, timeline, DoD | ✅ |
| `backlog.md` | E1–E13 epics + stories, ID scheme | ✅ |
| `epics-and-stories.md` | Full traceable backlog (42 stories) + P0 execution slice | ✅ |
| Standalone Architecture doc | — | ⚠️ None exists; `plan.md` §2 (Before→After table) + PRD §4 serve this role. **Accepted at hackathon scale** — target stack is fully specified. |
| UX design doc | — | ⚠️ None; conversion is page-for-page (Index, Login, Upload). **Accepted** — no new UX is being designed. |

No duplicates found; `epics-and-stories.md` exists only at the confirmed path.

## 2. Validation Results (10 checks)

| # | Check | Verdict | Evidence / notes |
|---|---|---|---|
| 1 | PRD ↔ brief ↔ backlog ↔ epics/stories aligned | ✅ with 3 minor findings | Rubric weights (15/15/15/10/10/10/10/5/10, +15 bonus, −5/−5/−3/−3 deductions), preserved flows, target stack, and story IDs match across all four. Minor findings in §3. |
| 2 | P0 slice realistic for one day | ✅ tight but feasible | 15 slice items; three parallel lanes; appmod-assisted. **Lane A carries 8 of 15 items** — feasible for a pair, but only with the cut line in §4 (risk 2) pre-agreed. |
| 3 | 4-person split realistic | ✅ | 2/1/1 matches PRD §12; solo lanes B and C have self-contained scopes; E11 (P1) is lane C's natural cut line, E2-S3 is lane A's. |
| 4 | Highest-value rubric areas prioritized | ✅ | P0 stories cover the seven 10–15-pt sections = 85 pts; execution order attacks §1 (15 pts, "unblocks everything") first; E12's cheap 10 pts run all day. |
| 5 | Deployment goal not lost | ✅ with 1 watch item | `azd up` is the north star in every doc; E10 is P0; E12-S4 smoke test defends the −5. **Watch:** the deploy window is only 45 min (15:30–16:15) and "first `azd up` fails" is rated High — see §4 risk 3 mitigation. |
| 6 | Cross-lane dependencies visible | ✅ | All four are named in the epics doc: E6-S1 interface → E3-S4 (flagged "early" twice); shared `Data/` shapes (E0-S3.8, single merge owner); E10-S4 ↔ E2-S4 config key names; E7-S1 spans A+C. |
| 7 | Risks and open decisions visible | ✅ | PRD §11 risk table carried into epic-level "main risks"; all eight PRD §15 decisions captured as E0-S3 with owners and a timebox. |
| 8 | No contradictions with the LLM Wiki | ✅ | Spot-checked: 4-table schema, ≤1000-claim export, `ExportLog` never written, `SearchByClaimant` dead code + injection, no logout/`Role` enforcement, restore-succeeds/build-fails baseline — all consistent with `overview.md` and `build-and-test.md`. |
| 9 | Env prerequisites separated from real code defects | ✅ | MSB3644 + MSB4226 labeled environment-only (vanish at `net9.0`, zero fix work) in PRD §1/§10, plan §6, brief §2, and the epics baseline table; E0-S2 makes the team acknowledge it explicitly. |
| 10 | CS0234 handled consistently | ✅ | All docs treat Web→Services CS0234 as the **one confirmed baseline code defect**, **eliminated by design** via E4-S2 (`HttpClient` → minimal API), never repaired with a WCF reference. Wiki confirms no out-of-repo proxy exists, so replacement is the only sound path. |

## 3. Minor Findings (no action required tonight)

1. **Brief header says "team of 6 / three pairs"** while PRD §12 and the epics doc plan for 4 people. Brief §10 already covers the 4-person collapse — cosmetic; ignore or fix the header when convenient.
2. **PRD internal wrinkle:** §5 lists E12 under "In Scope (P0 core)" while §8 rates it P1. The epics doc resolves this sensibly (E12 = P1, except E12-S4 smoke test = P0). No conflict in practice.
3. **Deliberate, documented deviations** in the epics doc: E2-S3 downgraded to P1 (decision-gated per PRD §15.4) and E9-S2 marked P0-or-N/A (topology decision). Both are traceable and correct — just be aware they differ from the raw backlog's P0 epic labels.
4. **E6-S1 mentions an in-memory fake "for tests"** but no test project exists (wiki: `Pendiente/Unknown`). The fake is still useful for local dev; whether a token test project exists is decision 5 below.

## 4. Go / No-Go Recommendation

**🟢 GO.** The planning set is aligned, traceable (FR/NFR → story coverage map exists), grounded in a verified baseline, and correctly shaped for one day. The single gating condition: **E0-S3 (morning decisions) must actually happen at 09:00** — five of the fifteen slice items sit behind those decisions.

## 5. Top Risks for Tomorrow Morning

1. **Shared `Data/` DbContext conflicts (High — the #1 risk in every doc).** Lanes A and B both touch it. Mitigate at 09:00: agree entity/`DbSet` shapes (decision 3), name **one** merge owner for `Data/`.
2. **Lane A overload.** 8 of 15 slice items on the Web+API pair. Pre-agree the cut line: E2-S3 (SQL-injection fix — or just delete the dead method), E8-S3 (`/health`), and any Blazor temptation go overboard first. E3+E4 (the demo's face) are never cut.
3. **First `azd up` fails — expected, 45-min window.** Don't wait for 15:30: lane C should run `azd provision` against the skeleton **mid-afternoon (~14:30)** so infra errors surface before the integration merge, leaving the deploy window for app wiring only.
4. **Managed Identity / SQL AAD friction.** Blocked until decision 4 (who owns the RG + AAD admin group) is answered. Prep `Authentication=Active Directory Default` + `DefaultAzureCredential` from the first EF Core connection string.
5. **Time sunk on non-issues.** Nobody installs the 4.6.1 targeting pack, fights MSB4226, or adds a WCF reference for CS0234. E0-S2 exists precisely to say this out loud. Estimated waste if ignored: 1–2 person-hours of a 32-person-hour day.

## 6. First 5 Decisions the Team Must Make (09:00, timeboxed 15 min)

| # | Decision | Blocks | Recommendation |
|---|---|---|---|
| 1 | Scoring: separate Container App or in-process module in Web? | E9-S2, E10-S1, `azure.yaml` | Separate app — cleaner demo of the WCF→API story; skip only if lane C is drowning |
| 2 | `azure.yaml` root: repo root or `src/ContosoInsurance/`? | E10-S1, all of lane C | Repo root — simplest `azd up` for the "new hire" signal |
| 3 | Shared `Data/` entity/`DbSet` shapes | E2-S1, E6-S2, both lanes A+B | Mirror the 4 existing tables exactly; no schema changes; write shapes in `wiki/log.md` |
| 4 | Who provisions the shared RG + SQL AAD admin group? | E10-S2, E7-S1 | Assign one named person **before** Sprint 1 starts |
| 5 | Token test project for the CI gate? | E11-S2, decision feeds E6-S1's fake | Yes — one trivial xUnit test, 10 minutes, makes the `dotnet test` gate meaningful |

(Decisions 6–8 from PRD §15 — `SearchByClaimant`, latent gaps, auth scope — can be made lazily; default answers: delete it, defer them, cookie auth.)

## 7. Recommended First Implementation Step

**E1-S1 via appmod:** after the 09:00 decision block and Assess runs (E0-S4), Apply the SDK-style/`net9.0` conversion — **`Common` first** (the only leaf project), then `Data` → `Services` → `Worker` → `Web`, reviewing every diff. This one story dissolves both environment prerequisites (MSB3644/MSB4226), unblocks all three lanes, and starts the 15-pt §1 clock. Expected: Common/Data/Services/Worker compile on .NET 9 before midday; Web intentionally red until E3/E4-S2.

## 8. Final P0 Checklist Before Coding

- [ ] Tools verified on all 4 machines: `dotnet 9.x`, `azd`, `az account show`, `docker`, appmod extension (E0-S1)
- [ ] Branches cut: `track/a-web-api`, `track/b-worker-storage`, `track/c-platform` (E0-S2)
- [ ] Baseline facts acknowledged aloud: MSB3644/MSB4226 = environment, CS0234 = the one code defect, eliminated by E4-S2 — no one "fixes" any of them directly (E0-S2)
- [ ] All 5 decisions above recorded (+ lazy defaults for 6–8) in `wiki/log.md` (E0-S3)
- [ ] Shared `Data/` shapes written down; single merge owner named (E0-S3.8)
- [ ] appmod Assess run per lane; plans skimmed (E0-S4)
- [ ] Commit convention agreed: one story per commit, ID in message (E12-S2)
- [ ] `docs/learnings.md` created and open in someone's editor (E12-S1)
- [ ] Cut lines pre-agreed: lane A drops E2-S3/E8-S3 first; lane C drops E11 first; E13 stretch only when core is green

---

> If reality diverges tomorrow, update the wiki and planning docs, log it in `wiki/log.md`, and re-run this check only if scope changes materially. Good hunting, Legacy Breakers.
