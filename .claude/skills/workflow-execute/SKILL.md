---
name: workflow-execute
description: Execute an implementation plan step by step using the verification cycle that fits the work — test-driven for new behaviour, characterization-first for refactors, mutation-verified for test work, execution-verified for documentation. Use when a plan is ready and implementation should begin on a feature branch.
disable-model-invocation: true
argument-hint: "[issue number | plan file path]"
---

# workflow-execute

Executes an implementation plan under the verification cycle that fits the work.

Every mode below has the same shape: **something must be observed to fail, or observed to hold, before the work is trusted.** What differs is what that something is. Red-green does not apply to a refactor, and it cannot apply to a test — so the verification inverts rather than being dropped.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running.

---

## Step 1 — Load the plan

If `$ARGUMENTS` is an issue number, find the matching plan in `docs/plans/`. If it is a path, use it. If empty, take the most recent by date prefix.

Read the **Execution Mode** section — it selects the cycle in Step 4. Parse the `- [ ]` checkboxes as the work. Read the **Acceptance Criteria** — these define done.

---

## Step 2 — Branch and mark in progress

Branch prefix matches the work type:

| Work type | Prefix |
|---|---|
| greenfield, enhancement | `feat/` |
| bugfix | `fix/` |
| refactor | `refactor/` |
| test-improve | `test/` |
| docs-improve | `docs/` |

```bash
gh issue edit <n> --add-label "status:in-progress" --remove-label "status:planned"
gh issue comment <n> --body "Implementation started. Plan: <file>. Mode: <mode>. Steps: <N>."
```

---

## Step 3 — Create the task list

Create a task per checkbox with TaskCreate, setting `addBlockedBy` where order matters.

**Announce the list and the execution mode, with one line on why that mode fits.** The mode determines what "verified" means for the next hour of work; the user should get the chance to disagree before it starts, not after.

---

## Step 3.5 — Architecture pre-flight

**Before writing a single line of implementation code**, invoke `/architecture-guide` with a one-sentence description of the work.

Extract the hard rules and **treat them as non-negotiable constraints for every task** — not as guidance to weigh against convenience once the code is half-written.

If a task conflicts with a rule, ask the user:

> **"Update the plan to comply"** or **"Continue as an accepted deviation"**

**Do not write code until the conflict is resolved.** A deviation decided before the code exists is a decision; the same deviation discovered afterwards is a sunk cost arguing for itself.

---

## Step 4 — Execute each task

Use the cycle for the selected mode.

---

### MODE A — TDD

*For greenfield, enhancement, and bugfix.*

1. **Mark the task `in_progress`.**
2. **Follow existing patterns** — read related files and match their conventions, naming, and structure before writing anything new.
3. **RED.** Write the failing test(s) from the plan's Testing Strategy. **RUN them. Confirm they fail.**

   If a test passes with no implementation behind it, the test is not asserting the right thing — **fix the test before writing any production code**. Do not proceed to green on an unverified red.

   **For a bugfix, the first test must REPRODUCE the defect** and fail *for the reason the bug exists*, not for a setup error. **Read the failure message and confirm it describes the actual defect.** A bugfix test that fails for the wrong reason will go green after a fix that changed nothing relevant — and the bug ships with a test that appears to cover it.

4. **GREEN.** Minimum implementation to pass. Run. Confirm. **Minimum means minimum** — do not build ahead of the test.
5. **REFACTOR.** Improve naming, remove duplication, simplify — no behaviour change. Re-run and confirm still green.
6. **Commit test and implementation together** as one unit. The message explains why, not what:
   ```
   <subject>

   Refs #<n>
   ```
7. **Check off the plan item** — edit `- [ ]` to `- [x]` in the plan file.
8. **Mark the task completed.**

---

### MODE B — Characterization-first

*For refactors.*

**A refactor cannot be red-green tested, because the behaviour is supposed to stay the same. The cycle inverts, and that inversion is the point.**

1. **Read the impact analysis** — the section listing constraining tests, and the behaviours it flagged as having no test.
2. **PIN.** For every behaviour this refactor could change that has no test, write a characterization test asserting **what the code does TODAY** — not what it should do.

   **If today's behaviour is wrong, that is a separate bugfix issue: record it and do not fix it here.** Fixing it inside a refactor makes the diff a behaviour change wearing a refactor's clothing, and no reviewer looking at a refactor is looking for one.

   Run the characterization tests and **confirm they PASS against unchanged code**. A characterization test that fails immediately is describing something other than current behaviour — fix the test, not the code.

3. **Commit the characterization tests ALONE, before any refactoring.**

   This commit is the safety net and **must exist separately in history**, so a bisect can prove the behaviour was pinned before it was touched.

4. **REFACTOR in small steps.** After each step, run the full constraining set.

   **Any test going red means the refactor changed behaviour — revert that step.**

   > **Do not "fix" the test to match the new behaviour; that defeats the entire exercise.**

   The test is the specification. Editing the specification to match the implementation is not a refactor passing its tests, it is a refactor deleting them.

5. **Commit each green refactoring step.**
6. **At the end, confirm no test assertion was modified during the refactor.** If any was, **explain why in the handover** — that is a behaviour change wearing a refactor's clothing, and a reviewer needs to know.

---

### MODE C — Mutation-verified

*For test improvements.*

**You cannot write a failing test for a test. The verification inverts: prove the new test would catch the bug it claims to catch.**

1. **Baseline.** Run coverage and record what is uncovered, plus any *covered* code whose tests assert nothing meaningful. Covered-but-unasserted is the more dangerous of the two, because the number says it is handled.
2. **Write** the new or strengthened test. Run it. Confirm it passes.
3. **MUTATE.** Deliberately break the production code the test covers — invert a condition, shift a boundary, return a wrong constant. Run the test.

   **It MUST fail.**

   If it still passes, the test asserts nothing — and adding it is **worse than adding nothing**, because it makes the coverage number lie. A gap you can see is safer than a gap reported as covered.

4. **RESTORE the production code exactly.** Re-run, confirm green, and **verify with `git diff` that no production file was left modified.**
5. **Commit the test alone**, recording in the commit message the mutation used to verify it. That record is what lets a future reader trust the test without re-deriving the proof.

---

### MODE D — Documentation

*For documentation improvements.*

1. **Read the code the documentation describes.** The code is the source of truth; where they disagree the doc is wrong — **unless the code is the bug, and if it is, that is a separate issue.**
2. **Write or correct** the documentation.
3. **VERIFY BY EXECUTION.** Follow every instruction in the changed documentation **literally, from a clean state, exactly as written.** Every command must run. Every stated output must appear.

   This is the documentation equivalent of running the test: **a doc that has not been executed is a doc that is wrong.** The author cannot see the missing step, because they already know it.

4. **Record what you had to fix** because the instructions did not work as written. That list is the actual finding — it is what a reader would have hit.
5. **Commit.**

---

> **A task in any mode is NOT complete until its verification step has actually been run and observed — not assumed.**
>
> Not "mostly passes". Not "fails for an unrelated reason". Not "this obviously works".

---

## Step 5 — Guidelines during execution

- **Prefer small focused changes** over sweeping ones.
- **Do not refactor or improve code outside the scope of the plan.**
- **If you hit an ambiguity the plan does not cover, use AskUserQuestion before proceeding.** Do not guess and continue.
- **If no test framework exists, setting one up is the first task**, before any feature work.
- **Cross-cutting renames:** Grep for the identifier first, review every match, then make narrow per-file edits. A bare global replace on a short identifier reliably hits third-party SDK names and documentation in the same pass.

---

## Step 6 — Final checks

1. **Run the full test suite.** Fix failures.
2. **Run lint and format.** Fix issues.
3. **Verify every plan checkbox is checked.**
4. **Verify every acceptance criterion has at least one passing test asserting it.**

   This is the real completion gate — **checkbox exhaustion is not done, AC proof is done.** Report any AC without a test as a **blocker**.

5. **Mode B only:** confirm no pre-existing test assertion was modified.
6. **Mode C only:** confirm `git diff` shows no production code changed.

---

## Step 7 — Comment and hand on

```bash
gh issue comment <n> --body "Implementation complete. <N> steps. Branch: <branch>. Ready for /workflow-handover."
```

Suggest `/workflow-handover <n>`.

---

## Constraints

- Never write implementation code before its verification exists and has been observed.
- Never close a task with an acceptance criterion unproven.
- Never modify a characterization test to accommodate a refactor.
- Never commit a test that has not been proven to fail against a mutation, in Mode C.
- Do not refactor outside the plan's scope.
- Do not commit implementation without its test in the same commit, in Mode A.
- Do not fix a behaviour defect discovered during a refactor. Record it as a separate issue.
