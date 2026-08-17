---
name: solution-plan
description: Turn a baselined requirement set into a reviewed engineering plan — work streams, outcomes, sequencing, risks, and open decisions. Use when requirements are baselined and the work needs shaping before issues are written.
disable-model-invocation: true
argument-hint: "[requirement scope or empty for the full baseline]"
---

# solution-plan

**This skill runs two passes: plan, then review the plan.** The second pass grades what the first produced. Do not merge them.

## When this skill applies

**The greenfield path only.**

Enhancement, bugfix, refactor, test-improve and docs-improve work goes straight from `/impact-analysis` or `/work-intake` to `/github-issues` — because a single-issue change does not need work streams. Shaping one issue into three streams with a dependency graph is the ceremony `/work-intake` exists to prevent.

If the intake record in `docs/intake/` classifies this work as anything other than `greenfield`, say so and point at `/github-issues` instead of planning.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running — the question protocol governs Step 5 and the four-verdict model governs Step 6.

---

## Step 0 — Read the baseline

Read `requirements/baseline.md`, `requirements/assumptions.md`, `requirements/nfr.md`, and `requirements/review.md`.

**If `requirements/review.md` carries an unresolved BLOCK, stop.** Tell the user to run `/requirements-refine` first.

Planning on an unreviewed baseline wastes the plan: the review exists to catch dropped requirements and invented scope, and a plan built before it is a plan built on requirements that may not survive it.

If `$ARGUMENTS` names a requirement scope, plan that subset. If empty, plan the full baseline.

---

## Step 1 — Research in parallel

Dispatch two agents with the Task tool, concurrently:

1. **Existing patterns and conventions** relevant to this work.
2. **Past learnings** in `docs/solutions/` — what has already been tried, and what went wrong.

**Review both before continuing.** Work streams drawn without them repeat decisions already made and mistakes already paid for.

---

## Step 2 — Identify work streams

Roughly **three to eight**. Each is a coherent slice that could be owned end to end by one person or pair.

For each stream, record:

- **Name**
- **REQ IDs it covers**
- **Outcomes that mean it is done**
- **Dependencies on other streams**

### The outcome rule

**Outcomes are what changes for the user, not what gets built.**

> "Links resolve in under 50ms at p99" is an outcome.
> "Add a cache layer" is a task.

The difference matters because a task can be completed while the outcome it was supposed to produce does not happen — the cache ships, the p99 does not move, and the stream is "done". An outcome is falsifiable; a task is only ever finished.

---

## Step 3 — Sequence

Produce an **acyclic dependency graph** across the streams and identify the **critical path**.

If the graph has a cycle, two streams are actually one stream, or a dependency is stated in the wrong direction. Resolve it — do not record a cyclic graph.

**Flag any seam where splitting could lose an edge case.** A boundary between two streams is where a behaviour spanning both gets implemented by neither, because each owner reasonably assumed the other had it.

---

## Step 4 — Risks and open decisions

### Risks

Enumerate the **five to ten** things most likely to derail this, each with a **concrete mitigation, not a restatement**.

> "Risk: the migration may be slow. Mitigation: monitor the migration." — that is a restatement.
> "Risk: the migration may be slow. Mitigation: run it in batches of 10k behind a flag, with a measured dry run against a production-sized copy first." — that is a mitigation.

A mitigation is an action someone can be assigned. If it cannot be assigned, it is a hope.

### Open decisions

Keep open decisions in **ONE consolidated table**, ordered by urgency:

| Decision | Recommended approach | Owner role | Urgency |
|---|---|---|---|

**Never append a second table on a later pass. Merge into the existing one.** Separate tables per round are how plan documents drift — the reader finds two tables, cannot tell which is current, and the answer depends on which round each was written in.

---

## Step 5 — Resolve the blocking decisions

Resolve every open decision that blocks the plan, using the question protocol from `_shared/references/conventions.md`.

**Do not proceed to Step 6 until they are answered.**

**Never leave an unresolved question in the final document.** A plan with open questions in it is a draft that will be read as a plan — nobody re-reads the Open Decisions table before starting the work it authorised.

Where a decision cannot be resolved because two authorities conflict, that is an **ESCALATE**: surface both citations and the trade-off, name the role that must decide, and stop. Do not pick a side to keep the plan moving.

---

## Step 6 — Review pass

Check the plan against the baseline. **Quote the requirement text and the plan text side by side for each finding** — a verdict with no quoted evidence is invalid output, not a weak finding.

| ID | Check | Severity |
|---|---|---|
| **P1** | Every must-have REQ is covered by at least one work stream | **Blocker** |
| **P2** | Every work stream traces to at least one REQ | **Blocker** |
| **P3** | Every applicable NFR is attached to the stream that could violate it | Major |
| **P4** | The dependency graph is acyclic and the critical path is stated | Major |
| **P5** | Every risk has a mitigation that is an action, not a hope | Major |

P1 catches a dropped requirement. P2 catches invented scope — a stream nobody asked for, which is the more common of the two and the harder to see, because it always looks like useful work.

On a blocker, fix the plan and re-run this step. Do not record the finding and continue.

---

## Step 7 — Write the plan

Write `docs/plans/YYYY-MM-DD-solution-plan.md` with these sections:

| Section | Contents |
|---|---|
| **Context** | What this plans, which baseline scope, and the intake classification |
| **Work Streams** | A table, **plus a section per stream** |
| **Sequencing** | The dependency graph, the critical path, and flagged seams |
| **Risks** | Five to ten, each with an assignable mitigation |
| **Open Decisions** | The one consolidated table, all resolved |
| **Review Findings** | Step 6's findings with quoted evidence, and the overall verdict |
| **Coverage Map** | REQ ID to work stream |

### The table and the sections must match one-for-one

Same numbering, same names, no stream in one and not the other.

Drift between an at-a-glance table and its detail sections is the easiest way to confuse a reader: they scan the table, act on it, and never learn that the section it points to describes something slightly different. Check this before writing, not after.

---

## Step 8 — Hand on

Report the stream count, the critical path, the REQ coverage from the Coverage Map, and the overall verdict. Suggest `/github-issues`.

---

## Constraints

- Do not write or modify any application code.
- Do not plan on a baseline that has not been reviewed.
- Do not leave an unresolved open question in the final document.
- Do not let the work-stream table and the work-stream sections drift apart.
- Do not run this skill for non-greenfield work. Point at `/github-issues` instead.
- Do not merge the plan pass and the review pass.
