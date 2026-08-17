---
name: github-issues
description: Validate drafted work against the standards packs and create it as GitHub issues, with acceptance criteria, traceability, labels, and dependency links. Use when a plan or an impact analysis is complete and the work needs to land on the board.
disable-model-invocation: true
argument-hint: "[work stream name | 'all' | issue description]"
allowed-tools: Read, Write, Grep, Glob, Bash, Task, AskUserQuestion
---

# github-issues

Turns drafted work into issues on the board — after validating it against the standards packs.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running. The four-verdict model governs Step 4 and the question protocol governs every question asked here.

---

## Step 0 — Read the source

Read the intake record in `docs/intake/` first. It names the path, and the path determines what else to read:

| Path | Source to read |
|---|---|
| `greenfield` | The solution plan in `docs/plans/` |
| `enhancement`, `bugfix`, `refactor` | The impact analysis in `docs/impact/` |
| `test-improve`, `docs-improve` | The source description directly |

Confirm authentication with `gh auth status`. **If it fails, stop and tell the user.** Do not draft issues that cannot be created.

---

## Step 1 — Confirm scope

| Path | Scope |
|---|---|
| `greenfield`, `enhancement` | `$ARGUMENTS` names a work stream, or `all` for every stream. Otherwise ask which. |
| `bugfix`, `refactor`, `test-improve`, `docs-improve` | **Exactly one issue.** |

**Do not manufacture a backlog from a one-line fix.** On a single-issue path there is nothing to decompose — splitting a bug fix into three issues does not add rigor, it adds three things to close.

---

## Step 2 — Triage

Triage each outcome into **User Story**, **Spike**, or **needs-splitting**.

### The sizing principle

A story is the **smallest deployable unit of work that makes sense to product and engineering alike**.

- If the title needs "and", it is two stories.
- If it cannot be finished and merged in one sprint, it is an epic in disguise.

### Split signals

- More than about five acceptance criteria.
- ACs describing unrelated behaviours.
- A reviewer would reasonably want to merge half without the other half.
- The work spans two deployable units.

**Confirm the triage with the user before creating anything.**

---

## Step 3 — Draft each issue body

Use this exact shape:

```markdown
**As a** <user> **I want to** <action> **so that** <benefit>

## Context
<1-3 sentences: why now, what surrounds this>

## Acceptance Criteria
- [ ] Given <state>, when <action>, then <observable outcome>

## Out of Scope
<the boundary — what this issue does not cover>

## Traces to
REQ-003, REQ-007

## Blocked by
#12, #14

## Technical Notes
<only when a constraint or required pattern matters upfront; otherwise omit>
```

### Rules

- **One AC, one outcome.** If an AC needs "and", split it.
- **ACs describe observable outcomes, never implementation.** File paths, class names, and environment variable names belong in Technical Notes. A reader must be able to verify each AC **without reading the diff**.
- **Every issue traces to at least one REQ.** An issue tracing to none is invented scope and must not be created.
- **For a bugfix, `Traces to` names the REQ the current behaviour violates.** That is what makes it a bug rather than a preference — and it is the same citation `/impact-analysis` Step 0 established. If it is absent, this is not a bugfix.

### Cross-cutting ACs — add when the trigger applies

| Trigger | Required AC |
|---|---|
| User-visible behaviour change | An automated test covers it |
| New failure path | The failure surfaces in logs |
| New user-facing copy | Reviewed before merge |
| Performance-sensitive path | A stated budget with a number |
| New data collected | Retention stated |

### Path-specific AC rules

**Refactor.** The acceptance criteria are **behavioural invariants**: *"the existing behaviour of X is unchanged, proven by tests Y and Z"*. Take X, Y and Z from the constraining-tests list in the impact analysis.

> A refactor with no such AC has no definition of done — "the code is better" is not verifiable, so the issue can never be honestly closed or honestly rejected.

**Test-improve.** The AC states **which uncovered behaviour becomes covered and how that is proven** — not a coverage percentage. A percentage can be raised by testing whatever is easiest, which is rarely what was uncovered for a reason.

---

## Step 4 — Forward conformance

**Before creating anything**, check each drafted issue against the four standards packs in [`standards/`](../../../standards/). Use each rule's **detection hint** to decide whether that rule applies to this issue — the hint exists precisely so this decision is mechanical rather than a matter of taste.

Emit a verdict per pack:

| Verdict | Meaning and required action |
|---|---|
| **PASS** | No conflict. Proceed. |
| **PASS_WITH_AMENDMENT** | Propose the **exact amended acceptance criterion** — the replacement text, not a description of it. Apply it after the user accepts, then **re-check**. |
| **BLOCK** | Violates a standard. Cannot be created until resolved. |
| **ESCALATE** | A genuine conflict between the requirement and a standard, or between two standards. |

### On ESCALATE

Present **both citations** and the trade-off. Give **at least two concrete options, each naming what is given up**. Name the **ROLE** that must decide. Then **STOP**.

**Do not create the issue. Do not pick a side.**

### Evidence

**Every finding quotes the rule text and the issue text before the verdict.** A verdict with no quoted evidence is invalid output, not a weak finding — it cannot be argued with, and being arguable is the entire point of producing it.

### Record

Write `validation/<slug>-conformance.md` with the findings and **the standards `VERSION` in force**, read from [`standards/VERSION`](../../../standards/VERSION). `/gate-check` blocks on a validation record written against a superseded version, which it can only detect if the version is recorded here.

### Why this step exists

**This step is why the standards packs exist.** Catching a violation here costs minutes; catching it at review costs a rewrite — and catching it in production costs whatever the rule was protecting.

---

## Step 5 — Preview and approve

Preview **every issue in full, with its conformance verdict**, and get explicit approval before any write.

Creating issues is not reversible in a tidy way — a closed issue is still on the board, still in search results, and still notified to everyone watching.

---

## Step 6 — Create the issues

One `gh` call per issue, **in dependency order**, so blockers exist before their dependents reference them:

```bash
gh issue create --title "<title>" --body-file <tmpfile> --label "type:story,risk:medium,status:refined"
```

- Use the **type label matching the intake classification**, and the **risk label from the intake record** — not a default.
- **Create the tmpfile with Write, not a shell heredoc.** Issue bodies contain backticks and quotes that break shell escaping.
- **Create issues ONE AT A TIME in the foreground.**

> Do not batch them into a background task and do not delegate the batch to a subagent: a retried or re-spawned dispatch creates duplicates, and check-then-create does not protect against concurrent duplication.

---

## Step 7 — Second pass for dependencies

Once every issue has a number, edit the bodies to replace placeholder references with real `Blocked by #N` lines.

This is a second pass rather than part of Step 6 because an issue cannot reference a number that does not exist yet.

---

## Step 8 — Update the traceability matrix

Update `traceability/rtm.md`: one row per REQ with its issue numbers, plus **two queries answered explicitly**:

1. **Which REQs have no issue?** — requirements nobody is building.
2. **Which issues trace to no REQ?** — work nobody asked for.

**Report both counts. A non-zero count in either is a finding, not a footnote.**

---

## Step 9 — Hand on

Report the created issue numbers, the conformance verdicts, and both RTM counts. **Name the next skill in the path** from the intake record — `/workflow-brainstorm` where the path includes it, `/workflow-plan` where it was deliberately skipped.

---

## Constraints

- Do not create any issue without user approval of its full body.
- Do not create an issue carrying a BLOCK or ESCALATE verdict.
- Do not create issues in parallel or in a background task.
- Do not create an issue that traces to no requirement.
- Do not manufacture multiple issues on a single-issue path.
- Do not put acceptance criteria in the Context section.
- Do not record a conformance verdict without the standards VERSION in force.
