# Requirements baseline

**Source:** `docs/010 - Assignment - AI-Proficient Software Engineer 1.pdf` — "Interview Assignment: Build an AI-Assisted Software Engineering System - URL Shortener". Two pages. Text extracted with `pdftotext -layout`.

**Intake:** [`docs/intake/2026-08-17-url-shortener-assignment-intake.md`](../docs/intake/2026-08-17-url-shortener-assignment-intake.md) — `greenfield` · `ambiguous` · `risk:medium`

**Depth mode:** FULL

**Classification key:** `F` functional · `NF` non-functional · `C` constraint

---

## A. The product — URL shortener

Source: §2 Scenario — *"You will build a URL shortener service from scratch with core APIs, analytics, and reliability features."*

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-001 | F | The service shall accept a destination URL and return a short code identifying it. | §2 "URL shortener service" |
| REQ-002 | F | The service shall resolve a short code to its destination URL and redirect the caller to it. | §2 "URL shortener service" |
| REQ-003 | F | The service shall expose its operations as APIs. | §2 "core APIs" |
| REQ-004 | F | The service shall record analytics about short-link usage. | §2 "analytics" |
| REQ-005 | F | The service shall provide reliability features. | §2 "reliability features" |
| REQ-006 | C | The service shall be built from scratch. | §2 "from scratch" |

> **REQ-003, REQ-004 and REQ-005 are not testable as written.** Each names a category without naming its contents. They are recorded here as the source states them, and pinned by [ASM-001], [ASM-002] and [ASM-003] respectively. The Step 3 review raises each under R4.

---

## B. Objective and execution model

Source: §1 Objective, §2 Scenario, §7 Expectation.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-007 | C | The work shall produce a working prototype. | §1 "Build a working prototype" |
| REQ-008 | C | The prototype shall transform a requirement into a reviewable engineering outcome. | §1 |
| REQ-009 | C | The work shall demonstrate requirement understanding. | §1 |
| REQ-010 | C | The work shall demonstrate task decomposition. | §1 |
| REQ-011 | C | The work shall demonstrate multi-step execution. | §1 |
| REQ-012 | C | The work shall demonstrate output generation. | §1 |
| REQ-013 | C | Execution shall be engineer-led and AI-accelerated, not autonomous orchestration. | §1 "engineer-led execution accelerated by AI, not autonomous orchestration" |
| REQ-014 | C | The work shall be completed over 2–3 days. | §2 "over 2-3 days" |
| REQ-015 | C | The work shall use AI assistance. | §2 "using AI assistance (Copilot/Claude/etc.)" |
| REQ-016 | C | The work shall demonstrate engineering judgment, evidenced by a recorded decision with its rationale and the alternatives rejected, at each point where two viable approaches existed. | §2, §6 |
| REQ-017 | C | The work shall be production-grade as defined by [ASM-019] — satisfying the four standards packs with no open waiver, passing the `/gate-check` criteria, and carrying the observability `STD-OPS-02` requires. | §7 |

---

## C. Scenario coverage

Source: §3 Scope, §5 Deliverables.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-018 | C | The work shall cover a greenfield scenario. | §3, §5 |
| REQ-019 | C | The work shall cover a brownfield enhancement. | §3 |
| REQ-020 | C | The work shall cover a brownfield refactor. | §3 |
| REQ-021 | C | The work shall cover a brownfield bug fix. | §3 |
| REQ-022 | C | The work shall cover a test improvement. | §3 "Test and documentation improvements" |
| REQ-023 | C | The work shall cover a documentation improvement. | §3 |
| REQ-024 | C | The work shall cover a well-defined requirement. | §3 |
| REQ-025 | C | The work shall cover an ambiguous requirement. | §3 |
| REQ-026 | C | Each of the three demonstrated scenarios shall show its decomposition. | §5 |
| REQ-027 | C | Each of the three demonstrated scenarios shall show its execution. | §5 |
| REQ-028 | C | Each of the three demonstrated scenarios shall show its validation. | §5 |

---

## D. Requirement understanding and decomposition

Source: §4.1, §4.2.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-029 | C | The work shall interpret the intent of a requirement. | §4.1 |
| REQ-030 | C | The work shall identify ambiguity in a requirement. | §4.1 |
| REQ-031 | C | The work shall normalize a requirement into a clear engineering problem. | §4.1 |
| REQ-032 | C | The work shall convert high-level requirements into actionable tasks. | §4.2 |
| REQ-033 | C | Tasks shall carry their dependencies. | §4.2 |
| REQ-034 | C | Tasks shall carry their sequencing. | §4.2 |

---

## E. Codebase reasoning (brownfield)

Source: §4.3.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-035 | C | The work shall identify modules, services, APIs and data flows impacted by a change. | §4.3 |
| REQ-036 | C | The work shall demonstrate architectural understanding of the system under change. | §4.3 |

---

## F. AI-assisted execution

Source: §4.4, described in the source as the *"Critical Differentiator"*.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-037 | C | AI shall be used for implementation. | §4.4 |
| REQ-038 | C | AI shall be used for debugging. | §4.4 |
| REQ-039 | C | AI shall be used for refactoring. | §4.4 |
| REQ-040 | C | AI shall be used for test generation. | §4.4 |
| REQ-041 | C | AI shall be used for documentation. | §4.4 |
| REQ-042 | C | AI shall be used for review preparation. | §4.4 |
| REQ-043 | C | Each task given to AI shall state its intent. | §4.4 |
| REQ-044 | C | Each task given to AI shall state its constraints. | §4.4 |
| REQ-045 | C | Each task given to AI shall state its acceptance criteria. | §4.4 |
| REQ-046 | C | Each task given to AI shall state its technical context. | §4.4 |
| REQ-047 | C | Prompting shall be disciplined and iteratively refined. | §4.4 |
| REQ-048 | C | Every AI contribution shall be recorded as generated, edited or rejected, with rationale. | §4.4 "maintain traceability (generated/edited/rejected with rationale)" |
| REQ-049 | C | A static analysis quality gate shall be applied. | §4.4 |
| REQ-050 | C | A linting quality gate shall be applied. | §4.4 |
| REQ-051 | C | A test quality gate shall be applied. | §4.4 |
| REQ-052 | C | A security quality gate shall be applied. | §4.4 |
| REQ-053 | C | A performance quality gate shall be applied. | §4.4 |
| REQ-054 | C | AI usage shall be secure. | §4.4 "enforce secure AI usage" |
| REQ-055 | C | High-impact changes shall require human sign-off. | §4.4 |
| REQ-056 | C | The engineer shall retain ownership of correctness. | §4.4 |
| REQ-057 | C | The engineer shall retain ownership of maintainability. | §4.4 |
| REQ-058 | C | The engineer shall retain ownership of production readiness. | §4.4 |

---

## G. Engineering output

Source: §4.5.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-059 | C | The work shall produce production-quality code. | §4.5 |
| REQ-060 | C | The work shall produce API and schema definitions. | §4.5 |
| REQ-061 | C | The work shall produce unit tests. | §4.5 |
| REQ-062 | C | The work shall produce integration tests. | §4.5 |
| REQ-063 | C | The work shall produce supporting documentation. | §4.5 |
| REQ-064 | NF | The design shall satisfy `standards/architecture.md` and the layer rules in `architecture/layers.md` with no unwaived violation raised by `review-architecture`. | §4.5, §6 |

---

## H. Validation, risk and oversight

Source: §4.6, §4.7.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-065 | C | The work shall identify risks. | §4.6 |
| REQ-066 | C | The work shall identify trade-offs. | §4.6 |
| REQ-067 | C | The work shall identify failure scenarios. | §4.6 |
| REQ-068 | C | The work shall define validation guardrails. | §4.6 |
| REQ-069 | C | The work shall define safety guardrails. | §4.6 |
| REQ-070 | C | The engineer shall lead execution. | §4.7 |
| REQ-071 | C | The engineer shall approve all outputs. | §4.7 |
| REQ-072 | C | AI shall assist within tasks rather than across them. | §4.7 |

---

## I. Deliverables

Source: §4.8, §5.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-073 | C | The prototype shall be runnable end to end. | §5 "Working prototype (runnable end-to-end)" |
| REQ-074 | C | An architecture overview shall state the components. | §5 |
| REQ-075 | C | An architecture overview shall state the tools. | §5 |
| REQ-076 | C | An architecture overview shall state the execution approach. | §5 |
| REQ-077 | C | An architecture overview shall state the control flow. | §5 |
| REQ-078 | C | An architecture overview shall state the key decisions. | §5 |
| REQ-079 | C | Setup instructions shall be provided. | §5 |
| REQ-080 | C | The testing approach shall be documented. | §5 |
| REQ-081 | C | Limitations shall be documented. | §5 |
| REQ-082 | C | Trade-offs shall be documented. | §5 |
| REQ-083 | C | A final engineering summary shall state the plan and its rationale. | §4.8 |
| REQ-084 | C | A final engineering summary shall list the artifacts produced. | §4.8 |
| REQ-085 | C | A final engineering summary shall state the risks. | §4.8 |
| REQ-086 | C | A final engineering summary shall state the assumptions made. | §4.8 |
| REQ-087 | C | A final engineering summary shall state the limitations. | §4.8 |

---

## J. Code quality attributes

Source: §6 Evaluation Criteria — *"modular, testable, reliable, secure, scalable code with safe change management"*.

| ID | Type | Requirement | Source |
|---|---|---|---|
| REQ-088 | NF | The code shall observe the layer boundaries and permitted dependency direction in `architecture/layers.md` §2, verified by `review-architecture` raising no unwaived violation. | §6 "modular" |
| REQ-089 | NF | Every acceptance criterion shall have at least one automated test asserting it, verified at `/gate-check` criterion 2. | §6 "testable" |
| REQ-090 | NF | The service shall meet NFR-001 and NFR-003 under the load stated in [ASM-011], and a failure in a non-critical path shall not degrade the redirect path. | §6 "reliable" |
| REQ-091 | NF | The code shall satisfy `standards/security.md` with no unwaived finding from `review-security`. | §6 "secure" |
| REQ-092 | NF | The service shall sustain the scale target in [ASM-011] — 10,000 links and 100 requests per second on resolve. | §6 "scalable" |
| REQ-093 | C | Every change shall state its rollback path naming the mechanism, and every schema change shall be additive and reversible per `STD-DATA-01`. | §6 "safe change management" |
| REQ-094 | C | Every decision shall be recorded with its rationale, and every AI-rendered verdict shall quote the rule applied and the text judged. | §6, §7 "defensible reasoning" |

---

## K. Added by the Step 3 review

The review pass found one dropped assertion (R1) and two compound requirements (R3) in the sections above. These four REQs are the corrections.

| ID | Type | Requirement | Source | Fixes |
|---|---|---|---|---|
| REQ-095 | C | The service shall be improved after its initial build, not only completed. | §2 "complete **and improve** it over 2-3 days" | **R1** — the word "improve" was dropped entirely from the first pass |
| REQ-096 | C | The work shall demonstrate output validation. | §1 "output generation/validation" | **R3** — split out of REQ-012, which joined generation and validation |
| REQ-097 | C | A final engineering summary shall state the trade-offs. | §4.8 | **R3** — split out of REQ-085 |
| REQ-098 | C | A final engineering summary shall state the validation performed. | §4.8 | **R3** — split out of REQ-085 |

REQ-095 is the substantive one. "Complete **and improve**" is the source's own instruction that this is not a single build, and it independently corroborates the ASM-016 decision to treat the three scenarios as distinct pieces of work.

---

## Source sections mapped without minting a new REQ

**§6 Evaluation Criteria is a rubric that indexes §4, not a separate requirement set.** Recorded as [ASM-015]. Each criterion maps to existing REQs:

| §6 criterion | Maps to |
|---|---|
| Effectiveness of AI-assisted engineering execution | REQ-037…REQ-058 |
| Architecture/system design quality | REQ-036, REQ-074…REQ-078 |
| Depth of decomposition and execution quality | REQ-032…REQ-034, REQ-011 |
| Realism/quality of outputs | REQ-059…REQ-063 |
| Validation and risk management rigor | REQ-065…REQ-069 |
| Clarity and defensibility of decisions | REQ-094 |
| Core engineering principles | REQ-088…REQ-093 |
| Engineering judgment | REQ-016 |

**§7 Expectation** restates §1 and §4.7. "Strong design fundamentals" → REQ-064, REQ-088. "Effective AI use as accelerator" → REQ-013, REQ-015. "Output ownership" → REQ-056…REQ-058, REQ-071. "Defensible reasoning" → REQ-094. "Production-grade" → REQ-017.

**The document classification footer** appearing on both pages is document metadata, not a requirement on the work. Recorded as [ASM-018]. Per `CLAUDE.md`, this repository is self-contained and does not reference any company or internal system, so the marking is deliberately not reproduced in these artifacts.

---

## Counts

| Type | Count |
|---|---|
| Functional (F) | 5 |
| Non-functional (NF) | 7 |
| Constraint (C) | 86 |
| **Total** | **98** |

Product requirements: 6 (REQ-001…REQ-006). Process and deliverable requirements: 92.

That ratio is a finding in itself: **this document specifies the engineering process far more precisely than it specifies the product.** The URL shortener is described in one sentence of eleven words; the way it must be built occupies the rest of the document.
