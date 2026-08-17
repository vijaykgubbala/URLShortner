---
name: gate-check
description: Evaluate whether an issue is allowed to proceed past review, against traceability, validation, and review findings. Use before merging, before opening a PR, or whenever someone asks whether something is ready to ship.
disable-model-invocation: true
argument-hint: "[issue number]"
---

# gate-check

## The doctrine

**This gate can fail on its own. Only a named human can pass it, or waive it in writing.**

That asymmetry is the whole design: **automated checks are trusted to say no and never trusted to say yes.**

A check that can pass a gate by itself is a check that will eventually pass it wrongly, silently, at scale — and nobody will be accountable, because nobody decided. A check that can only fail can be wrong in one direction, and being wrong in that direction costs an argument rather than an incident.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running.

---

## Risk class determines who must approve

Read the issue's risk label:

| Label | Approval required |
|---|---|
| `risk:low` | Engineer self-approval, **recorded** |
| `risk:medium` | Engineer **plus one named reviewer** |
| `risk:high` | Engineer, named reviewer, **and explicit written sign-off** — all three recorded with **name and timestamp** |

Self-approval at `risk:low` is still a record: a name and a timestamp, not an absence.

---

## Blocking criteria

**Evaluated in order. Any one failing blocks.**

### 1. Unaddressed critical finding
Any critical review finding in `docs/reviews/YYYY-MM-DD-<issue>.json` whose disposition is not `addressed`.

### 2. Unproven acceptance criterion
Any acceptance criterion on the issue with no passing test asserting it.

### 3. Uncovered requirement
Any requirement the issue traces to that has no test coverage at all.

### 4. Untraceable issue
Any issue with no REQ in its `Traces to` line.

### 5. Stale conformance record
Any conformance record in `validation/` written against a superseded `standards/VERSION`. Compare the version in the record against the current contents of `standards/VERSION`.

### 6. Missing approvals
Approvals absent for the issue's risk class, per the table above.

### 7. Mode-specific
Read the execution mode from the handover, then check the one that applies:

| Mode | Blocks if |
|---|---|
| `characterization` | A pre-existing test assertion was modified |
| `mutation-verified` | Any test was committed without a recorded mutation proof |
| `documentation` | The instructions were not executed |
| `TDD` | — no additional criterion |

A refactor that edited an assertion changed behaviour, whatever the commit message says. A test committed without its mutation proof is a test nobody demonstrated asserts anything. A documentation change that was never executed is unverified by the only means available.

---

## The only override

**An unexpired waiver in [`standards/waivers.md`](../../../standards/waivers.md), naming its approver, referencing the specific rule or finding being waived.**

There is no other path.

- Not "deferred with a good reason".
- Not "the engineer judged it acceptable".
- Not a review disposition of `ignored`, however well argued.

> A severity model whose top level is advisory is a severity model with one fewer level than it claims.

A waiver with **no expiry date** or **no named approver** is invalid — treat it as absent, not as a weak waiver. A waiver scoped to a pack rather than a specific rule is not a waiver, it is a repeal.

---

## The gate record

Write `docs/gates/GATE-<issue>.md` recording **each criterion with pass or fail, the evidence for each, and the approver name and timestamp on every pass**:

```markdown
---
issue: <number>
risk: <risk:low | risk:medium | risk:high>
standards_version: <from standards/VERSION>
outcome: <PASS | BLOCK>
---

## Criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Unaddressed critical finding | pass | <the review record, and the dispositions checked> |
| 2 | Unproven acceptance criterion | fail | <the AC, and that no test asserts it> |
| ... | | | |

## Waivers applied
| Waiver | Rule/finding | Approver | Expiry |
|---|---|---|---|

## Approvals
| Role | Name | Timestamp |
|---|---|---|
```

**Evidence is quoted, not summarised.** "AC 3 has no test" is a claim; "AC 3: *'Given an expired link, when requested, then 410 is returned'* — no test in the suite asserts a 410 response" is evidence a human can overturn.

A criterion recorded as `pass` with an empty evidence cell is not a pass. It is an unfilled form.

---

## Outcome

**On BLOCK** — state which criteria failed and stop. Do not recommend, do not soften, do not describe the work as nearly ready. The gate blocked; that is the output.

**On PASS** — record the approvals with names and timestamps, then suggest `/workflow-compound <n>`.

A pass requires a named human to have approved it. If no name is available, the gate has not passed — it is waiting.

---

## Constraints

- Do not pass a gate with an unaddressed critical finding.
- Do not accept a waiver with no expiry date or no named approver.
- Do not record an approval without a name — "approved" with no approver is not a record.
- Do not pass a gate yourself. This skill evaluates; a human passes.
- Do not record a criterion as passing without quoted evidence.
- Do not stop evaluating at the first failure — report every failing criterion, so the engineer fixes them in one pass rather than discovering them one at a time.
