---
name: work-intake
description: "Classify incoming work and select the path that fits it — greenfield, enhancement, bugfix, refactor, test improvement, or documentation improvement — and set the depth of requirement interrogation. Use whenever any new work arrives: a requirement doc, a feature request, a bug report, a refactor proposal, or a note that the tests or docs need attention."
disable-model-invocation: true
argument-hint: "[path to source doc | description of the work]"
---

# work-intake

## Why this skill exists

**Applying maximum rigor to every piece of work is not rigor, it is ceremony.**

A one-line bug fix routed through full requirements engineering produces artifacts nobody reads and buries the judgment that actually mattered. The baseline entry, the assumption register, the solution plan, the brainstorm — each is defensible in isolation, and together they hide the one sentence that was the whole decision.

This skill chooses how much process the work deserves and **records why**. The recording is not paperwork: a path chosen deliberately and a path chosen by default look identical downstream, and only one of them can be argued with.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running — the question protocol governs every question this skill asks.

---

## Step 1 — Read the source

Take `$ARGUMENTS`. It may be a path to a requirement document, an issue number, a bug report, a paragraph of description, or a pointer to the conversation so far.

- **A file path** — read it.
- **An issue number** — `gh issue view <n> --json number,title,body,labels`.
- **A description** — take it as the source, and say so in the intake record.
- **Nothing** — ask what arrived. Do not classify an empty input.

---

## Step 2 — Classify along two independent axes

**Do not collapse these axes.** They vary separately and each drives a different decision. A well-defined greenfield feature and an ambiguous one take the same path with different depth; an ambiguous bugfix and an ambiguous refactor take different paths at the same depth.

### Axis 1 — Work type

| Type | Definition |
|---|---|
| `greenfield` | New system or new feature. No existing behaviour to preserve. |
| `enhancement` | New behaviour added to an existing system. Existing behaviour must keep working. |
| `bugfix` | Existing behaviour is wrong against its stated requirement. |
| `refactor` | Internal structure changes; observable behaviour must **not** change. |
| `test-improve` | The production code is fine; tests are missing, weak, or wrong. |
| `docs-improve` | The code is fine; documentation is missing, wrong, or stale. |

**The bugfix / enhancement distinction matters and is routinely blurred.**

- A **bugfix** means the behaviour is wrong against what was asked for.
- An **enhancement** means what was asked for was itself wrong or incomplete.

Blurring the two is how requirements silently rot, because the second kind never gets written back into the baseline. The code gets fixed, the requirement that specified the wrong thing stays as it was, and the next person implements the original mistake again from the baseline.

The test: find the requirement the behaviour is measured against. If the code disagrees with it, that is a bugfix. If the code agrees with it and the requirement is what is wrong, that is an enhancement — and the baseline changes too.

When it is genuinely unclear, **ask the user** via the question protocol. Do not pick.

### Axis 2 — Ambiguity

| Level | Definition |
|---|---|
| `well-defined` | The desired outcome is unambiguous and testable as stated. |
| `ambiguous` | Key terms are undefined, the outcome is stated as a feeling, or two honest readings of the text would produce different systems. |

**Judge by attempting the test:** can you write the acceptance criterion right now, from the text alone, without inventing anything?

If you find yourself supplying a number, choosing between two readings, or deciding what a word means — it is ambiguous. That supplied thing is a decision, and it is not yours to make quietly.

**Do not be generous here.** Most work that feels clear has one buried decision in it. The cost of calling well-defined work ambiguous is one extra pass; the cost of the reverse is a system built on an unrecorded assumption.

---

## Step 3 — Select the path

State the selected path explicitly.

| Work type | Path |
|---|---|
| `greenfield` | `/requirements-refine` → `/solution-plan` → `/github-issues` → loop |
| `enhancement` | `/requirements-refine` → `/impact-analysis` → `/github-issues` → loop |
| `bugfix` | `/impact-analysis` → `/github-issues` (single issue) → loop, `/workflow-execute` in **TDD mode** |
| `refactor` | `/impact-analysis` → `/github-issues` (single issue) → loop, `/workflow-execute` in **characterization mode** |
| `test-improve` | `/github-issues` (single issue) → loop, `/workflow-execute` in **mutation-verified mode**, skip `/workflow-brainstorm` |
| `docs-improve` | `/github-issues` (single issue) → loop, `/workflow-execute` in **documentation mode**, skip `/workflow-brainstorm` |

### The ambiguity axis modifies the path, it does not replace it

| Level | Modification |
|---|---|
| `well-defined` | `/requirements-refine` runs in **lean mode**. Skip `/workflow-brainstorm` unless the implementation approach itself is genuinely open. |
| `ambiguous` | `/requirements-refine` runs in **full mode**. `/workflow-brainstorm` is **mandatory**. Expect ASM entries, and be prepared for an ESCALATE. |

Apply the modifier to the path from the table above. A `well-defined` `greenfield` still runs `/requirements-refine` — in lean mode. An `ambiguous` `bugfix` still starts at `/impact-analysis` — and gets a mandatory brainstorm.

---

## Step 4 — Assign a risk class

Assign `risk:low`, `risk:medium`, or `risk:high` from three inputs:

- **Blast radius** — how much breaks if this is wrong, and how many users see it.
- **Reversibility** — can this be undone by reverting, or does it involve a migration, a published contract, or emitted data?
- **Sensitive surface** — does it touch security, money, or personal data?

Anything touching security, money, or personal data starts at `risk:high` and is argued down with a reason, not up.

**This label drives who must approve at `/gate-check`** — self-approval at low, a named reviewer at medium, written sign-off at high. Choose it deliberately. Defaulting to `risk:medium` because it feels safe is how a high-risk change gets one reviewer, and how a trivial one gets three.

---

## Step 5 — Confirm with the user

**Present all four together before anything downstream runs:** work type, ambiguity level, risk class, and selected path — each with one line of reasoning.

Use the question protocol. Offer the classification you propose as the recommended option and the most plausible alternative beside it.

**This is the highest-leverage decision in the system**, because everything downstream inherits it. A misclassification here is not corrected later; it is amplified by every stage that trusts it. A bugfix routed as greenfield produces a baseline nobody asked for. An enhancement routed as bugfix leaves the requirement wrong forever.

Do not proceed to Step 6 without explicit confirmation.

---

## Step 6 — Write the intake record

Write `docs/intake/YYYY-MM-DD-<slug>-intake.md`:

```markdown
---
source: <path, issue number, or "conversation">
work_type: <greenfield | enhancement | bugfix | refactor | test-improve | docs-improve>
ambiguity: <well-defined | ambiguous>
risk: <risk:low | risk:medium | risk:high>
confirmed_by: <the user, in conversation>
---

## Source
<what arrived, and where it came from>

## Classification

### Work type: <type>
<the reasoning — including, for bugfix or enhancement, the requirement the behaviour was
measured against and which of the two is wrong>

### Ambiguity: <level>
<the reasoning — if ambiguous, name the undefined terms or the two honest readings>

## Risk class: <class>
<blast radius, reversibility, and sensitive surface, each addressed>

## Selected path
<the full stage sequence, with the ambiguity modifier applied and any execute mode named>

## Stages skipped
| Stage | Why |
|---|---|
| /workflow-brainstorm | <reason> |
```

**Every stage deliberately skipped is listed with its reason.** This matters more than it looks: a skipped stage with a recorded reason is a decision; a skipped stage with no record is an omission. Six months later nobody can tell the two apart, and the reviewer who finds the gap has no way to know whether it was considered.

If no stage was skipped, say so explicitly rather than omitting the section.

---

## Step 7 — Invoke the first skill in the selected path

Invoke it directly. Report the intake record path and what runs next.

---

## Constraints

- Do not write or modify any application code.
- Do not proceed without the user confirming the classification.
- Do not default everything to the greenfield path. Over-processing a bug fix is a failure of this skill, not a safe choice.
- Do not record a skipped stage without a reason.
- Do not collapse the two axes into a single judgment of "how big is this".
- Do not classify an empty input. Ask what arrived.
