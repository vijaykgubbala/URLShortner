# Forward conformance — URL shortener issue set

**Standards version in force:** `standards@2026.08.1`
**Read from:** `standards/VERSION`
**Date:** 2026-08-17
**Scope:** 23 drafted issues from [`docs/plans/2026-08-17-solution-plan.md`](../docs/plans/2026-08-17-solution-plan.md), all eight work streams
**Checked before creation**, per `/github-issues` Step 4.

> `/gate-check` criterion 5 blocks on a conformance record written against a superseded `standards/VERSION`. If the packs are edited, this record is stale and the issues must be re-checked.

---

## Method

Each drafted issue was checked against every rule in the four packs, using each rule's **Detection hint** field to decide applicability. A rule that does not fire on an issue is not reported for that issue.

Every finding quotes the rule text and the issue text before the verdict.

---

## Findings

### F-01 — `STD-ARCH-06` · ISS-15 · **PASS_WITH_AMENDMENT** → applied

> **Rule** (`standards/architecture.md`, `STD-ARCH-06` — *A new integration pattern requires an ADR*):
> *"Introducing a transport, persistence technology, messaging pattern, or third-party integration style not already used in this repo requires an accepted `ADR-nnn` in `decisions/` before the code merges."*
>
> **Detection hint match:** *"A new package dependency providing transport, queuing, caching, or storage"* — ISS-15 introduces caching, which is not currently used anywhere in this repository.
>
> **Issue text, ISS-15 as drafted:** *"Given a code resolved once, when it is resolved again, then the second resolution does not query the database."*

**Verdict: PASS_WITH_AMENDMENT.** The issue introduces a caching pattern with no ADR requirement stated, so the pattern would land by implementation rather than by decision.

**Amendment applied** — added to ISS-15 Technical Notes, verbatim:

> **An ADR is required before merge** — `STD-ARCH-06` treats a new caching pattern as an integration pattern requiring a recorded decision.

**Re-checked:** PASS.

---

### F-02 — `STD-DATA-06` · ISS-06 · **PASS_WITH_AMENDMENT** → applied

> **Rule** (`standards/data.md`, `STD-DATA-06` — *Collection reads are bounded*):
> *"Every query returning a collection must impose a maximum row count at the query, not by discarding rows after they are fetched."*
>
> **Detection hint match:** *"A repository or query method returning a collection type with no limit, take, page-size, or top clause"* — ISS-06 lists the link collection.
>
> **Issue text, ISS-06 as drafted:** the list behaviour originally carried no page-size criterion.

**Verdict: PASS_WITH_AMENDMENT.** An unbounded list is correct against 10 links and takes the process down against 10,000 — which is the confirmed scale target in ASM-011.

**Amendments applied** — two acceptance criteria added to ISS-06, verbatim:

> - [ ] Given a collection request with no page size, when it is served, then a default page size is applied and the response states it.
> - [ ] Given a collection request with a page size above the maximum, when it is served, then the maximum is applied rather than the requested value.

**Re-checked:** PASS.

---

### F-03 — `STD-OPS-02` · nine issues · **PASS_WITH_AMENDMENT** → applied

> **Rule** (`standards/operability.md`, `STD-OPS-02` — *Logs, a metric, and an alert are acceptance criteria*):
> *"A story introducing a new behaviour or failure path must include acceptance criteria for its structured log events, its metric, and the alert condition — not a follow-up issue for them."*
>
> **Detection hint match:** *"A story adding an endpoint, background job, integration, or failure path whose acceptance criteria mention no log event, no metric name, and no alert threshold."*

Fired on ISS-01, ISS-02, ISS-03, ISS-04, ISS-08, ISS-09, ISS-12, ISS-13, ISS-14 — every issue adding an endpoint, a job, or a failure path.

**Verdict: PASS_WITH_AMENDMENT.** Observability deferred to a follow-up is observability that arrives after the first incident it was needed for.

**Amendments applied** — an observability criterion naming both the log event and the metric was added to each. Example, ISS-03, verbatim:

> - [ ] Given a resolve failure, when it occurs, then a structured log event `redirect.resolve.failed` is emitted and the `redirect_failures` counter increments.

**Re-checked:** PASS.

**A judgment recorded with this finding.** The rule requires an alert condition as well as a log and a metric. Adding three separate criteria to nine issues would push several past five acceptance criteria — the split signal in `/github-issues` Step 2. The log and metric were combined into one criterion, and an explicit alert criterion was added only to **ISS-13**, where the alert is the actual control:

> - [ ] Given an analytics failure rate above its threshold, when it persists, then a stated alert condition fires.

**Rationale:** the split signal is *"ACs describing unrelated behaviours"*. Observability of a behaviour is not unrelated to that behaviour, so the signal does not properly fire here. This is a deliberate reading of the rule rather than a silent shortcut, and a reviewer may overturn it. **Alert conditions for the remaining eight issues are therefore carried by ISS-16's telemetry work rather than individually — which is closer to "follow-up" than the rule prefers, and is the weakest point in this conformance pass.**

---

### F-04 — `STD-OPS-01` · six issues · **PASS**

> **Rule** (`standards/operability.md`, `STD-OPS-01` — *Every story carries a performance budget with a number*):
> *"A story touching a request path must state a latency or throughput budget as a numeric value at a named percentile."*

Fired on ISS-02, ISS-03, ISS-06, ISS-08, ISS-09, ISS-11, ISS-15. Numeric budgets were present as drafted, inherited from NFR-001 and NFR-002.

> **Issue text, ISS-03:** *"Given the redirect path, when measured at 100 requests per second against 10,000 links, then it resolves in under 50 ms at p99."*

**Verdict: PASS.** No amendment needed.

**Note:** ISS-06, ISS-08 and ISS-09 do not carry an explicit numeric budget. They are read and mutation paths on the management API, not the hot path, and NFR-001/NFR-002 do not cover them. **This is a gap the packs do not resolve** — `STD-OPS-01` says "a story touching a request path", which these do. Recorded rather than papered over; see Open Items.

---

### F-05 — `STD-SEC-01` · five issues · **PASS**

> **Rule** (`standards/security.md`, `STD-SEC-01` — *Authorization is decided at story level*):
> *"Every story that reads or changes data must state in its acceptance criteria who is permitted to perform the action and what happens when someone else attempts it."*

Fired on ISS-05, ISS-06, ISS-08, ISS-09, ISS-11.

> **Issue text, ISS-09:** *"Given a missing or incorrect token, when delete is called, then `403` is returned and the link still resolves."*

**Verdict: PASS.** Each mutating issue states both the permitted actor and the rejection case. The read issues (ISS-06, ISS-11) are anonymous by design per ASM-007, and state it in Out of Scope rather than leaving it unaddressed.

**This rule passed because the escalation was resolved at `/requirements-refine` rather than deferred.** Had ASM-007 been left open, every mutation issue would have carried a BLOCK here.

---

### F-06 — `STD-SEC-05` · three issues · **PASS**

> **Rule** (`standards/security.md`, `STD-SEC-05` — *Destination URLs are allowlisted before any redirect*):
> *"A user-supplied or stored destination URL must be validated against an allowlist of permitted schemes and hosts immediately before issuing a redirect or server-side request."*

Fired on ISS-01, ISS-03, ISS-08.

> **Issue text, ISS-03:** *"Given a stored destination the allowlist policy now rejects, when the code is requested, then the redirect is refused with `410` and no `Location` header is sent."*

**Verdict: PASS.** The rule says *immediately before issuing a redirect*, not only at creation — ISS-03 validates at resolve time, not merely trusting what ISS-02 stored.

---

### F-07 — `STD-SEC-03` · ISS-05 · **PASS**

> **Rule** (`standards/security.md`, `STD-SEC-03` — *Secrets are not serializable and not debugger-visible*):
> *"No property holding a credential, token, key, or connection string may be serializable, logged by a default formatter, or exposed on a type's public surface."*
>
> **Detection hint match:** *"A property or field whose name contains … `token` …"*
>
> **Issue text, ISS-05:** *"Given any response other than the original create, when it is returned, then it does not contain the token."* and *"Given the application's logs, when a mutation is processed, then the token does not appear in any log argument."*

**Verdict: PASS.**

---

### F-08 — `STD-DATA-02` · ISS-12 · **PASS**

> **Rule** (`standards/data.md`, `STD-DATA-02` — *Retention is stated for every new data class*):
> *"A story collecting or persisting a class of data not already stored must state how long it is kept and what removes it."*
>
> **Issue text, ISS-12:** *"Given buckets older than 90 days, when the scheduled purge runs, then those rows are removed."*

**Verdict: PASS.** Both halves present — the period and the mechanism. The additional criterion requiring the purge be demonstrated by seeding old buckets is what stops this being satisfied on paper.

---

### F-09 — `STD-DATA-01` · ISS-02, ISS-12 · **PASS**

> **Rule** (`standards/data.md`, `STD-DATA-01` — *Schema changes are additive and reversible*):
> *"A schema change must be additive — new nullable column, new table, new index — and must not drop or rename an in-use column in the same release that stops writing to it."*

Both issues create new tables in a system with no prior schema, so nothing is dropped or renamed. Stated in Technical Notes on both.

**Verdict: PASS.**

---

### F-10 — `STD-SEC-06` · ISS-18 · **PASS**

> **Rule** (`standards/security.md`, `STD-SEC-06` — *Every identified threat has a negative test*):
> *"Each threat named in a story, ADR, or review finding must be paired with an automated test asserting that the attack fails."*
>
> **Issue text, ISS-18:** covers open redirect, token forgery and code enumeration, plus a standing criterion for threats named later.

**Verdict: PASS.**

---

### F-11 — `STD-OPS-04` · ISS-13 · **PASS**

> **Rule** (`standards/operability.md`, `STD-OPS-04` — *A non-critical failure must not degrade the critical path*):
> *"A failure or timeout in an analytics, notification, logging, or other non-critical dependency must not fail, block, or measurably slow the primary user operation."*
>
> **Issue text, ISS-13:** *"Given the analytics store is unavailable, when a redirect is requested, then it is served successfully."*

**Verdict: PASS.** Owned by the stream that writes analytics, not the stream that would suffer the breach — the correction the solution plan's P3 review made.

---

### F-12 — `STD-ARCH-03`, `STD-ARCH-04` · endpoint issues · **PASS**

> **`STD-ARCH-03`** — *"Every HTTP route must begin with a version prefix and use kebab-case plural nouns, with no verbs in the path."*

All drafted routes are `/v1/short-links` and `/v1/short-links/{code}`. The public redirect is `GET /{code}`, which carries no version prefix — **permitted**, because `architecture/api.md` §1.4 defines it as a dedicated public redirect endpoint that is explicitly not the domain API.

> **`STD-ARCH-04`** — *"The OpenAPI document must be produced from the running application's types and route metadata, and must not be edited by hand."*

ISS-07 states it as an acceptance criterion, including the negative: no hand-maintained file committed.

**Verdict: PASS on both.**

---

### F-13 — `STD-ARCH-02` · ISS-01 · **PASS**

> **Rule** (`standards/architecture.md`, `STD-ARCH-02` — *Domain rules live in the domain layer*):
> *"Business rules, invariants, and validation of domain meaning must be implemented in the domain layer and never in a controller, endpoint handler, or view model."*
>
> **Issue text, ISS-01 Technical Notes:** *"Policy lives in the domain layer per `architecture/layers.md` §3.2 — it is a rule about what a valid short link is, not request-shape validation."*

**Verdict: PASS.** The distinction is drawn explicitly, which is what stops the allowlist being implemented as a controller `if`.

---

## Rules that did not fire

| Rule | Why not |
|---|---|
| `STD-ARCH-01` (dependency direction) | No issue states a project reference; enforced at review, not story level |
| `STD-ARCH-05` (BFF invariants) | No BFF — API-only scope confirmed at `/solution-plan` |
| `STD-SEC-02` (trust-boundary validation) | Fires at code review; no drafted issue defers validation |
| `STD-SEC-04` (no PII in logs) | ASM-017 confirmed no visitor-attributable data is collected at all |
| `STD-OPS-03` (rollback path) | Applies per change at handover, not per story |
| `STD-OPS-05` (feature flag) | No issue changes an existing endpoint's behaviour; all are new |
| `STD-OPS-06` (timeout and retry) | No outbound calls in scope; the cache is in-process |
| `STD-DATA-03` (derived identifier retention) | No derived identifier — ASM-017 |
| `STD-DATA-04` (migration backfill) | No migration reinterprets existing rows; all are new tables |
| `STD-DATA-05` (no cross-boundary copy) | No other system's data is stored |

---

## Summary

| Verdict | Count |
|---|---|
| **PASS** | 10 findings |
| **PASS_WITH_AMENDMENT** | 3 findings — all amendments applied and re-checked to PASS |
| **BLOCK** | 0 |
| **ESCALATE** | 0 |

**No issue carries a BLOCK or ESCALATE verdict. All 23 may be created.**

---

## Open items

Two things this pass could not close. Neither blocks creation; both are recorded so they are decisions rather than omissions.

**O-1 — `STD-OPS-01` on the management API.** ISS-06, ISS-08 and ISS-09 touch request paths and carry no numeric budget, because NFR-001 and NFR-002 cover only the create and resolve paths. The rule as written applies to them; the baseline supplies no figure. Either the NFRs need extending or the rule needs a scope qualifier. **Owner role: engineer, at `/workflow-plan` time.**

**O-2 — alert conditions (see F-03).** Eight issues carry log and metric criteria but no alert criterion, with alerting consolidated into ISS-16. `STD-OPS-02` asks for all three per story and explicitly rejects follow-up work. **This is the weakest point in this conformance pass and a reviewer may reasonably overturn it. Owner role: engineer plus the reviewer at `/gate-check`.**

---

## Appendix — issues added after the first pass

**#40** *Demonstrate a test improvement end to end* and **#41** *Demonstrate a documentation improvement end to end*, created to close the REQ-022 and REQ-023 gaps the traceability matrix surfaced.

**Verdict: PASS on all four packs. No rule fires on either.**

Checked and cleared, with the reason each did not fire:

| Rule | Why it does not fire |
|---|---|
| `STD-OPS-01` (numeric performance budget) | Neither issue touches a request path. Both are demonstration scenarios over existing behaviour. |
| `STD-OPS-02` (logs, metric, alert as ACs) | Detection hint requires a story adding an endpoint, background job, integration, or failure path. Neither adds one. |
| `STD-SEC-01` (authorization at story level) | Neither reads nor changes data. |
| `STD-DATA-01`, `STD-DATA-02`, `STD-DATA-06` | No schema change, no new data class, no collection read. |
| `STD-ARCH-*` | No project reference, route, or layer placement stated. |

**#33** was patched to attach REQ-088 alongside REQ-089. No conformance impact — a traceability attachment, not a change to the work.
---

## Post-creation amendment — #17, found at brainstorm

**Not found by this conformance pass.** Recorded here so the record matches the board.

`/workflow-brainstorm 17` found that #17's acceptance criteria specified `400` where `architecture/api.md` §4.5 requires `422` for a domain rule violation, and `architecture/layers.md` §3.2 names "permitted destination" as a domain rule by name. The criteria were amended.

**Why this pass could not have caught it.** Forward conformance checks drafted issues against the four packs in `standards/`. The rule violated lives in `architecture/`, which this step never reads. **The error contract, the layer model, the data model and the API shape are unenforced at story time.**

This is a gap in the pipeline rather than in this conformance run. It is a candidate for `/workflow-compound`'s graduation question — a missing rule, not a lesson: either `/github-issues` Step 4 should also check `architecture/`, or the architecture reference should be represented by rules in `standards/architecture.md` that carry detection hints.

**Owner role:** engineer, at `/workflow-compound`.