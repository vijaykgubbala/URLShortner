---
name: workflow-plan
description: Create a research-backed implementation plan with checkboxes and a test-first ordering. Use when an issue is understood and needs a detailed plan before any code is written.
disable-model-invocation: true
argument-hint: "[issue number | plan description]"
---

# workflow-plan

Produces the implementation plan `/workflow-execute` runs against.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running — the question protocol governs Step 4.

---

## Step 0 — Resolve the issue

```bash
gh issue view <n> --json number,title,body,labels
```

**Read the acceptance criteria. They are the definition of done for everything downstream, so carry them forward verbatim rather than paraphrasing.** A paraphrase drifts by one word, and that word is what the reviewer at `/gate-check` measures the work against.

Also read:

- **The intake record** in `docs/intake/` — for the **execution mode** and the risk class.
- **The impact analysis** in `docs/impact/`, if one exists — for the constraining tests, the unproven-behaviour list, and the blast radius.

**Planning does not start work; do not change labels yet.**

---

## Step 1 — Read the brainstorm

Read the matching brainstorm in `docs/brainstorms/` if one exists. Use its recommended approach, its Q&A decisions, and its stress-test findings as input — particularly the stress-test findings, which record where the recommendation was weak.

If the path deliberately skipped brainstorming, note that here rather than treating the absence as an oversight.

---

## Step 2 — Research in parallel

Dispatch with the Task tool, concurrently:

1. **Existing patterns** relevant to this work.
2. **Past learnings** in `docs/solutions/`.
3. **External best practice** — add this third agent only when the work involves unfamiliar territory, a new integration, or a high-risk area.

---

## Step 3 — Architecture pre-flight

**Mandatory whenever the plan touches `src/`.**

1. Derive a one-sentence description of what this plan implements and which layers it touches.
2. Invoke the Skill tool: `/architecture-guide <that description>`.
3. Extract the returned rules — especially which types belong in which layer and which project references are permitted.

**Any plan step that would violate a returned rule must be rewritten to comply BEFORE the plan is written to disk.** Not flagged in the plan, not left for review to catch — rewritten. A plan that records a known violation authorises it.

**If a returned rule conflicts with the brainstorm's recommendation, surface the conflict rather than silently picking one.** The brainstorm chose an approach without the rules in front of it; the rules were written without this task in front of them. Which yields is a decision, and per the ESCALATE doctrine it is not yours to make quietly.

---

## Step 4 — Risk resolution gate

Enumerate every risk, unknown, ambiguity and decision point, including:

- Multiple valid approaches where the choice affects architecture.
- External dependencies with unknowns.
- Ambiguous requirements not settled by the issue or the brainstorm.
- Performance, security or scale concerns with no obvious answer.

**Resolve every one with the user.** Do not proceed to Step 5 until all are answered.

**Never list an unresolved question in the final plan.** A plan containing open questions is a draft, and it will be executed as a plan — `/workflow-execute` works the checkboxes, not the caveats.

---

## Step 5 — Write the plan

Write `docs/plans/YYYY-MM-DD-<issue>-<type>-<slug>-plan.md`, where type is `feature`, `fix`, `refactor` or `infra`.

| Section | Contents |
|---|---|
| **Context** | The issue, the REQs it serves, the chosen approach |
| **Execution Mode** | `TDD` \| `characterization` \| `mutation-verified` \| `documentation`, carried from the intake record |
| **Acceptance Criteria** | Copied **VERBATIM** from the issue, not paraphrased |
| **Architecture Constraints** | The binding rules from `/architecture-guide`, **quoted** with their source citations |
| **Relevant Learnings** | Lessons from `docs/solutions/`, inlined at the step where they apply, not as a reading list |
| **Testing Strategy** | Every test case with description, type (unit/integration/e2e), the AC it proves, and its edge cases |
| **Implementation Steps** | Checkboxes |
| **Decisions Made** | With rationale. No open questions. |
| **Known Constraints** | Accepted external limitations, framed as accepted, not as worries |

### Two ordering rules

- **Every implementation step references which test case it satisfies.**
- **Test-writing steps appear BEFORE the implementation steps they verify.**

This is what makes the cycle in `/workflow-execute` mechanical rather than aspirational. If the plan interleaves them arbitrarily, the executing session has to decide the order under pressure, and it will decide the way that gets to green fastest.

### Mode-specific planning

**`characterization`** — the Testing Strategy lists the **characterization tests to write first**, drawn from the impact analysis's unproven-behaviour list. State explicitly that **they must pass against unchanged code before any refactoring begins**.

> A characterization test written after the refactor pins the new behaviour, not the old one — which is the exact thing it was supposed to detect a change in.

**`mutation-verified`** — every test case **names the mutation that will be used to prove it actually asserts something**: the specific change to production code that must make this test fail. A test that passes both before and after its mutation is asserting nothing, and coverage counts it anyway.

**`documentation`** — the Testing Strategy is **replaced by an execution checklist**: every instruction in the document, to be followed literally from a clean state. Documentation is verified by execution, not by review — a reader who already knows the answer cannot tell that a step is missing.

---

## Step 6 — Coverage check

**Every acceptance criterion has at least one test case in the Testing Strategy.**

An AC with no test is a **blocker** — fix the plan. Do not note it and move on: an unfixed gap here becomes an unprovable AC at `/workflow-execute` Step 6 and a blocked gate at `/gate-check` criterion 2, both at higher cost.

On the `documentation` mode, read "test case" as "execution checklist item".

---

## Step 7 — Label and comment

```bash
gh issue edit <n> --add-label "status:planned" --remove-label "status:refined"
gh issue comment <n> --body "Plan created. Doc: <path>. Mode: <mode>. Steps: <N>."
```

Add and remove in the same call. An issue carrying two status labels has no state.

Report the plan path, the execution mode, the step count, and the AC-to-test coverage. Suggest `/workflow-execute <n>`.

---

## Constraints

- Do not write or modify any application code.
- Do not skip the parallel research step or the architecture pre-flight.
- Do not write the plan until every identified risk has a user-confirmed answer.
- Never list an unresolved risk or open question in the final plan.
- Do not paraphrase the acceptance criteria.
- Do not write a plan whose implementation steps precede the tests that verify them.
- Do not change labels before Step 7.
