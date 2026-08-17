# CLAUDE.md

## Thesis

> The AI produces evidence and verdicts; the engineer produces decisions. Every AI contribution traces to a requirement, is validated before it is built, and is recorded as accepted, edited, or rejected with rationale. No gate passes without a named human.

Everything below is machinery in service of that sentence. When a rule here seems inconvenient, re-read it: the inconvenience is almost always the control working.

---

## The pipeline

### The front door

**All work starts at `/work-intake`.** It classifies the work on two independent axes — work type and ambiguity — assigns a risk class, and selects the path. Applying maximum rigor to every piece of work is not rigor, it is ceremony; the intake decides how much process the work deserves and records why.

```
/work-intake  ->  <the path it selects>  ->  the per-issue loop
```

### The paths

| Work type | Path to the board |
|---|---|
| `greenfield` | `/requirements-refine` → `/solution-plan` → `/github-issues` |
| `enhancement` | `/requirements-refine` → `/impact-analysis` → `/github-issues` |
| `bugfix` | `/impact-analysis` → `/github-issues` (single issue) |
| `refactor` | `/impact-analysis` → `/github-issues` (single issue) |
| `test-improve` | `/github-issues` (single issue) |
| `docs-improve` | `/github-issues` (single issue) |

`/solution-plan` runs on the greenfield path only — a single-issue change does not need work streams. The ambiguity axis modifies the chosen path rather than replacing it: `well-defined` runs `/requirements-refine` lean and may skip the brainstorm; `ambiguous` runs it full and makes the brainstorm mandatory.

### The per-issue loop

```
/workflow-brainstorm -> /workflow-plan -> /workflow-execute -> /workflow-handover
  -> /workflow-review -> /gate-check -> /workflow-compound
```

Handover runs **before** review deliberately: the handover artifact — release notes, risk analysis, planned-versus-actual test coverage — tells the reviewer where to look before they start. `/gate-check` sits between review and compound because it is the step that can stop the work, and it must run after the findings exist and before the issue is closed out.

### How much of the loop to run

**Default depth is minimal: build with tests, commit, move on.** Run stages of the loop when the issue's risk label calls for them, not by default.

| Risk label | Loop stages that run |
|---|---|
| `risk:low` | `/workflow-execute` only |
| `risk:medium` | `/workflow-execute` → `/workflow-review` |
| `risk:high` | **The full loop, every stage, no exceptions** |

The intake already decided how much process the work deserves and wrote it on the issue as a risk label. Running the full loop on a `risk:low` issue anyway discards that decision — it is the same ceremony `/work-intake` exists to prevent, arriving one stage later.

**The risk label is read per issue, never per batch.** A request to build several issues together does not lower the depth of the highest-risk one among them; each is run at its own label. This is the failure mode that has actually occurred here — two issues named in one sentence, and the `risk:high` one built at the depth of the `risk:medium` one beside it.

Depth below `risk:high` is a default, not a ceiling. Raise it for any issue where the work turns out to be more than the label anticipated, and say that is what happened.

### Execution modes

`/work-intake` assigns an execution mode, `/workflow-plan` writes the plan against it, and `/workflow-execute` runs the matching cycle. Red-green cannot apply to a refactor (behaviour must not change) or to a test (you cannot write a failing test for a test), so in those modes the verification **inverts** rather than being dropped.

| Mode | Used for | What must be observed |
|---|---|---|
| `TDD` | greenfield, enhancement, bugfix | A test failing before the code exists |
| `characterization` | refactor | Tests passing against **unchanged** code, committed before any refactoring |
| `mutation-verified` | test-improve | The new test **failing** against a deliberate break |
| `documentation` | docs-improve | Every instruction executed literally from a clean state |

---

## Identifier scheme

Every ID has exactly one owning artifact. The owner is where the ID is minted and where its authoritative text lives; everything else references it.

| ID form | Owning artifact | What it identifies |
|---|---|---|
| `REQ-nnn` | `requirements/baseline.md` | A single testable requirement |
| `ASM-nnn` | `requirements/assumptions.md` | An assumption made in place of a decision, with its confidence |
| `NFR-nnn` | `requirements/nfr.md` | A non-functional requirement |
| `ADR-nnn` | `decisions/` | An architecture decision record |
| `STD-<PACK>-nn` | `standards/` | A rule in a standards pack, e.g. `STD-ARCH-04` |
| `WVR-nnn` | `standards/waivers.md` | A written, expiring waiver against a specific rule or finding |
| `TEST-<Name>` | the test suite | A named test asserting a specific criterion |

**GitHub issue numbers are the story IDs.** Do not invent a parallel `STORY-nnn` scheme. Two ID systems for one thing is how traceability rots — the moment they can disagree, one of them is wrong and nobody knows which.

---

## Directory layout

```
requirements/       baseline.md, assumptions.md, nfr.md, review.md
standards/          the four rule packs, VERSION, waivers.md
architecture/       the architecture reference docs
governance/         gate definitions and edit-gate policy
decisions/          ADRs
validation/         <slug>-conformance.md — forward conformance records
docs/
  intake/           per-work classification records
  impact/           per-change impact analyses
  brainstorms/      per-issue brainstorm docs
  plans/            the solution plan and per-issue implementation plans
  handovers/        per-issue handover artifacts
  reviews/          review records
  gates/            GATE-<issue>.md records
  solutions/        captured learnings, by category
traceability/       rtm.md — requirement to issue mapping
src/                application code
tests/              test suite
```

Each stage writes into exactly one of these, and the stage after it reads from there. A missing document is therefore visible as a gap rather than an assumption — `/gate-check` blocks on several of them by absence alone.

---

## Skill conventions

Skills live at `.claude/skills/<name>/SKILL.md` and **nowhere else**. Any other path under `.claude/` is silently ignored — no error, no warning, no hint that the skill you just wrote will never load. If a skill does not appear when you type `/`, check its path first and its frontmatter second.

Frontmatter rules, all of which are failure modes someone has already hit:

- **All values are single-line.** No arrays, no multi-line strings, no block scalars.
- **Any value containing `[`, `]`, `<`, `>`, or `:` must be wrapped in double quotes**, or YAML parsing breaks and the skill vanishes. `argument-hint` is the usual casualty.
- **Names follow `namespace-verb`**, never `verb-noun`. `workflow-execute`, not `execute-workflow`.
- **Descriptions are `"{what it does}. Use when {specific triggers}."`** The trigger half is what makes the skill findable; a description without it is a title.

Supporting files are linked with relative paths. Every skill closes with a `## Constraints` section.

---

## Evidence before verdict

Any skill that renders a judgment must first quote the rule it is applying and the text it is judging. A verdict with no quoted evidence is **invalid output, not a weak finding** — do not record it, do not soften it, produce the evidence or produce nothing.

This is what makes a finding arguable by a human, which is the entire point of producing it. A human cannot overturn "this looks wrong". They can overturn "`STD-SEC-02` says X, this line says Y".

---

## GitHub conventions

Issues are created with the `gh` CLI.

**State is carried by labels** — there is no other source of truth for where an issue stands:

- `status:refined` → `status:planned` → `status:in-progress` → `status:in-review` → `status:done`

**Type labels:** `type:story`, `type:spike`

**Risk labels:** `risk:low`, `risk:medium`, `risk:high` — the risk label determines who must approve at `/gate-check`.

**Dependencies** are expressed as `Blocked by #N` lines in the issue body, because GitHub has no native predecessor link.

**Branch per issue:** `feat/<issue-number>-short-description`

---

## Architecture guidance

**Before writing or modifying any code in `src/`, run `/architecture-guide "<one sentence describing what you are about to build>"`.**

It dispatches `architecture-advisor`, which reads [`architecture/`](architecture/) in its own context and returns only the rules that bind your task, quoted verbatim with their source — so the rules stay in one reviewable place instead of being copied into prompts, and your session spends context on the applicable subset rather than on documents that are mostly irrelevant to it.

`architecture/`, the advisor, and the skill are gated together as one trust boundary: see [`governance/architecture-docs-edit-gate.md`](governance/architecture-docs-edit-gate.md). Do not modify any of the three without explicit in-conversation approval — surface the gate and stop.

---

## Standards

Four rule packs are the enforceable rule source. Review agents cite rule IDs from them; story validation checks against their detection hints. **If a rule is not in one of these files, no part of the system can enforce it** — which is the reason to put it there rather than in a skill prompt.

| Pack | Prefix | Covers |
|---|---|---|
| [`standards/architecture.md`](standards/architecture.md) | `STD-ARCH` | Layer boundaries, dependency direction, REST path shape, generated OpenAPI, BFF responsibilities, ADR triggers |
| [`standards/security.md`](standards/security.md) | `STD-SEC` | Story-level authorization, trust-boundary validation, secret handling, PII in logs, redirect allowlisting, negative tests |
| [`standards/operability.md`](standards/operability.md) | `STD-OPS` | Numeric performance budgets, logs/metrics/alerts as ACs, rollback paths, critical-path isolation, feature flags, timeouts |
| [`standards/data.md`](standards/data.md) | `STD-DATA` | Additive schema change, retention, derived identifiers, migration backfill and reverse, data ownership, bounded reads |

Every rule carries a **detection hint** — what in the code or in a story's text indicates the rule applies. That field is what a validator or review agent actually matches on; a rule without one fires on everything or on nothing.

[`standards/VERSION`](standards/VERSION) pins the pack version. Validation records carry it, so a story validated under a superseded pack is detectable rather than assumed current.

[`standards/waivers.md`](standards/waivers.md) is the only override for a blocking rule. **A waiver with no expiry date is invalid** — treated as absent, not as a weak waiver.

---

## Shared conventions

Every skill reads [`.claude/skills/_shared/references/conventions.md`](.claude/skills/_shared/references/conventions.md): the four-verdict model, the severity scale, the question protocol, and the finding envelope. When a skill and that document disagree, the document wins and the skill is a defect.
