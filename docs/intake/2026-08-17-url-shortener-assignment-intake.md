---
source: docs/010 - Assignment - AI-Proficient Software Engineer 1.pdf
work_type: greenfield
ambiguity: ambiguous
risk: risk:medium
confirmed_by: the user, in conversation
date: 2026-08-17
---

## Source

`docs/010 - Assignment - AI-Proficient Software Engineer 1.pdf` — an interview assignment titled "Build an AI-Assisted Software Engineering System - URL Shortener". Text extracted with `pdftotext -layout`; the PDF is 2 pages, 66 lines of text.

The assignment asks for a URL shortener built from scratch with "core APIs, analytics, and reliability features", delivered over 2–3 days, demonstrating engineer-led execution accelerated by AI.

**Note on the invocation.** The skill was invoked with the path `doccs/010 - ...` (no extension). The actual file is `docs/010 - ... .pdf`. Recorded because the intake record should match what was actually read, not what was typed.

---

## Classification

### Work type: greenfield

The assignment states "build a URL shortener service **from scratch**" (§2). `src/` contains only a `.gitkeep` — there is no existing behaviour to preserve, and nothing to enhance, fix, or refactor.

**Why not enhancement:** the AIDLC system merged in PR #14 is tooling, not the product under test. Building the URL shortener does not modify it.

**On the assignment's other scenarios.** §3 lists brownfield scenarios and test/documentation improvements in scope, and §5 requires three demonstrated scenarios — greenfield, brownfield, ambiguous. Those are **downstream work that requires the greenfield system to exist first**: an enhancement, refactor or bug fix cannot be classified against a system that has not been built. Each will get its own `/work-intake` pass once there is a codebase to reason about. Confirmed with the user rather than assumed.

### Ambiguity: ambiguous

Applied the test — can an acceptance criterion be written now, from the text alone, without inventing anything? **No.** Undefined terms carrying real weight:

| Term | What is undefined |
|---|---|
| "core APIs" (§2) | Which operations. Create and resolve are implied; list, update, delete, bulk are not stated either way. |
| "analytics" (§2) | What is counted, at what granularity, retained how long, and whether per-visitor data is collected at all. |
| "reliability features" (§2) | Rate limiting, caching, idempotency, circuit breaking, and graceful degradation are all plausible readings. |
| short code | Length, charset, whether custom aliases are permitted, whether codes expire, and whether they must be unguessable. |
| — | No performance budget with a number, no scale target, no authentication or ownership model, no technology stack. |

Two honest readings produce materially different systems: one is a single-table CRUD service with a counter column; the other carries an event stream, a retention policy, and a rate-limited public redirect path. Both satisfy the text as written.

**Not judged generously.** Per this skill's own rule, the cost of calling well-defined work ambiguous is one extra pass; the cost of the reverse is a system built on unrecorded assumptions.

---

## Risk class: risk:medium

Argued **down** from high, with reasons, rather than up from low.

**Blast radius** — low. A local prototype, no users, nothing deployed, no consumers of any contract.

**Reversibility** — total. No migration against real data, no published contract, no emitted events. Reverting the branch removes it entirely.

**Sensitive surface** — present, and the reason this is not `risk:low`:

- A URL shortener is by definition a **redirect surface**. `STD-SEC-05` requires user-supplied destination URLs to be validated against an allowlist before any redirect; an unvalidated one makes this service a credible-looking hop for someone else's phishing.
- **Analytics implies collection and retention.** `STD-DATA-02` requires a stated retention for every new data class; `STD-SEC-04` prohibits user identifiers or PII in log arguments. If visitor IP addresses or user agents are counted, `STD-DATA-03` applies to any derived identifier.

This skill's rule is that anything touching security or personal data starts at `risk:high` and is argued down with a reason. The reason is that nothing is deployed and no real user data exists. That justifies medium, not low — the two surfaces above are precisely where the standards packs bite, and the assignment is explicitly evaluated on "validation and risk management rigor" (§6).

**Consequence:** `/gate-check` will require the engineer plus one named reviewer. Individual issues may carry their own risk labels — the redirect endpoint in particular is a candidate for `risk:high` at `/github-issues` time.

---

## Selected path

```
/requirements-refine (FULL mode)
  -> /solution-plan
  -> /github-issues  (with forward conformance against the four standards packs)
  -> per issue:
       /workflow-brainstorm  (MANDATORY - ambiguity modifier)
       /workflow-plan
       /workflow-execute     (TDD mode)
       /workflow-handover
       /workflow-review
       /gate-check
       /workflow-compound
```

**Ambiguity modifier applied:** `ambiguous` puts `/requirements-refine` in **full mode** and makes `/workflow-brainstorm` **mandatory**. Expect ASM entries with confidence levels, and be prepared for an ESCALATE where a requirement conflicts with a standard.

**Execution mode:** `TDD` — new behaviour, so red-green-refactor applies directly.

---

## Stages skipped

**None.**

Ambiguous greenfield is the deepest path in the system; every stage runs. This section is stated explicitly rather than omitted, because a skipped stage with a recorded reason is a decision and one with no record is an omission — and six months later nobody can tell them apart.

---

## Observations carried forward

**1. Part of the assignment is already delivered.** §4.4 ("AI-Assisted Execution") asks for traceability of generated/edited/rejected with rationale, quality gates spanning analysis/linting/tests/security/performance, human sign-off for high-impact changes, and explicit engineer ownership. §7 restates it as a principle. That machinery merged in PR #14: `/gate-check`, the four standards packs, the four review agents, and the `status:*` label lifecycle. Deliverable §5's "architecture overview" is substantially covered by `architecture/` and `CLAUDE.md`.

What is missing is the URL shortener that system is meant to be demonstrated on.

**2. `architecture/` was written prescriptively and is now an input, not an output.** It already commits to a four-layer model, relational storage, a BFF, and cryptographically random short codes — decisions made before any requirement was baselined, recorded at `architecture/README.md` §1.3. `/requirements-refine` should treat these as existing constraints to be reconciled against, and any conflict between a requirement and one of them is an ESCALATE, not a quiet correction.

**3. The assignment names a 2–3 day budget (§2).** That is a constraint on scope, not a requirement. It belongs in `/solution-plan` as a sequencing input — expect it to force an essential-versus-nice-to-have cut, and `/requirements-refine` should ask that question rather than assuming full scope.
