---
name: workflow-brainstorm
description: Facilitate structured brainstorming for one issue before any plan is written. Use when the user wants to explore how to approach a piece of work, or is about to start an issue whose approach is not yet obvious.
disable-model-invocation: true
argument-hint: "[issue number | topic description]"
---

# workflow-brainstorm

Explores how to approach one issue, before a plan commits to an approach.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running — the question protocol governs Step 2.

---

## Step 0 — Resolve the issue and check whether this skill should run at all

```bash
gh issue view <n> --json number,title,body,labels
```

Read the acceptance criteria and the `Traces to` line, then read those REQs from `requirements/baseline.md`. Read the intake record in `docs/intake/`.

### Offer to skip when the intake says the approach is settled

If the intake record classified this work as **`well-defined`**, or as **`test-improve`** or **`docs-improve`**, say so and **offer to skip**.

> Brainstorming a settled approach is theatre.

It produces a document with two invented alternatives and a foregone recommendation, and it costs the reader the time to work out that nothing was actually open. Worse, it launders a decision that was already made into one that looks deliberated.

**Proceed only if the user confirms the implementation approach is genuinely open.** "The requirement is clear but I do not know how to build it" is a legitimate reason to continue — a `well-defined` classification is about the requirement, not about the implementation.

### Warn if work has already started

If the issue carries `status:in-progress` or later, warn via AskUserQuestion:

> "Issue #\<n\> is already \<status\>. Continue brainstorming anyway?" — **Continue anyway** / **Stop**

**Brainstorming does not start work, so do not change labels here.** An issue's status reflects where the work is, and exploring an approach does not move it.

---

## Step 1 — Research in parallel

Dispatch two agents with the Task tool, concurrently:

1. **Existing patterns, conventions and related code** — what this codebase already does near this problem.
2. **Past learnings** in `docs/solutions/` — what has been tried, and the known pitfalls on this topic.

**Review both before asking anything.**

Questions must be grounded in the actual code, not in the abstract. An ungrounded question — "should we cache this?" — invites an ungrounded answer, and produces options that may not be buildable here. A grounded one — "the existing resolver reads through `X`; do we extend that or bypass it?" — can be answered.

The past-learnings search is not optional decoration: a pitfall already paid for should shape the questions asked, not be rediscovered in review.

---

## Step 2 — Ask clarifying questions

Use the question protocol. **Ground every option in what the research actually found.**

Cover:

- **Problem scope** — and explicitly what is out of scope.
- **The key scenarios** — including the ones that are not the happy path.
- **Constraints** — performance, compatibility, timeline.
- **Pattern or divergence** — follow an existing codebase pattern, or deliberately diverge from it.
- **External dependencies.**
- **Essential versus nice-to-have** if scope has to be cut.

That last one is asked now, while it is cheap, rather than during implementation when it is asked under pressure and answered by whoever is at the keyboard.

---

## Step 3 — Propose approaches

Propose **two to three** approaches with pros and cons.

**Apply YAGNI — favour the simplest approach that solves the stated problem.** Not the most extensible, not the one that anticipates next quarter. The stated problem.

**Recommend one and say why.** A menu with no recommendation pushes the analysis back to the reader, who has less context than you do at this moment.

---

## Step 4 — Stress-test the recommendation

Before writing anything up, **argue against your own recommendation**:

1. **The strongest objection** — the best case against it, made properly rather than as a straw man.
2. **The scenario where it fails** — concrete, not "if requirements change".
3. **What would have to be true for the runner-up to win** — and whether that thing might be true.

**If the stress test materially weakens the recommendation, revise it or switch approaches before continuing.** The test is not a formality you pass; it is allowed to change the answer.

**Record the outcome.** This is the step that catches an approach that merely reads well — an approach that survives being written up persuasively but not being argued against.

---

## Step 5 — Write the brainstorm document

Write `docs/brainstorms/YYYY-MM-DD-<issue>-<slug>-brainstorm.md`:

| Section | Contents |
|---|---|
| **Problem Statement** | What is being solved, and what is out of scope |
| **Codebase Context** | What the research found — patterns, related code, constraints |
| **Q&A Decisions** | The question and the answer for each |
| **Proposed Approaches** | Two to three, with pros and cons |
| **Recommendation** | The chosen approach and why |
| **Stress-Test Findings** | The objection, the failure scenario, the runner-up condition, and whether the recommendation changed |
| **Related Docs** | The intake record, the impact analysis or plan, and any learning from `docs/solutions/` that applied |

---

## Step 6 — Comment on the issue

```bash
gh issue comment <n> --body "Brainstorm complete. Doc: <path>. Recommended: <approach>. Next: /workflow-plan <n>"
```

---

## Step 7 — Hand on

Suggest `/workflow-plan <issue-number>`.

---

## Constraints

- Do not write or modify any application code. This skill produces documentation only.
- Do not skip the codebase research. Questions must be grounded in the actual code.
- Do not skip the past-learnings search — known pitfalls must inform the questions asked.
- Do not skip the stress test.
- Do not brainstorm work the intake classified as settled without confirming first.
- Do not change any label. Brainstorming does not start work.
