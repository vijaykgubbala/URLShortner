# Shared conventions

Read by every skill in the chain. When a skill's own text disagrees with this document, this document wins and the skill is a defect to be fixed.

---

## The four-verdict model

Every skill that renders a judgment returns exactly one of these four verdicts. There is no fifth, and there is no "mostly passes".

### PASS

The artifact satisfies the rule being applied, with quoted evidence for it.

**Required next action:** proceed to the next stage. Record the verdict; no further work.

### PASS_WITH_AMENDMENT

The artifact does not satisfy the rule as written, but a specific, stated change makes it satisfy the rule — and that change does not require a decision anyone has yet to make.

**Required next action:** apply the amendment, then proceed. The amendment is applied *before* the artifact moves on, not logged as follow-up. `proposed_amendment` must contain the replacement text itself (see the finding envelope below).

### BLOCK

The artifact violates the rule, and proceeding would carry the violation downstream where it costs more to fix.

**Required next action:** stop. Fix the artifact and re-run the check that produced the BLOCK. Do not proceed with the finding noted — a noted blocker is an unfixed blocker with paperwork.

### ESCALATE

Two authorities conflict, and resolving the conflict is a decision, not an analysis.

**Required next action:** surface the conflict and stop.

**The ESCALATE doctrine — a hard rule.** When a requirement conflicts with a standard, or two authorities conflict with each other, the skill:

1. Surfaces the conflict with **both citations**, quoted.
2. States the **trade-off** — what is given up either way.
3. Names the **ROLE that must decide** (not a person; the role, e.g. "security owner", "product owner").
4. **STOPS.**

It never picks a side. It never picks a side *and flags it*. It never picks the side that lets the pipeline continue.

A system that quietly picks a side is worse than no system, because it produces a green tick over an unmade decision — and a green tick over an unmade decision is indistinguishable, downstream, from a decision that was actually made.

---

## Severity

| Severity | Definition | Blocks? |
|---|---|---|
| **critical** | Correctness, security, or data-integrity defect that will cause harm in production, or a violation of a rule whose whole purpose is to prevent that harm. | **Always.** |
| **high** | A defect that will cause real cost — a wrong result in a reachable edge case, a missing control, an untested acceptance criterion — but not immediate harm on the critical path. | Blocks unless explicitly dispositioned with a written reason. |
| **medium** | A quality or maintainability problem that will slow the next change. Correct today, expensive tomorrow. | No. Recorded and dispositioned. |
| **low** | A preference, a nit, an inconsistency with no functional consequence. | No. |

**critical always blocks.** The only override is a written waiver in `standards/waivers.md` with an **expiry date**. Not a good reason, not an engineer's judgement, not "deferred to a follow-up issue" — a waiver, named approver, expiry date.

A waiver with no expiry date is invalid. A waiver without an expiry is a standard being quietly repealed by whoever was in a hurry.

---

## The question protocol

Every interactive skill asks questions this way:

- **One question at a time**, via `AskUserQuestion`. Never a batch, never a numbered list the user has to answer in prose.
- **Two to four concrete options** per question. Concrete means the user can pick one without asking what it means. "Use a queue" is concrete; "consider async processing" is not.
- **Label your recommendation `(Recommended)` and put it first.** Having an opinion is part of the job; hiding it behind a neutral menu is not neutrality, it is offloading the analysis you were asked to do.
- **Keep going until no real ambiguity remains**, rather than stopping at a fixed count. Three questions is not the target and neither is ten. The target is that nothing material is still unresolved when you start writing.

Stop when the remaining ambiguities are ones where any reasonable default is fine. Say which defaults you took.

---

## The finding envelope

Every reviewing skill emits findings in this shape. All five fields are required.

| Field | Contents |
|---|---|
| `lens` | The perspective that produced this finding — correctness, security, testing, architecture, requirements-fidelity, etc. |
| `verdict` | One of PASS, PASS_WITH_AMENDMENT, BLOCK, ESCALATE. |
| `rule` | The rule being applied, **quoted**, with its ID and source path. Not paraphrased. |
| `evidence` | The text or code being judged, **quoted**, with its file and line. Not summarized. |
| `proposed_amendment` | On PASS_WITH_AMENDMENT: the replacement text itself. Empty or absent on other verdicts. |

**On `proposed_amendment`:** it must contain text that could be pasted directly in. "Add a concurrency criterion" is not an amendment — it is a request for someone else to write the amendment. `"Given two requests arrive for the same short code within 10ms, when both are processed, then exactly one record is created"` is an amendment.

**On `rule` and `evidence`:** both are quotes. A finding whose `rule` field says "best practice" or whose `evidence` field says "several places in the file" is invalid output under the evidence-before-verdict rule in `CLAUDE.md`. Produce the quote or produce nothing.
