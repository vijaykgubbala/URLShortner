---
name: workflow-handover
description: Produce the handover artifact for a finished branch — release notes, risk analysis, and planned-versus-actual test coverage. Use after implementation and before review, so the reviewer knows where to look.
disable-model-invocation: true
argument-hint: "[issue number]"
allowed-tools: Read, Write, Grep, Glob, Bash, Task
---

# workflow-handover

Produces the handover artifact for a finished branch.

This runs **before** review, deliberately: the handover tells the reviewer where to look before they start. A reviewer who has to derive the risk surface themselves reviews the diff they were given rather than the change that was made.

---

## Step 0 — Gather the facts

Resolve the issue and gather, **in as few calls as possible**:

- Current branch
- Base branch
- Changed files — `git diff --name-only <base>...HEAD`

**Do not pull the raw full diff into context — summarize it.** The diff is large, mostly uninformative, and displaces the thing this document is actually for. File names plus targeted reads of what matters are enough.

---

## Step 1 — Locate the prior artifacts

- **The plan** in `docs/plans/` — for the Testing Strategy to reconcile against, and the execution mode.
- **The impact analysis** in `docs/impact/`, if one exists — for the blast radius, hot paths, and consumers already established.

**If no plan exists, continue with `plan: none` and note the degradation in the document** rather than aborting. A handover with no plan to reconcile against is worth less, and saying so is what tells the reviewer that the planned-versus-actual table is missing one side rather than being clean.

---

## Step 2 — Write the handover

Write `docs/handovers/YYYY-MM-DD-<issue>-<branch-slug>-handover.md`:

```markdown
---
issue: <number>
branch: <name>
base: <name>
plan: <path or "none">
mode: <execution mode used>
---

## Release Notes

## Risk Analysis
| Area | Blast Radius | Reviewer Focus | Mitigation |

## Test Coverage
### Planned vs Actual
| Planned Test | Status | Notes |

### What's Not Tested

### Mode-specific evidence

## Rollback
```

### Release Notes

Written **for product, QA and support — not engineers.** What changed from a user's point of view.

**For a refactor, state plainly that no behaviour changed and what proves it** — naming the characterization tests and the pinning commit. "Refactored the resolver" tells support nothing; "no user-visible change; the existing redirect behaviour is pinned by 14 characterization tests committed in `a1b2c3d` before any change" tells them whether to expect tickets.

### Risk Analysis

One row minimum.

- **Blast Radius** is small / medium / large **with its scope named** — "medium: every caller of the redirect endpoint", not "medium".
- **Reviewer Focus** is where a human should look first, **not a restatement of the area**. "The cache invalidation on revoke" is focus; "the caching changes" is the area again.

**Carry forward what the impact analysis found rather than re-deriving it.** That analysis was done with the whole codebase in view and before the code existed; re-deriving it from the diff produces a narrower answer that looks like agreement.

### Planned vs Actual

Status is **written**, **skipped**, or **changed**. Any test written that was not planned gets a row marked `(unplanned) ... added`.

**This table is the honest record of drift between what was promised and what was done — do not tidy it.** A skipped test with a reason is information; a skipped test quietly removed from the table is the reviewer being told the plan was followed.

### What's Not Tested

**Explicit, unhedged. Name the gaps.**

> "Everything is covered" is almost never true and reads as though nobody looked.

If a behaviour is genuinely untested, say which one and why. This section is the one a reviewer reads first when deciding where to spend their attention.

### Mode-specific evidence

Include the section for the mode that was used:

| Mode | Evidence required |
|---|---|
| **characterization** | The pinning commit SHA, and confirmation that no assertion was modified during the refactor |
| **mutation-verified** | The mutation used per test, and confirmation that production code was restored |
| **documentation** | What had to be fixed because the instructions did not work as written |
| **TDD** | No additional section required |

These are the proofs the mode's discipline actually happened. Without them, a reviewer has the claim that a refactor preserved behaviour and no way to check it.

### Rollback

How to undo this in production — **and whether undoing it after data has been written is actually possible.**

Name the mechanism: revert, feature-flag flip, or documented reverse migration. Where a true rollback is not possible because data has been written or events emitted, say so and state the recovery procedure and what it loses.

---

## Step 3 — Confirm the write

**Read the file back.** A handover that was not actually written is indistinguishable from one that was, right up until the reviewer goes looking for it.

---

## Step 4 — Comment and hand on

Comment on the issue with the handover path, the blast radius, and the untested-gap count. Suggest `/workflow-review <n>`.

---

## Constraints

- Produce exactly one file under `docs/handovers/` and nothing else.
- Do not pull raw git diff output into main context; summarize it.
- Do not soften the What's Not Tested section.
- If no plan was found, say so in the document rather than silently skipping the reconciliation.
- Do not re-derive the blast radius from the diff when an impact analysis already established it.
- Do not omit the mode-specific evidence section for the mode that was used.
