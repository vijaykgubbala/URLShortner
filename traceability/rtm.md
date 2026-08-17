# Requirements traceability matrix

**Baseline:** [`requirements/baseline.md`](../requirements/baseline.md) — 98 REQs
**Issue set:** #17–#41, created 2026-08-17 from [`docs/plans/2026-08-17-solution-plan.md`](../docs/plans/2026-08-17-solution-plan.md)
**Conformance:** [`validation/url-shortener-conformance.md`](../validation/url-shortener-conformance.md) · `standards@2026.08.1`

---

## Query 1 — Which REQs have no issue?

**37 of 98.** A non-zero count here is a finding, not a footnote, so every one is accounted for below.

All 37 are **process obligations the merged AIDLC pipeline discharges by being operated**. They are not buildable units of work; creating issues for them would manufacture a backlog out of policy already in force.

| REQs | What discharges them |
|---|---|
| REQ-007…REQ-018 | Objective and execution-model constraints. `/work-intake` classifies, `/workflow-plan` decomposes, `/workflow-execute` executes, `/gate-check` gates. Evidenced by the artifacts each stage writes. |
| REQ-032…REQ-034 | Task decomposition with dependencies and sequencing — `/workflow-plan` and `/workflow-execute` Step 3 `addBlockedBy`. |
| REQ-043…REQ-047 | Task definition carrying intent, constraints, acceptance criteria and technical context — the body shape mandated by `/github-issues` Step 3, visible in every issue #17–#41. |
| REQ-054…REQ-058 | Secure AI usage, human sign-off, engineer ownership — `/gate-check` risk-class approvals, ASM-020, ASM-022. |
| REQ-064…REQ-072 | Design conformance, risk identification, guardrails, engineer leads and approves — `/workflow-review` lenses and `/gate-check`. |
| REQ-093, REQ-094 | Safe change management and recorded decisions — `STD-DATA-01`, the ADR requirement, the evidence-before-verdict rule. |
| REQ-096 | Output validation — `/workflow-execute` Step 6 and `/gate-check` criterion 2. |

**This classification is a claim, not a proof.** A reviewer who disagrees should say so at `/gate-check`. The honest position: these REQs are satisfied by machinery already merged in #14, and the evidence is the artifacts rather than an issue.

### Gaps closed since the first pass

| REQ | Was | Now |
|---|---|---|
| REQ-022 | No issue — §3 puts test improvements in scope; the triage followed §5's three scenarios and dropped it | **#40** |
| REQ-023 | No issue — same, for documentation improvements | **#41** |
| REQ-088 | Attachment gap — #33 traced REQ-089 but not REQ-088 | **#33**, patched |

The REQ-022/REQ-023 gap came from §3 naming four scope categories while §5 requires three scenarios. The two sections do not agree and the triage silently followed §5 — the same class of error the requirements review caught with the dropped "and improve".

---

## Query 2 — Which issues trace to no REQ?

**Zero.** Every issue #17–#41 carries at least one REQ in its `Traces to` section, checked against live issue bodies rather than drafts.

An issue tracing to no requirement is invented scope and must not be created — `/github-issues` Step 3. None was.

---

## Coverage

61 of 98 REQs map to at least one issue.

| REQ | Issues |
|---|---|
| REQ-001 | #17 #18 #20 |
| REQ-002 | #17 #19 #20 |
| REQ-003 | #21 #22 #23 #24 #25 |
| REQ-004 | #27 #28 |
| REQ-005 | #29 #30 #31 #32 |
| REQ-006 | #18 |
| REQ-019 | #35 |
| REQ-020 | #35 |
| REQ-021 | #35 |
| REQ-022 | #40 |
| REQ-023 | #41 |
| REQ-024 | #36 |
| REQ-025 | #36 |
| REQ-026 | #35 #36 #40 #41 |
| REQ-027 | #35 #36 #40 #41 |
| REQ-028 | #35 #36 #40 #41 |
| REQ-029 | #36 |
| REQ-030 | #36 |
| REQ-031 | #36 |
| REQ-035 | #35 |
| REQ-036 | #35 |
| REQ-037 | #26 |
| REQ-038 | #26 |
| REQ-039 | #26 |
| REQ-040 | #26 |
| REQ-041 | #26 |
| REQ-042 | #26 |
| REQ-048 | #26 |
| REQ-049 | #33 |
| REQ-050 | #33 |
| REQ-051 | #33 |
| REQ-052 | #33 #34 |
| REQ-053 | #33 |
| REQ-059 | #33 |
| REQ-060 | #23 |
| REQ-061 | #33 |
| REQ-062 | #33 |
| REQ-063 | #39 |
| REQ-073 | #37 |
| REQ-074 | #38 |
| REQ-075 | #38 |
| REQ-076 | #38 |
| REQ-077 | #38 |
| REQ-078 | #38 |
| REQ-079 | #37 |
| REQ-080 | #39 |
| REQ-081 | #39 |
| REQ-082 | #39 |
| REQ-083 | #39 |
| REQ-084 | #39 |
| REQ-085 | #39 |
| REQ-086 | #39 |
| REQ-087 | #39 |
| REQ-088 | #33 |
| REQ-089 | #33 |
| REQ-090 | #29 |
| REQ-091 | #34 |
| REQ-092 | #31 |
| REQ-095 | #35 |
| REQ-097 | #39 |
| REQ-098 | #39 |

---

## Counts

| Measure | Value |
|---|---|
| REQs in baseline | 98 |
| REQs with at least one issue | **61** |
| REQs with no issue — discharged by pipeline operation | 37 |
| REQs with no issue — genuine gaps | **0** |
| Issues created | 25 (#17–#41) |
| Issues tracing to no REQ | **0** |
| Issues at `risk:high` | 5 (#17, #19, #21, #25, #29) |