---
name: impact-analysis
description: Map what a change to existing code will touch — modules, endpoints, data flows, consumers, hot paths, and the tests that constrain it — before any plan is written. Use for any enhancement, bug fix, or refactor against code that already exists.
disable-model-invocation: true
argument-hint: "[issue number | description of the change]"
---

# impact-analysis

Maps what a change to existing code will actually touch, before a plan is written against it.

Greenfield work does not need this skill — there is nothing to disturb. Everything else does, because the cost of a change to existing code is dominated by what it touches indirectly, and that is the part nobody estimates.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running.

---

## Step 0 — Establish the correct behaviour first

Read the intake record in `docs/intake/` and the requirement or bug report.

**For a bugfix, establish what the correct behaviour is and cite where it is specified** — a `REQ-nnn` in `requirements/baseline.md`, an acceptance criterion on the issue, or a rule in `standards/`. Quote it.

A bug is a **deviation from a stated requirement**. If nothing states the correct behaviour, this is not a bug:

> This is an enhancement wearing a bug's clothing.

Say exactly that, and route it back to `/work-intake` for reclassification rather than proceeding. **Do not proceed past this step.**

The distinction is not pedantry. Fixing an "enhancement wearing a bug's clothing" as a bug changes the code and leaves the requirement stating the wrong thing — so the next person to implement from the baseline reintroduces it, correctly, according to the document.

For an enhancement or refactor, record instead what behaviour must be **preserved**, and where that is specified.

---

## Step 1 — Map the change surface

**Use Grep and Glob. Do not assume.** An impact analysis assembled from memory of the codebase is the failure mode this skill exists to prevent.

Produce four lists:

### 1.1 Entry points touched
Endpoints, handlers, background jobs, scheduled tasks, UI routes.

### 1.2 Modules and layers
Every module involved, and **which layer each change belongs in** per [`architecture/layers.md`](../../../architecture/layers.md). If a change appears to belong in a layer the current code does not put it in, that is a finding.

### 1.3 Data flows crossed
What reads this, what writes it, what sits downstream. Include caches, read models, emitted events, and anything that copies the data.

### 1.4 Consumers
Anything depending on a contract this change alters — other services, the BFF, published events, stored data shapes, persisted messages.

**Name them.** "Various callers", "downstream consumers", and "clients of this API" are not answers. Enumerate them.

If you could not enumerate them — the search does not reach outside this repository, the consumers are unknown, the contract is public — **say that explicitly**:

> Consumers could not be enumerated: <what you searched, and why it is insufficient>.

An honest "I could not determine this" is a finding a human can act on. A generic plural is the same gap with the alarm switched off.

---

## Step 2 — Classify blast radius

Classify as **small**, **medium**, or **large**, and **show the reasoning**.

| Class | Meaning |
|---|---|
| **small** | Contained within one module. No contract change. |
| **medium** | Multiple modules, or a contract change with enumerated and reachable consumers. |
| **large** | A contract, a schema, or a hot path is affected. |

**A classification with no reasoning is a guess wearing a label.** State which of the four lists from Step 1 drove the classification, and name the specific contract, schema, or hot path where the class is large.

---

## Step 3 — Hot paths and invariants that must not regress

For each hot path and each invariant, state three things:

1. **What currently holds** — the property, stated concretely.
2. **How it is currently proven** — the test that asserts it, by file and name. **Or that it is not proven at all**, said plainly.
3. **What would break it** — the specific change or addition that would violate it.

This is the step that catches *"the analytics write is on the redirect path"* before somebody writes it that way — and it only catches it if the redirect path's latency invariant is written down here with what would violate it.

An invariant that currently holds by accident, with nothing proving it, is the most dangerous kind. Say so.

---

## Step 4 — Schema and data changes

For each schema or stored-data change, answer all five. **An unknown answer to any of these is a finding, not a gap to fill in later.**

1. **Is it additive and reversible?** Per [`standards/data.md`](../../../standards/data.md) `STD-DATA-01`, a destructive change in the same release that stops writing to the column is a violation.
2. **What is the migration?** Named, versioned, in source control.
3. **Is a backfill needed, and over what volume?** State the row count. "Some rows" is not a volume.
4. **What happens to in-flight requests during the migration?** Both application versions run against one schema during a rolling deploy.
5. **Is it re-runnable?** A backfill that cannot be safely re-run must say so.

If there are no schema or data changes, state that explicitly rather than omitting the section.

---

## Step 5 — Rollback

State how this change is undone in production, and **whether undoing it after data has been written is actually possible**.

> "Revert the commit" is not a rollback plan for anything that touched a schema.

Name the mechanism — revert, feature-flag flip, or documented reverse migration. Where a true rollback is impossible because data has been written or emitted, say that, and state what the recovery procedure is instead and what is lost by it.

---

## Step 6 — Existing tests that constrain the change

List them **by file**, with the behaviour each one pins.

**For a refactor this is the most important section in the document.** A refactor must not change observable behaviour — and these tests are the specification of the behaviour you must preserve. There is no other specification. Any behaviour with no test is behaviour you can break silently, and nothing in the pipeline will notice.

**Flag untested behaviour explicitly as characterization-test candidates:**

```markdown
### Characterization-test candidates
| Behaviour | Where | Why untested matters here |
|---|---|---|
```

`/workflow-execute` reads this list in characterization mode and writes those tests **before** touching the structure. A candidate omitted here is a behaviour that gets refactored with nothing watching it.

Report the count of constraining tests — it is one of the two numbers Step 8 reports.

---

## Step 7 — Failure scenarios

Enumerate what can go wrong **in production**. For each: how it is detected, its blast radius, and its mitigation.

**Prefer the specific over the generic.** *"The cache and the database disagree after a revoke"* beats *"caching issues"* — the first names a scenario someone can test, alert on, and mitigate; the second names a topic.

A failure with no detection method is worse than one with no mitigation: an undetected failure is one you learn about from a user.

---

## Step 8 — Write the document

Write `docs/impact/YYYY-MM-DD-<slug>-impact.md` containing all of the above:

```markdown
---
issue: <number or "none">
intake: <path to the intake record>
work_type: <from the intake record>
blast_radius: <small | medium | large>
constraining_tests: <count>
---

## Correct Behaviour        (Step 0 — quoted, with its source)
## Change Surface           (Step 1 — entry points, modules and layers, data flows, consumers)
## Blast Radius             (Step 2 — class plus the reasoning)
## Hot Paths and Invariants (Step 3)
## Schema and Data Changes  (Step 4)
## Rollback                 (Step 5)
## Constraining Tests       (Step 6 — by file, plus characterization-test candidates)
## Failure Scenarios        (Step 7)
```

**Report two numbers:** the blast radius and the count of constraining tests.

**If the blast radius is large but the intake record assigned `risk:low`, say so and recommend raising it.** The two were judged at different times with different information — this one had the codebase in front of it. A large blast radius sitting under `risk:low` means `/gate-check` will accept self-approval on a change that affects a contract, a schema, or a hot path.

---

## Constraints

- Do not write or modify any application code.
- Do not name a consumer group generically. Enumerate, or state that you could not.
- Do not classify blast radius without showing the reasoning.
- Do not proceed past Step 0 on a "bug" with no stated correct behaviour.
- Do not fill an unknown schema or migration answer with a plausible guess. An unknown is a finding.
- Do not omit a section because it does not apply. State that it does not apply.
