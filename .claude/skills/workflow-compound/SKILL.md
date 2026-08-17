---
name: workflow-compound
description: Capture what was learned from completed work as searchable institutional knowledge. Use when an issue is finished, a bug is fixed, or a review surfaced something worth not repeating.
argument-hint: "[issue number | 'context']"
---

# workflow-compound

Turns what was learned into something that changes what happens next time.

**This is the one hybrid skill in the chain** — it carries no `disable-model-invocation`, so Claude can also invoke it on its own when a session obviously produced a lesson. Every other skill in the chain waits to be called; this one is allowed to notice.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running.

---

## Step 0 — Determine mode

| `$ARGUMENTS` | Mode |
|---|---|
| `context` | Capture only from the current conversation |
| An issue number, or nothing | Sweep the artifacts for that issue |

In sweep mode, read all of:

- The **intake record** — `docs/intake/`
- The **impact analysis** — `docs/impact/`
- The **brainstorm** — `docs/brainstorms/`
- The **plan** — `docs/plans/`
- The **handover** — `docs/handovers/`
- The **review JSON** — `docs/reviews/`
- The **gate record** — `docs/gates/`
- The **commit history** for the issue

---

## Step 1 — Extract candidate lessons

**A lesson qualifies only if it would change what someone does next time.**

> "We used xUnit" is not a lesson.
> "Integration tests against the in-memory provider pass while the real provider rejects the query — assert against the real one" is.

The test is not "was this true?" but "would knowing it in advance have changed a decision?"

### Mine specifically

- **Review findings dispositioned as `addressed`** — each one is a mistake that got made.
- **Any AMEND, BLOCK or ESCALATE** the earlier stages produced.
- **Anything in the handover's What's Not Tested that surprised someone.**
- **Any plan step that turned out wrong.**
- **For a bugfix: why the earlier gates did not catch this.**

### The gate question

**For a bugfix, answering "which gate should have caught this?" is the most valuable question in the whole system.**

A defect that reached production means a gate did not do its job. Naming which one is how the gates improve — otherwise the system accumulates fixes and never gets better at preventing them.

Work through the chain and name the specific stage:

| Stage | Would have caught it if… |
|---|---|
| `/requirements-refine` | The requirement was ambiguous and the ASM was never raised |
| `/impact-analysis` | The affected consumer or hot path was never enumerated |
| `/workflow-plan` | The AC had no test case in the Testing Strategy |
| `/workflow-execute` | The test existed but was never observed to fail for the right reason |
| `/workflow-review` | A lens exists for this defect class and did not raise it |
| `/gate-check` | A criterion covers this and passed anyway |

If **no** gate would have caught it, that is the finding — the system has a hole, and it is a candidate for a new rule rather than a lesson.

---

## Step 2 — The graduation question

**Asked of every candidate: is this really a lesson, or a missing rule?**

- A **lesson** lives in `docs/solutions/` and someone has to remember to read it.
- A **rule** lives in a standards pack where a validator or review agent enforces it, or in a lint rule where the compiler enforces it.

**Prefer the rule every time — a document nobody reads is not a control.**

### If it graduates

Propose the rule **in the correct pack**, in the pack's exact shape:

- A rule **ID** in that pack's sequence — `STD-ARCH-nn`, `STD-SEC-nn`, `STD-OPS-nn`, `STD-DATA-nn`
- A **Statement** — one sentence, testable, imperative
- A **Rationale** — why it exists
- A **Detection hint** — what in the code or in a story's text indicates the rule applies
- **Applies to** and **Severity if violated**

**Ask the user before writing it.** A new rule binds every future change, and `review-security` and `review-architecture` will begin citing it immediately — that is a decision, not a capture.

**Bump `standards/VERSION` when a pack changes.** Conformance records carry the version they were validated under, and `/gate-check` criterion 5 blocks on a record written against a superseded pack. A pack edited without a version bump makes every prior record silently claim currency it does not have.

---

## Step 3 — Write the remaining lessons

Write to `docs/solutions/<category>/<slug>.md`:

| Section | Contents |
|---|---|
| **Problem** | What was being attempted |
| **What Happened** | The observed behaviour, concretely |
| **Root Cause** | Why, not what |
| **Prevention** | What to do differently, as instructions |
| **Key Insight** | The one sentence worth remembering |

**Keep them short and actionable — engineers scan, they do not read.** Start every instruction sentence with a verb.

**Deduplicate against `docs/solutions/` before writing.** A near-duplicate is worse than nothing because it splits the search: two half-documents where a reader finds one and stops, missing the half that mattered. If a near-duplicate exists, extend it instead.

---

## Step 4 — Close the issue

```bash
gh issue edit <n> --add-label "status:done" --remove-label "status:in-review"
gh issue comment <n> --body "Complete. Learnings: <paths>. <N> rules proposed."
```

Add and remove in the same call — an issue carrying two status labels has no state.

Only close an issue whose gate actually passed. `status:done` on an issue that never cleared `/gate-check` is the one label in the system that cannot be recovered from by reading further, because it says the work is finished.

---

## Constraints

- Do not modify application code.
- Do not write a lesson that is really a rule — graduate it and ask.
- Do not write a near-duplicate of an existing solution doc.
- Keep each solution doc focused on one problem.
- For a bugfix, do not skip the "which gate should have caught this" question.
- Do not add a rule to a standards pack without user approval.
- Do not edit a standards pack without bumping `standards/VERSION`.
