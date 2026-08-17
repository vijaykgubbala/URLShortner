---
name: requirements-refine
description: Turn a raw requirement document into a baselined, testable requirement set, then review it for fidelity and gaps. Use when a requirement doc, brief, spec, email thread, or feature request needs to become something engineering can build against — even if the user does not say "requirements".
disable-model-invocation: true
argument-hint: "[path to requirement doc | topic description]"
---

# requirements-refine

**This skill has two passes. The first refines, the second reviews what the first produced.**

**Do not merge them.** A skill that drafts and grades in one pass grades its own reasoning — it will find the baseline faithful because it wrote the baseline, and the review becomes a signature rather than a check.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running — the question protocol governs Step 2 and the four-verdict model governs Step 4.

---

## Step 0 — Load the source

Take `$ARGUMENTS`:

- **A file path** — read it. For a PDF, extract the text and record how.
- **A description** — take it as the source and say so in the baseline.
- **Nothing** — ask what needs specifying. Do not baseline an empty input.

**Also read the intake record in `docs/intake/` for this work.** It carries the ambiguity classification that Step 0.5 needs, the risk class, and the selected path Step 5 hands on to.

If no intake record exists, say so and ask whether to run `/work-intake` first. Running without one means guessing the depth, which is the decision this skill is supposed to inherit rather than make.

---

## Step 0.5 — Select depth

**Read the intake record's ambiguity classification. Announce which mode is running and why.**

### LEAN MODE — `well-defined`

Decompose into `REQ-nnn`, record only genuine ambiguities as ASMs, elicit NFRs, and go **straight to the review pass**. Skip Step 2 entirely.

**Do not interrogate the user on decisions the source already settles.**

> Asking six questions about a clear requirement trains people to stop reading the questions.

That cost is not recoverable: once questions are noise, the one question that mattered is skipped too.

### FULL MODE — `ambiguous`

The complete procedure below, **including Step 2's interrogation**.

### In either mode, the review pass in Step 3 runs identically

**Depth changes how much is asked, never how much is checked.** A well-defined requirement gets fewer questions, not a weaker review — the review is what catches a dropped requirement or invented scope, and those failures are no less likely because the source read clearly.

---

## Step 1 — Refine

### Decompose

Break the prose into **atomic clauses — one testable assertion each**. Assign `REQ-nnn`. Classify each as **functional**, **non-functional**, or **constraint**.

### Record every ambiguity as an ASM

For **every** ambiguity, create an `ASM-nnn` carrying a **proposed default** AND a **confidence level** of `high`, `medium`, or `low` — rather than silently deciding.

> **Hard rule: an ambiguity this skill resolved on its own without an ASM record is a defect in this skill's output.**

The register exists to capture **what the AI decided quietly**, not only what it thought to ask about. Those are different sets, and the first is the dangerous one — a question you were asked is a decision you made, while a default applied silently is a decision nobody knows was taken.

The test: if you supplied a number, chose between two readings, or decided what a word meant, that is an ASM. Write it down even when the default is obviously right — `high` confidence is what obviously-right looks like in the register.

### Elicit NFRs by category

Work through all six: **performance, availability, security, privacy, operability, cost.**

**Any category that does not apply is marked not-applicable WITH A REASON, never omitted** — because the omitted ones are the ones nobody volunteers. A missing privacy section reads as "no privacy concerns" and as "nobody thought about privacy", and there is no way to tell which from the document.

### Write the artifacts

- `requirements/baseline.md` — the REQs, classified
- `requirements/assumptions.md` — the ASM register, with defaults and confidence
- `requirements/nfr.md` — the NFRs, all six categories present

---

## Step 2 — Interrogate (full mode only)

Ask about the high-impact ambiguities using the question protocol.

**Only ask about ASM entries with `medium` or `low` confidence.** A `high`-confidence default does not need the user's attention; it needs to be visible in the register so it can be overturned if wrong.

**Update the register with the answers, keeping the original proposed default visible** so the change is auditable. The record should show what the AI would have decided and what the human decided instead — that difference is the evidence the human was actually in the loop.

**Skip this step entirely in lean mode.**

---

## Step 3 — Review pass

**Identical in both modes.** Re-read the source against the baseline you just wrote and check five things, **each producing quoted evidence rather than an opinion**.

| ID | Check | Failure means | Severity |
|---|---|---|---|
| **R1** | **Coverage** — every assertion in the source maps to at least one REQ | A dropped requirement | **Blocker** |
| **R2** | **Provenance** — every REQ traces to text in the source | Invented scope | **Blocker** |
| **R3** | **Atomicity** — no REQ contains "and" joining two testable assertions | Two requirements in one, only one of which gets tested | Major |
| **R4** | **Testability** — every REQ could be proven or disproven by an observable outcome | A REQ you cannot write a test for is a wish | Major |
| **R5** | **Ambiguity handling** — every genuinely unclear term has an ASM | A decision was made silently | **Blocker** |

**Each finding quotes the source text and the REQ text side by side before the verdict:**

```markdown
**R2 — REQ-014**
Source: <no corresponding text found>
REQ-014: "The service shall expose a bulk import endpoint."
Verdict: BLOCK — invented scope.
```

R1 and R2 are the two directions of the same question and both are blockers: R1 catches what was lost in translation, R2 catches what was added. R2 is the harder one to see, because invented scope always looks like useful work.

---

## Step 4 — Write the review record

Write `requirements/review.md` containing:

- The findings, with quoted evidence
- **The depth mode used** — so a reader knows whether Step 2 ran
- An **overall verdict** per the four-verdict model

**On BLOCK, fix the baseline and re-run Step 3.** Do not record the blocker and continue — `/solution-plan` Step 0 reads this file and refuses to plan against an unresolved BLOCK, so a recorded-and-ignored blocker stops the pipeline one stage later with less context.

---

## Step 5 — Report and hand on

Report the counts:

- REQs, by classification
- ASMs, **by confidence level**
- NFRs, by category, including which were marked not-applicable
- Review findings by severity, and the overall verdict

**Suggest the next skill in the intake record's path** — `/solution-plan` on the greenfield path, `/impact-analysis` on the enhancement path.

---

## Constraints

- Do not write or modify any application code. This skill produces documentation only.
- Do not merge the refine and review passes.
- Do not resolve an ambiguity without recording it as an ASM first.
- Do not omit an NFR category. Mark it not-applicable with a reason instead.
- Do not run full-mode interrogation on work classified well-defined.
- Do not weaken the review pass in lean mode. Depth changes what is asked, not what is checked.
- Do not record a finding without quoting the source text and the REQ text.
