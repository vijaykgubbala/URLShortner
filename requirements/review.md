# Requirements review

**Source:** `docs/010 - Assignment - AI-Proficient Software Engineer 1.pdf`
**Baseline under review:** [`baseline.md`](baseline.md) · [`assumptions.md`](assumptions.md) · [`nfr.md`](nfr.md)
**Depth mode:** **FULL** — the intake classified this work `ambiguous`, so Step 2's interrogation ran. Fourteen assumptions were put to the user across four rounds.

> Depth changes how much is asked, never how much is checked. This review is identical to the one a `well-defined` classification would have received.

**Rounds:** two. Round 1 returned **BLOCK**. The baseline was corrected and Step 3 re-ran.

---

## Round 1 — findings

### R1 Coverage — one dropped requirement · **BLOCK**

**R1-01 — "improve" was dropped**

> **Source, §2:** *"Your task is to complete **and improve** it over 2-3 days using AI assistance"*
>
> **Baseline:** REQ-014 — *"The work shall be completed over 2–3 days."*

The first pass mapped the timeframe and the word "complete", and dropped "improve" entirely. That is not a synonym: "complete and improve" states that the work includes at least one pass over what was already built.

**Verdict: BLOCK.** A dropped requirement.

*Every other assertion in §1, §2, §3, §4.1–4.8, §5, §6 and §7 maps to at least one REQ. Verified section by section against the extracted text.*

---

### R2 Provenance — no invented scope · **PASS**

Every REQ traces to source text. Two are worth stating explicitly because they rest on inference rather than a direct quote:

> **Source, §2:** *"a URL shortener service"*
>
> **Baseline:** REQ-001 — *"accept a destination URL and return a short code"*; REQ-002 — *"resolve a short code … and redirect"*

Neither behaviour is quoted verbatim. Both are definitional to the named product category — a URL shortener that does not shorten or redirect is not one. **Verdict: PASS**, recorded so a reader can disagree.

The code quality attributes REQ-088…REQ-093 trace to §6's list *"modular, testable, reliable, secure, scalable code with safe change management"* — quoted, one REQ per attribute.

**Nothing in the baseline lacks a source.** The temptation on a document this process-heavy is to add the requirements one expects to see; none were added.

---

### R3 Atomicity — two compound requirements · **Major**

**R3-01**

> **Source, §1:** *"output generation/validation"*
>
> **Baseline:** REQ-012 — *"The work shall demonstrate output generation **and** validation."*

Two separately testable assertions. Work could generate outputs and never validate them, satisfying half the REQ while it reads as satisfied.

**R3-02**

> **Source, §4.8:** *"risks/trade-offs/validation"*
>
> **Baseline:** REQ-085 — *"A final engineering summary shall state risks, **trade-offs and** validation."*

Three assertions in one row.

**Verdict: Major on both.** Not blockers — nothing is lost, but a partially satisfied REQ reports as satisfied.

*Two were considered and cleared:* REQ-013 (*"engineer-led and AI-accelerated, not autonomous orchestration"*) is one statement of an execution model, not two assertions. REQ-035 (*"modules, services, APIs and data flows"*) is one act of identification over four artifact types, and splitting it would produce four REQs proven by the same evidence.

---

### R4 Testability — six unfalsifiable requirements · **Major**

Each of these could not be proven or disproven by an observable outcome as first written.

| REQ | As written | Why it fails |
|---|---|---|
| REQ-016 | *"demonstrate engineering judgment"* | No outcome distinguishes work that demonstrates it from work that does not |
| REQ-017 | *"treated as production-grade engineering work"* | "Production-grade" undefined — see R5-01 |
| REQ-064 | *"clean and maintainable"* | Both adjectives, neither measurable |
| REQ-088 | *"modular"* | An adjective |
| REQ-090 | *"reliable"* | An adjective |
| REQ-092 | *"scalable"* | An adjective with no target |
| REQ-094 | *"clear and defensible"* | Adjectives |

> A REQ you cannot write a test for is a wish.

**Verdict: Major.** All amended — each now names an observable outcome, most by pointing at a standards rule, a `/gate-check` criterion, or a confirmed assumption. REQ-092 became testable only because [ASM-011] supplied a scale target; before that answer, "scalable" had nothing to be measured against.

*REQ-003, REQ-004 and REQ-005 were flagged in the first draft for the same fault and were resolved by [ASM-001], [ASM-002] and [ASM-003] during Step 2 rather than by amendment.*

---

### R5 Ambiguity handling — four unclear terms with no ASM · **BLOCK**

Each of these carries real weight and had no assumption behind it, which means a decision was made silently.

**R5-01 — "production-quality" / "production-grade"**

> **Source, §4.5:** *"Produce production-quality code"* · **§7:** *"Treat as production-grade engineering work"*

Two of the document's strongest claims on the work, neither defined. → **[ASM-019]**

**R5-02 — "high-impact changes"** *(the most consequential of the four)*

> **Source, §4.4:** *"require human sign-off for high-impact changes"*

This term decides **when a human is obliged to be involved**. Leaving the threshold undefined does not weaken the control — it removes it, because any change can be argued out of the category. → **[ASM-020]**

**R5-03 — "disciplined prompting"**

> **Source, §4.4:** *"use disciplined prompting with iterative refinement"*

No observable outcome separates disciplined prompting from undisciplined prompting. → **[ASM-021]**

**R5-04 — "secure AI usage"**

> **Source, §4.4:** *"enforce secure AI usage"*

An instruction to enforce something undefined. → **[ASM-022]**

**Verdict: BLOCK.** Four ambiguities were resolved by the AI's own reading without a record — which `/requirements-refine` defines as a defect in its own output.

---

## Round 1 verdict — **BLOCK**

| Check | Verdict | Findings |
|---|---|---|
| R1 Coverage | **BLOCK** | 1 |
| R2 Provenance | PASS | 0 |
| R3 Atomicity | Major | 2 |
| R4 Testability | Major | 7 |
| R5 Ambiguity handling | **BLOCK** | 4 |

---

## Corrections applied

| Finding | Correction |
|---|---|
| R1-01 | **REQ-095** added — the service shall be improved after its initial build |
| R3-01 | REQ-012 narrowed to generation; **REQ-096** added for validation |
| R3-02 | REQ-085 narrowed to risks; **REQ-097** and **REQ-098** added for trade-offs and validation |
| R4 (×7) | REQ-016, REQ-017, REQ-064, REQ-088, REQ-090, REQ-092, REQ-093, REQ-094 amended to name observable outcomes |
| R5-01…04 | **ASM-019, ASM-020, ASM-021, ASM-022** added to the register with proposed defaults and confidence levels |

Baseline: 94 REQs → **98**. Register: 18 ASMs → **22**.

---

## Round 2 — re-run

**R1 Coverage — PASS.** Re-walked every section against the corrected baseline. "Complete and improve" now maps to REQ-014 and REQ-095. No further unmapped assertion.

**R2 Provenance — PASS.** The four added REQs trace to quoted source: REQ-095 to §2 *"complete and improve"*, REQ-096 to §1 *"output generation/validation"*, REQ-097 and REQ-098 to §4.8 *"risks/trade-offs/validation"*.

**R3 Atomicity — PASS.** No remaining REQ joins two testable assertions with "and".

**R4 Testability — PASS.** Each amended REQ names an outcome that can be proven or disproven. Verified by attempting a test for each:

> **REQ-092:** *"shall sustain the scale target in [ASM-011] — 10,000 links and 100 requests per second on resolve"* → a load test at 100 rps against a 10,000-link dataset either passes or does not.
>
> **REQ-094:** *"every AI-rendered verdict shall quote the rule applied and the text judged"* → inspect any finding; the quotes are present or absent.

**R5 Ambiguity handling — PASS.** All four terms now carry an ASM with a proposed default and a confidence level. **ASM-019, ASM-021 and ASM-022 are `proposed`, not `confirmed`** — they were raised after Step 2 had run and have not been put to the user. That is recorded rather than hidden.

---

## Overall verdict — **PASS_WITH_AMENDMENT**

The baseline is faithful to the source and testable, with one amendment outstanding.

**The amendment:** three assumptions added during the review — **ASM-019** ("production-grade"), **ASM-021** ("disciplined prompting") and **ASM-022** ("secure AI usage") — carry `medium` confidence and status `proposed`. Under the question protocol, `medium`-confidence entries are interrogated. They were not, because they were discovered after Step 2 closed.

**Required next action:** put ASM-019, ASM-021 and ASM-022 to the user, or accept the proposed defaults explicitly. ASM-020 needs no interrogation — its default is the existing `risk:` taxonomy already implemented in `/gate-check`, at `high` confidence.

This is not a BLOCK. Each proposed default is visible, defensible, and overturnable, and none is load-bearing for the next stage: `/solution-plan` shapes work streams and will not be reshaped by how "disciplined prompting" is defined.

---

## Note carried forward to `/solution-plan`

**One conflict was surfaced and resolved during Step 2 rather than silently settled**, and the record is worth reading before planning:

`STD-SEC-01` requires authorization stated at story level, at `critical` severity. The source states no authentication model at all. The user's first two answers contradicted each other — "full CRUD" against "no destructive operations" — and the contradiction was put back rather than reconciled by the skill. The resolution was **full CRUD with a capability token**, which satisfies `STD-SEC-01` outright.

**No waiver was required and none was written.** `standards/waivers.md` remains empty.

**A second observation, unresolved.** The `architecture/` documents were written prescriptively before any requirement was baselined, and they already commit to decisions this baseline has only now confirmed — CSPRNG short codes ([ASM-004]), an expiry column ([ASM-006]), a relational store and a layered .NET-shaped structure ([ASM-008]). In each case the confirmed requirement agreed with the pre-existing document, so no conflict arose. **That is fortunate rather than sound.** Had the user answered differently on any of the three, the resolution would have required a gated edit to `architecture/`, and `/solution-plan` should treat those documents as constraints inherited without provenance rather than as derived from these requirements.
