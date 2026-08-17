# Assumption register

Every ambiguity found in the source carries an entry here with a **proposed default** and a **confidence level**, rather than being resolved silently.

> **An ambiguity resolved without a record in this file is a defect in `/requirements-refine`'s output.** This register exists to capture what the AI decided quietly, not only what it thought to ask about.

**Confidence key:** `high` — near-certain, recorded for visibility rather than interrogation. `medium` — defensible but a reasonable person could choose otherwise. `low` — genuinely undetermined; the default is a placeholder.

**Status key:** `confirmed` — the user accepted the proposed default. `overridden` — the user chose differently; **the original default is retained below**, so the change is auditable. `proposed` — not yet put to the user.

**Interrogation:** FULL mode. Fourteen entries were put to the user across four rounds; the two `high`-confidence entries were recorded for visibility rather than asked about.

---

## The product

### ASM-001 — What "core APIs" means
- **Ambiguity:** §2 requires "core APIs" without naming a single operation.
- **Proposed default:** create, resolve, read-metadata, list, delete. *(Confidence `low`)*
- **User decision:** **full CRUD plus list** — create, resolve, read, update, delete, list.
- **Status:** `overridden` — the default omitted update.
- **Affects:** REQ-003
- **Consequence:** update and delete are destructive operations on anonymous links, which forced the ASM-007 conflict to a decision rather than letting it pass.

### ASM-002 — What "analytics" means
- **Ambiguity:** §2 requires "analytics" without stating what is counted or at what granularity.
- **Proposed default:** per-link total click count and last-accessed timestamp only. *(Confidence `low`)*
- **User decision:** **counters plus a coarse daily time series.** Click counts bucketed by day. Still no visitor-attributable data.
- **Status:** `overridden` — the default had no time dimension.
- **Affects:** REQ-004, NFR-010, NFR-011
- **Consequence:** the daily bucket is a new persisted data class, so `STD-DATA-02` applies and ASM-013 becomes mandatory rather than optional.

### ASM-003 — What "reliability features" means
- **Ambiguity:** §2 requires "reliability features" without naming one.
- **Proposed default:** rate limiting on creation, read-through cache on resolve, health endpoint, non-blocking analytics writes. *(Confidence `low`)*
- **User decision:** **all four**, as proposed.
- **Status:** `confirmed`
- **Affects:** REQ-005, NFR-004, NFR-005
- **Note:** the non-blocking analytics write is not optional once analytics sits on the redirect path — `STD-OPS-04` requires it at `critical` severity.

### ASM-004 — Short code format and guessability
- **Ambiguity:** no length, character set, or guessability property stated.
- **Proposed default:** 7 characters, base62, from a cryptographically secure random source. *(Confidence `medium`)*
- **Status:** `confirmed` — not interrogated, and confidence raised to `high`, because the material half is already settled: `architecture/data.md` §1.4 mandates a CSPRNG and prohibits sequential or derivable codes. Only length and alphabet remained open, and 7 base62 characters yields ~3.5 × 10¹² codes, which is adequate at the confirmed scale (ASM-011).
- **Affects:** REQ-001

### ASM-005 — Custom aliases
- **Ambiguity:** the source does not say whether a caller may choose their own code.
- **Proposed default:** not supported. *(Confidence `medium`)*
- **User decision:** **not supported**, as proposed.
- **Status:** `confirmed`
- **Affects:** REQ-001
- **Rationale carried forward:** avoids a collision-and-reservation policy and an impersonation surface, neither of which the source asks for.

### ASM-006 — Link expiry
- **Ambiguity:** the source does not say whether links expire.
- **Proposed default:** optional expiry timestamp; links without one never expire. *(Confidence `medium`)*
- **User decision:** **optional expiry, purged by a scheduled process when passed.**
- **Status:** `confirmed`, with the purge mechanism made explicit.
- **Affects:** REQ-001, REQ-002
- **Consequence:** gives the `410 Gone` path in `architecture/api.md` §3.4 something real to return, and satisfies `architecture/data.md` §5.2 without a gated edit.

### ASM-007 — Authentication and ownership *(was an ESCALATE candidate — now resolved)*
- **Ambiguity:** the source states no authentication model and never says who may create, read, or delete a link.
- **Proposed default:** no authentication; all operations anonymous. *(Confidence `low`)*
- **The conflict:** `STD-SEC-01` requires every story that reads or changes data to state who is permitted to perform the action, at `critical` severity. Anonymous delete means anyone can destroy anyone's link.
- **First user answer** was "no destructive operations", which contradicted the "full CRUD" answer to ASM-001. **The contradiction was surfaced rather than reconciled silently**, and put back to the user.
- **User decision:** **full CRUD with a capability token.** Creation returns a secret management token; update and delete require it.
- **Status:** `overridden`
- **Affects:** REQ-001, REQ-003, NFR-008
- **Outcome: `STD-SEC-01` is satisfied, not waived.** No waiver is required and none is recorded. The token is a secret, so `STD-SEC-03` now applies to it — see NFR-008.

### ASM-013 — Analytics retention period
- **Ambiguity:** no retention stated for any collected data.
- **Proposed default:** 90 days for analytics; link records until deleted or expired. *(Confidence `low`)*
- **User decision:** **90 days for daily buckets, purged by a scheduled job.** Lifetime click totals retained with the link.
- **Status:** `confirmed`, with the purge mechanism made explicit.
- **Affects:** REQ-004, NFR-011

### ASM-017 — Visitor data in analytics
- **Ambiguity:** whether analytics captures anything attributable to a visitor.
- **Proposed default:** no. No IP address, user agent, referrer, or session identifier. *(Confidence `medium`)*
- **User decision:** **confirmed** — the daily time series is aggregate only.
- **Status:** `confirmed`
- **Affects:** REQ-004, NFR-010
- **Consequence: this system does not process personal data.** `STD-SEC-04` and `STD-DATA-03` are therefore not engaged. That is the single largest scope reduction in this baseline, and it was a decision rather than an omission.

---

## Non-functional

### ASM-010 — Performance budget
- **Ambiguity:** no latency or throughput figure anywhere in the source.
- **Proposed default:** resolve < 50 ms p99, create < 200 ms p99. *(Confidence `low`)*
- **User decision:** **confirmed as proposed.**
- **Status:** `confirmed`
- **Affects:** NFR-001, NFR-002, REQ-053
- **Note:** tight enough that the read-through cache must earn its place; `STD-OPS-01` is satisfied by a real number at a named percentile.

### ASM-011 — Scale and volume
- **Ambiguity:** no scale target stated.
- **Proposed default:** 10,000 links, 100 rps on resolve. *(Confidence `low`)*
- **User decision:** **confirmed as proposed.**
- **Status:** `confirmed`
- **Affects:** REQ-092, NFR-003
- **Consequence:** REQ-092 ("scalable") becomes falsifiable against a stated target instead of being an adjective.

### ASM-012 — Deployment target
- **Ambiguity:** the source never says where this runs.
- **Proposed default:** local only; no hosted environment. *(Confidence `medium`)*
- **User decision:** **confirmed** — local only, no container deployment.
- **Status:** `confirmed`
- **Affects:** REQ-073, cost NFR
- **Consequence:** the cost NFR category remains legitimately not-applicable. If this ever changes, that category reopens.

### ASM-008 — Technology stack
- **Ambiguity:** the source names no language, framework, or datastore.
- **Proposed default:** unresolved; defer to `/workflow-brainstorm`. *(Confidence `low`)*
- **User decision:** **.NET with relational storage, matching what `architecture/` already implies.**
- **Status:** `overridden` — resolved here rather than deferred.
- **Affects:** REQ-059, REQ-060
- **Consequence:** avoids a conflict with the gated `architecture/` documents, which already assume project-per-layer references, `Task`-returning methods with cancellation tokens, and a relational store. Deferring would have surfaced a conflict the brainstorm could not resolve without a gated edit.

---

## Scope and interpretation

### ASM-009 — Whether "2–3 days" is testable
- **Proposed default:** a scope-cutting input to `/solution-plan`, not an acceptance criterion. *(Confidence `medium`)*
- **Status:** `confirmed` — not interrogated. The ASM-016 answer (three distinct pieces of work) settles it: the budget functions as a planning constraint, which is what the default said.
- **Affects:** REQ-014

### ASM-014 — What "from scratch" excludes
- **Proposed default:** the AIDLC system is tooling; "from scratch" applies to the URL shortener, which has no existing code. *(Confidence `high`)*
- **Status:** `confirmed` — recorded for visibility, not interrogated.
- **Affects:** REQ-006

### ASM-015 — Whether §6 is a requirement set or a rubric
- **Proposed default:** a rubric indexing §4; map rather than duplicate, minting new REQs only for the code quality attributes §4 omits. *(Confidence `medium`)*
- **Status:** `confirmed` — not interrogated. The mapping is visible in `baseline.md` and can be overturned by reading it.
- **Affects:** the coverage map

### ASM-016 — Whether the three scenarios are separate work
- **Ambiguity:** §5 does not say whether the three scenarios are distinct work or narratives over one build.
- **Proposed default:** three distinct pieces of work. *(Confidence `medium`)*
- **User decision:** **confirmed** — greenfield build, then a real brownfield change, then an ambiguous requirement, each through the full pipeline.
- **Status:** `confirmed`
- **Affects:** REQ-018…REQ-028, REQ-035
- **Consequence:** roughly 40% more scope against the 2–3 day budget, and the only reading under which REQ-035 (identify impacted modules and data flows) has real prior code to reason about.

### ASM-018 — The document classification footer
- **Proposed default:** document metadata, not a requirement; not reproduced, because `CLAUDE.md` requires this repository to be self-contained. *(Confidence `high`)*
- **Status:** `confirmed` — recorded for visibility, not interrogated.

---

## Added during the Step 3 review

The review pass found four terms carrying real weight with no assumption behind them. Each is an R5 blocker — an unclear term with no ASM means a decision was made silently.

### ASM-019 — What "production-quality" and "production-grade" mean
- **Ambiguity:** §4.5 requires "production-quality code" and §7 says "treat as production-grade engineering work", with no definition of either. The terms gate REQ-017 and REQ-059 and cannot be tested as written.
- **Proposed default:** production-grade means satisfying the four standards packs without an open waiver, passing the `/gate-check` criteria, and carrying the observability required by `STD-OPS-02`. It does **not** mean deployed, load-tested at production volume, or operated.
- **Confidence:** `medium`
- **Affects:** REQ-017, REQ-059
- **Status:** `proposed`

### ASM-020 — What counts as a "high-impact change"
- **Ambiguity:** §4.4 requires human sign-off for "high-impact changes" without defining the threshold. This one decides when a human is obliged to be involved, so leaving it undefined defeats the control.
- **Proposed default:** the existing `risk:` label taxonomy. `risk:high` requires written sign-off with a name and timestamp; `risk:medium` requires a named reviewer; `risk:low` is self-approved and recorded. This is already implemented in `/gate-check`.
- **Confidence:** `high`
- **Affects:** REQ-055
- **Status:** `proposed`

### ASM-021 — What "disciplined prompting" means
- **Ambiguity:** §4.4 requires "disciplined prompting with iterative refinement" with no definition, and no observable outcome distinguishes it from undisciplined prompting.
- **Proposed default:** every task given to AI states intent, constraints, acceptance criteria and technical context (REQ-043…REQ-046), and every AI contribution is dispositioned (REQ-048). Discipline is evidenced by those artifacts existing, not by prompt text.
- **Confidence:** `medium`
- **Affects:** REQ-047
- **Status:** `proposed`

### ASM-022 — What "secure AI usage" means
- **Ambiguity:** §4.4 requires "enforce secure AI usage" with no definition.
- **Proposed default:** no secret, credential, or personal data is placed in a prompt; AI-generated code is reviewed against `standards/security.md` before merge (which `review-security` performs); no AI tool is granted write access to a gated path without in-conversation approval.
- **Confidence:** `medium`
- **Affects:** REQ-054
- **Status:** `proposed`

---

## Register summary

| Confidence | Count | IDs |
|---|---|---|
| `high` | 4 | ASM-004, ASM-014, ASM-018, ASM-020 |
| `medium` | 10 | ASM-005, ASM-006, ASM-009, ASM-012, ASM-015, ASM-016, ASM-017, ASM-019, ASM-021, ASM-022 |
| `low` | 8 | ASM-001, ASM-002, ASM-003, ASM-007, ASM-008, ASM-010, ASM-011, ASM-013 |
| **Total** | **22** | |

| Status | Count |
|---|---|
| `confirmed` | 13 |
| `overridden` | 5 |
| `proposed` (added at review, not yet put to the user) | 4 |

**Five defaults were overridden.** ASM-001, ASM-002, ASM-007, ASM-008 and ASM-013 all came back differently from what the AI proposed — which is the evidence that the interrogation did work rather than confirming its own suggestions.
