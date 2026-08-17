# Solution plan — URL shortener

**Date:** 2026-08-17
**Baseline:** [`requirements/baseline.md`](../../requirements/baseline.md) — 98 REQs, full scope
**Intake:** [`docs/intake/2026-08-17-url-shortener-assignment-intake.md`](../intake/2026-08-17-url-shortener-assignment-intake.md) — `greenfield` · `ambiguous` · `risk:medium`
**Requirements review:** PASS_WITH_AMENDMENT — no unresolved BLOCK

---

## Context

This plans the full baseline derived from the interview assignment: a URL shortener with full CRUD, analytics, and reliability features, built from scratch, demonstrated through three scenarios, under a 2–3 day budget.

**What shapes this plan more than anything else:** the baseline contains **6 product requirements and 92 process requirements**. The assignment specifies how the work must be done in far more detail than what must be built. Streams therefore split into *product* streams (WS-2…WS-5), *verification* (WS-6), and *demonstration and evidence* streams (WS-1, WS-7, WS-8) — and the last group is not overhead, it is most of the graded deliverable.

**Confirmed scope decisions carried in:**

| Decision | Answer | Source |
|---|---|---|
| API surface | Full CRUD + list | [ASM-001] |
| Authorization | Capability token on create; required for update and delete | [ASM-007] |
| Analytics | Counters + coarse daily buckets, no visitor data | [ASM-002], [ASM-017] |
| Reliability | All four: non-blocking analytics, rate limit, cache, health | [ASM-003] |
| Retention | 90 days for buckets, purged by scheduled job | [ASM-013] |
| Performance | Resolve < 50 ms p99; create < 200 ms p99 | [ASM-010] |
| Scale | 10,000 links, 100 rps, local only | [ASM-011], [ASM-012] |
| Stack | .NET + relational, matching `architecture/` | [ASM-008] |
| UI | **None — API only** | Step 5, this plan |
| Scenarios | Three distinct pieces of work | [ASM-016] |

**Research deviation, recorded.** Step 1 specifies two dispatched agents. Both targets — `docs/solutions/` and `src/` — are empty, as is `decisions/`, so agents would have returned nothing. Research was performed inline against `architecture/` and `standards/`, which are the documents that actually constrain this build. Recorded rather than skipped silently.

---

## Work Streams

| # | Stream | Covers | Depends on |
|---|---|---|---|
| **WS-1** | Execution discipline and traceability | REQ-007…017, 029…034, 037…058, 065…072, 093, 094, 096 | — (cross-cutting) |
| **WS-2** | Link creation and resolution | REQ-001, 002, 006; NFR-001, 002, 006, 007 | WS-1 |
| **WS-3** | Link management API and authorization | REQ-003, 060; NFR-008 | WS-2 |
| **WS-4** | Analytics | REQ-004; NFR-010, 011 | WS-2 |
| **WS-5** | Reliability and operability | REQ-005, 090, 092; NFR-003, 004, 005, 012, 013, 014 | WS-2, WS-4 |
| **WS-6** | Verification and quality gates | REQ-049…053, 059, 061, 062, 064, 088, 089, 091; NFR-009 | WS-2 |
| **WS-7** | Demonstration scenarios | REQ-018…028, 035, 036, 095 | WS-2…WS-6 |
| **WS-8** | Documentation and engineering summary | REQ-063, 073…087, 097, 098; NFR-015 | all |

---

### WS-1 — Execution discipline and traceability

**Covers:** REQ-007…017, REQ-029…034, REQ-037…058, REQ-065…072, REQ-093, REQ-094, REQ-096

**Outcomes**
- Every issue on the board carries its AI contributions recorded as generated, edited or rejected, each with a rationale.
- Every change passes five quality gates — analysis, lint, test, security, performance — and any one of them can fail it.
- No `risk:medium` or `risk:high` change reaches `status:done` without a named human recorded at `/gate-check`.
- Every decision where two viable approaches existed has a recorded rationale naming the rejected alternative.

**Notes.** The machinery is already built and merged (PR #14). This stream is about **operating and evidencing it**, not rebuilding it — [ASM-014] settles that reusing the tooling does not violate "from scratch". It is listed as a stream rather than assumed because REQ-037…058 are graded deliverables, and work that is assumed is work nobody evidences.

**Dependencies:** none. Cross-cutting — it applies to every other stream from the first commit.

---

### WS-2 — Link creation and resolution

**Covers:** REQ-001, REQ-002, REQ-006; NFR-001, NFR-002, NFR-006, NFR-007

**Outcomes**
- A submitted destination URL returns a short code, and requesting that code redirects to the destination.
- A redirect resolves in under 50 ms at p99.
- Link creation completes in under 200 ms at p99.
- A destination URL with a disallowed scheme or host is rejected at creation and never redirected to.
- Two concurrent creations never produce the same short code.

**Notes.** Codes are 7-character base62 from a CSPRNG ([ASM-004], mandated by `architecture/data.md` §1.4). No custom aliases ([ASM-005]). Optional expiry, with expired links returning `410 Gone` per `architecture/api.md` §3.4 ([ASM-006]). Uniqueness is enforced by a database constraint, not check-then-insert — `architecture/data.md` §1.5.

**NFR-006 is the defining security requirement of this system.** An unvalidated destination makes the service a phishing hop; it is owned here and tested in WS-6.

**Dependencies:** WS-1.

---

### WS-3 — Link management API and authorization

**Covers:** REQ-003, REQ-060; NFR-008

**Outcomes**
- A link's creator receives a capability token at creation and can read, update, delete and list using it.
- A caller without the correct token cannot update or delete a link, and receives `403`.
- The OpenAPI document is generated from the code and describes every operation.
- The capability token never appears in a response body after creation, in a log, or on a serializable property.

**Notes.** This stream carries the resolution of the `STD-SEC-01` escalation ([ASM-007]). The token is a secret, so `STD-SEC-03` applies to it — hence the fourth outcome. Routes follow `architecture/api.md` §2: `/v1/short-links`, kebab-case plural, no verbs.

**Dependencies:** WS-2 — there is nothing to manage until links exist.

---

### WS-4 — Analytics

**Covers:** REQ-004; NFR-010, NFR-011

**Outcomes**
- A link's lifetime click count is visible through the API.
- Daily click counts for the last 90 days are visible.
- No IP address, user agent, referrer or session identifier exists anywhere in the store.
- Buckets older than 90 days are removed by a scheduled process, demonstrably.

**Notes.** The third outcome is stated as an absence because that is the form it can be verified in — a schema inspection and a test that asserts no such column exists. [ASM-017] keeps this system out of the personal-data regime entirely, which is the single largest scope reduction in the baseline; it is worth protecting with a test rather than an intention.

**Dependencies:** WS-2.

---

### WS-5 — Reliability and operability

**Covers:** REQ-005, REQ-090, REQ-092; NFR-003, NFR-004, NFR-005, NFR-012, NFR-013, NFR-014

**Outcomes**
- The redirect path stays within its 50 ms p99 budget at 100 requests per second against 10,000 links.
- A total analytics failure does not fail, block, or measurably slow a redirect.
- Excessive link creation from one caller is rejected rather than served.
- The service reports its readiness through a health endpoint.
- Every failure path emits a structured log event, increments a metric, and has a stated alert condition.
- Every change states its rollback mechanism.

**Notes.** The second outcome is `STD-OPS-04` at `critical` severity, not a preference. It is the reason WS-5 depends on WS-4: the property cannot be demonstrated until there is an analytics write to fail.

**Dependencies:** WS-2, WS-4.

---

### WS-6 — Verification and quality gates

**Covers:** REQ-049…053, REQ-059, REQ-061, REQ-062, REQ-064, REQ-088, REQ-089, REQ-091; NFR-009

**Outcomes**
- Every acceptance criterion on every issue has at least one passing test asserting it.
- Unit and integration tests both exist and run in the pipeline.
- Static analysis, lint, test, security and performance gates run on every change and can fail it.
- Each identified threat — open redirect, token forgery, code enumeration — has a test asserting the attack fails.
- `review-architecture` and `review-security` raise no unwaived finding.

**Notes.** The fourth outcome is `STD-SEC-06`. The threats are enumerated here rather than in WS-2 so that one owner is accountable for the negative tests existing.

**Dependencies:** WS-2. Runs concurrently with WS-3…WS-5 rather than after them — tests are written per stream under TDD, and this stream owns the gates and the cross-stream coverage check.

---

### WS-7 — Demonstration scenarios

**Covers:** REQ-018…028, REQ-035, REQ-036, REQ-095

**Outcomes**
- A brownfield change to the built service is completed with its own intake, impact analysis, plan, execution, review and gate records.
- An ambiguous requirement is run through the pipeline and produces ASM entries with confidence levels.
- Each of the three scenarios shows its decomposition, its execution, and its validation.
- The impact analysis for the brownfield change names the modules, endpoints and data flows it touches.

**Notes.** [ASM-016] confirmed three distinct pieces of work, and REQ-095 — the "improve" the first requirements pass dropped — independently corroborates it. The greenfield scenario is WS-2…WS-6; this stream adds the other two.

**Dependencies:** WS-2…WS-6. A brownfield scenario needs real prior code, which is the whole point of REQ-035.

---

### WS-8 — Documentation and engineering summary

**Covers:** REQ-063, REQ-073…REQ-087, REQ-097, REQ-098; NFR-015

**Outcomes**
- A reader on a clean machine can run the service by following the setup instructions literally, with no undocumented step.
- The architecture overview states components, tools, execution approach, control flow and key decisions.
- The engineering summary states the plan and its rationale, the artifacts, the risks, trade-offs and validation, the assumptions, and the limitations.
- The limitations section names what was not built and what is not tested, without hedging.

**Notes.** NFR-015 is verified by execution, not review — an author who already has the environment cannot see the missing step. The third outcome is largely satisfiable from artifacts this pipeline already produces: the assumption register is REQ-086, the handovers carry risks and validation, and this plan is REQ-083.

**Dependencies:** all streams.

---

## Sequencing

```
WS-1 ─────────────────────────────────────────────────────  (cross-cutting, throughout)

WS-2 ──┬── WS-3 ──────────────────────┐
       │                              │
       ├── WS-4 ──── WS-5 ────────────┤
       │                              │
       └── WS-6 ──────────────────────┴── WS-7 ──── WS-8
```

**Acyclic.** Verified: no stream depends on one that depends on it. WS-1 has no dependencies and nothing depends on it as a prerequisite — it constrains rather than blocks.

**Critical path:** `WS-2 → WS-4 → WS-5 → WS-7 → WS-8`

WS-3 and WS-6 run parallel to WS-4/WS-5 and are not on the critical path. The path runs through WS-4 because `STD-OPS-04`'s non-blocking property (WS-5) cannot be demonstrated until an analytics write exists to fail.

### Seams where splitting could lose an edge case

| Seam | What could fall through | Owner assigned |
|---|---|---|
| **WS-2 / WS-3** | Destination URL validation. Creation lives in WS-3's CRUD surface; the redirect lives in WS-2. Each owner can reasonably assume the other validates. | **WS-2** owns NFR-006 at both ends; WS-3 calls into it. |
| **WS-4 / WS-5** | The non-blocking analytics write. WS-4 owns analytics, WS-5 owns the reliability property. WS-4 ships a synchronous write, WS-5 inherits a violation it did not create. | **WS-4** implements it non-blocking from the first commit; WS-5 verifies under failure injection. |
| **WS-2 / WS-4** | The latency budget once analytics joins the redirect path. WS-2's p99 is measured before WS-4 exists. | **WS-4** re-measures NFR-001 after its change; the budget is WS-2's but the regression risk is WS-4's. |
| **WS-6 / all** | Cross-stream AC coverage. Each stream writes its own tests under TDD, so nobody checks the whole. | **WS-6** owns the coverage check across streams. |

---

## Risks

| # | Risk | Mitigation |
|---|---|---|
| R-1 | **The 2–3 day budget cannot absorb 8 streams plus three scenarios.** | Sequence by the critical path and stop at a stream boundary, not mid-stream. WS-3 and WS-6 are parallel and can be thinned; WS-7's third scenario is the designated cut, taken as a documented limitation under REQ-081 rather than a silent omission. |
| R-2 | **Analytics on the redirect path breaks the 50 ms p99.** | Implement the analytics write as fire-and-forget behind a bounded queue from WS-4's first commit, and re-measure NFR-001 as WS-4's own exit criterion rather than WS-5's. |
| R-3 | **The capability token leaks** into a response, a log, or a serializable property, violating `STD-SEC-03`. | Model the token as a write-only type returned exactly once at creation; add a `review-security` negative test asserting it is absent from every other response and from log output. |
| R-4 | **Open redirect ships** because validation was assumed to live in the other stream. | Assign NFR-006 to WS-2 explicitly (see seams), and make the allowlist check a characterization-style test written before the redirect handler exists. |
| R-5 | **`architecture/` conflicts with an implementation choice** and requires a gated edit mid-build, blocking work. | Run `/architecture-guide` at `/workflow-plan` Step 3 for every issue touching `src/`, so conflicts surface at plan time when the plan can change, not at execution time when the code exists. |
| R-6 | **The brownfield scenario has nothing real to reason about** because WS-7 starts before the product is substantial enough. | Gate WS-7 on WS-2…WS-5 being merged, and choose the brownfield change from a limitation recorded in WS-8's documentation, so it targets something the codebase actually lacks. |
| R-7 | **Test coverage fragments across streams** and no acceptance criterion is checked end to end. | WS-6 owns a coverage check that runs against the issue list, not the test list — every AC on every closed issue must map to a passing test, verified at `/gate-check` criterion 2. |
| R-8 | **The retention purge is written but never demonstrated**, leaving `STD-DATA-02` satisfied on paper. | Make the purge's acceptance criterion an executed one: seed buckets older than 90 days, run the job, assert they are gone. |
| R-9 | **`architecture/frontend.md` governs a layer that will not exist**, since no UI is in scope. | Record as a known limitation in WS-8 under REQ-081, and note in the engineering summary that the document was written prescriptively before requirements existed. Do not silently delete it — `architecture/` is gated. |

---

## Open Decisions

**One consolidated table. All resolved before this plan was written.**

| Decision | Resolution | Owner role | Urgency |
|---|---|---|---|
| Is a UI in scope? | **No — API only.** The assignment names no interface; `architecture/frontend.md` will govern nothing, recorded as a limitation (R-9). | Product owner | Resolved — was blocking WS-8 and stream count |
| What is cut if the budget bites? | **Nothing confirmed in the baseline.** Absorb by keeping streams thin; WS-7's third scenario is the designated cut, documented. | Product owner | Resolved — was blocking sequencing |
| Authorization model | **Capability token on create**, required for update and delete. Satisfies `STD-SEC-01`; no waiver. | Security owner | Resolved at `/requirements-refine` |
| ASM-019 "production-grade" | Accepted: standards satisfied with no open waiver, `/gate-check` passed, `STD-OPS-02` observability present. | Engineer | Resolved |
| ASM-021 "disciplined prompting" | Accepted: evidenced by task artifacts carrying intent, constraints, acceptance criteria and technical context. | Engineer | Resolved |
| ASM-022 "secure AI usage" | Accepted: no secrets or personal data in prompts; security review before merge; no unapproved gated-path writes. | Security owner | Resolved |

**No open question remains in this document.** A plan with open questions in it is a draft that will be read as a plan.

---

## Review Findings

Step 6, run against the baseline after the plan was drafted.

### P1 — Coverage · **PASS**

Every REQ in the baseline maps to at least one stream. Verified by walking REQ-001 through REQ-098 against the Coverage Map below; no gap.

Two worth quoting because they were nearly missed:

> **REQ-095:** *"The service shall be improved after its initial build, not only completed."*
> **Plan, WS-7:** *"A brownfield change to the built service is completed with its own intake, impact analysis, plan, execution, review and gate records."*

REQ-095 was itself added by the requirements review as an R1 correction. Had it not been, WS-7 would have covered the three scenarios without anything requiring the product to be *improved* — a dropped requirement propagating one stage further.

> **REQ-006:** *"The service shall be built from scratch."*
> **Plan, WS-2:** covered, with [ASM-014] recording that the AIDLC tooling is not part of "from scratch".

### P2 — Traceability · **PASS**

Every stream traces to at least one REQ. One was examined closely and cleared:

> **WS-1** covers REQ-037…058, which the assignment calls the *"Critical Differentiator"*. The machinery already exists in PR #14, so a stream to "operate" it could look like invented scope.
> **Source, §4.4:** *"maintain traceability (generated/edited/rejected with rationale)"*

The requirement is that traceability be maintained during this work, not that the tooling be built. WS-1 traces to 40 REQs and is not invented scope. **Recorded because it was the most plausible candidate for it.**

### P3 — NFR attachment · **Major, one finding, fixed**

> **NFR-005:** *"A failure in the analytics path shall not fail, block, or measurably slow a redirect."*
> **Plan as first drafted:** NFR-005 attached to **WS-5** only.

WS-4 is the stream that *could violate* it — it writes the analytics record on the redirect path. Attaching the NFR only to WS-5 means WS-4 could ship a synchronous write and WS-5 would inherit a `critical` violation it did not create, discovering it during failure injection when the design is already set.

**Fixed:** NFR-005 is now owned jointly — WS-4 implements non-blocking from its first commit, WS-5 verifies under failure injection. Recorded in the seams table and in R-2.

All other applicable NFRs are attached to the stream that could violate them. The cost category is not-applicable per [ASM-012] and attaches to nothing, as recorded in `nfr.md`.

### P4 — Graph · **PASS**

The dependency graph is acyclic — verified by walking each stream's dependencies transitively. The critical path is stated: `WS-2 → WS-4 → WS-5 → WS-7 → WS-8`.

### P5 — Mitigations · **PASS**

All nine risks carry an action assignable to a stream owner. Two were rewritten during this check because the first draft restated the risk:

> **R-2, as first written:** *"Mitigation: monitor the p99 after adding analytics."*
> **R-2, as it now reads:** *"Implement the analytics write as fire-and-forget behind a bounded queue from WS-4's first commit, and re-measure NFR-001 as WS-4's own exit criterion."*

Monitoring is an observation. Re-measuring as an exit criterion is a gate someone owns.

### Overall verdict — **PASS**

One Major finding, fixed before this document was written. No blocker.

---

## Coverage Map

| REQ range | Stream |
|---|---|
| REQ-001, 002, 006 | WS-2 |
| REQ-003, 060 | WS-3 |
| REQ-004 | WS-4 |
| REQ-005, 090, 092 | WS-5 |
| REQ-007…017 | WS-1 |
| REQ-018…028 | WS-7 |
| REQ-029…034 | WS-1 |
| REQ-035, 036 | WS-7 |
| REQ-037…058 | WS-1 |
| REQ-049…053 | WS-6 *(quality gates — also WS-1 for the discipline)* |
| REQ-059, 061, 062, 064 | WS-6 |
| REQ-063 | WS-8 |
| REQ-065…072 | WS-1 |
| REQ-073…087 | WS-8 |
| REQ-088, 089, 091 | WS-6 |
| REQ-093, 094, 096 | WS-1 |
| REQ-095 | WS-7 |
| REQ-097, 098 | WS-8 |

| NFR | Stream |
|---|---|
| NFR-001, 002, 006, 007 | WS-2 |
| NFR-003, 004, 012, 013, 014 | WS-5 |
| NFR-005 | **WS-4 and WS-5** *(P3 finding)* |
| NFR-008 | WS-3 |
| NFR-009 | WS-6 |
| NFR-010, 011 | WS-4 |
| NFR-015 | WS-8 |
| Cost | not applicable — [ASM-012] |

**98 of 98 REQs covered. 15 of 15 NFRs attached. 8 of 8 streams trace to requirements.**
