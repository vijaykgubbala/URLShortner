---
issue: 17
risk: risk:high
standards_version: standards@2026.08.1
branch: feat/17-destination-url-policy
commit: de086bb
run: 2
outcome: BLOCK
---

# Gate — #17 Destination URL validation policy

**Run 2.** Run 1 blocked on criteria 2, 3 and 6 at commit `0b69c71`. Criterion 2 is now satisfied. **Criteria 3 and 6 still fail.**

All seven criteria were evaluated. The gate does not stop at the first failure.

---

## Criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Unaddressed critical finding | **pass** | `docs/reviews/2026-08-17-17.json`: 6 critical findings, 0 with a disposition other than `addressed`, 0 addressed without a commit SHA. All carry `"commit": "63bda98"`. |
| 2 | Unproven acceptance criterion | **pass** | Each of the six ACs on #17 has a named passing test. The clause that failed run 1 — AC-3's *"loopback, link-local, private or **reserved** address range"* — is now asserted on both families: `Ipv4_reserved_ranges_are_refused` (11 rows) and `Ipv6_reserved_ranges_are_refused` (5 rows). `IsPermittedV4` carries a `>= 224 => false` range arm rather than named octets. The six addresses run 1 measured as permitted were re-probed and all six are refused. Suite: 138 passing, 0 failing. |
| 3 | Uncovered requirement | **FAIL** | See F-2. Unchanged from run 1. |
| 4 | Untraceable issue | **pass** | #17 `## Traces to` reads `REQ-001, REQ-002; NFR-006, NFR-009`. Non-empty; every ID resolves in `requirements/baseline.md`. |
| 5 | Stale conformance record | **pass** | `standards/VERSION` = `standards@2026.08.1`. `validation/url-shortener-conformance.md` declares `**Standards version in force:** standards@2026.08.1`. Identical. No rule pack changed since the record was written; `standards/waivers.md` gained WVR-001, and the waiver register is not one of the four packs. |
| 6 | Missing approvals | **FAIL** | See F-3. Unchanged from run 1. |
| 7 | Mode-specific | **pass** | Handover frontmatter reads `mode: TDD`. TDD adds no criterion. |

---

## Failures

### F-2 — Criterion 3. REQ-002 has no test coverage at all

**The requirement, quoted from `requirements/baseline.md`:**

> **REQ-002** — *"The service shall resolve a short code to its destination URL and redirect the caller to it."*

**The evidence.** Searched all four test projects for a test asserting this behaviour:

| Pattern searched | Result |
|---|---|
| A test method naming redirect | no match |
| `Assert` on a `302` status | no match |
| `Assert` on a `Location` header | no match |
| Any reference to a short code | no match |

The single textual match for "redirect" anywhere in the suite is a documentation comment in `AddressRangeTests.cs`: *"addresses that must never be redirected to."* A comment is not a test.

**REQ-001** — *"The service shall accept a destination URL and return a short code identifying it"* — clears this criterion. Its acceptance-or-refusal half is asserted extensively; the "return a short code" half is not. Criterion 3's bar is "no coverage **at all**", and partial coverage clears it.

**This failure is arguable, and the gate does not resolve it.**

#17's own `## Out of Scope` reads: *"The create and redirect endpoints themselves — this issue delivers the policy they call."* The issue contributes to REQ-001 and REQ-002; #18 and #19 deliver them.

Read strictly, criterion 3 blocks **every component issue whose requirement spans several issues**. That is plausibly a defect in the criterion rather than in this work — the criterion was written assuming an issue delivers its requirements, and the decomposition in `docs/plans/2026-08-17-solution-plan.md` does not work that way.

**Two paths, both requiring a named human:**

1. **Judge the criterion misapplied here** and record that judgment with a name and timestamp. If taken, the criterion itself should be amended — a `/workflow-compound` candidate, since a criterion that must be judged misapplied on most issues is a criterion that will be ignored on all of them.
2. **Waive it** — a `WVR-nnn` naming criterion 3, scoped to #17, with an approver, an expiry and a follow-up.

Nothing automated can choose between those.

### F-3 — Criterion 6. No approvals are recorded

**The requirement for this risk class:**

> `risk:high` — engineer, named reviewer, **and explicit written sign-off** — all three recorded with **name and timestamp**.

#17 carries `risk:high`. The Approvals table below has **0 of 3 rows filled**.

The only name in any artifact for this issue is `ReposVijay`, recorded as the approver of WVR-001. That approves a specific waiver against `STD-SEC-05`. It is not an engineer approval, not a reviewer approval, and not a sign-off.

**This criterion cannot be satisfied by any automated step, and that is deliberate.** It is the criterion that makes the doctrine at the top of this skill real rather than decorative.

---

## Waivers applied

| Waiver | Rule / finding | Approver | Expiry | Status |
|---|---|---|---|---|
| WVR-001 | `STD-SEC-05`, scoped to the redirect path | ReposVijay | 2026-11-17 | **Valid** — unexpired, names a specific rule, names an approver, cites follow-up #19 |

Checked against the override rules: WVR-001 has both an expiry date and a named approver, and is scoped to a rule rather than to a pack. It is a valid override for SEC-001, which is why criterion 1 passes.

**No waiver covers F-2 or F-3.** WVR-001's `Rule ID` field names `STD-SEC-05` only; a waiver covers the rule it names.

---

## Approvals

| Role | Name | Timestamp |
|---|---|---|
| Engineer | — | — |
| Named reviewer | — | — |
| Written sign-off | — | — |

**None recorded. The gate has not passed; it is waiting.**

---

## Outcome — BLOCK

Criteria **3** and **6** failed. Criterion 2 was cleared since run 1.

Neither remaining failure can be closed by writing code:

- **F-2** needs a judgment on whether criterion 3 applies to a component issue, or a waiver.
- **F-3** needs two named humans and a timestamp.

---

## Run history

| Run | Commit | Failed | Note |
|---|---|---|---|
| 1 | `0b69c71` | 2, 3, 6 | Criterion 2 failed because AC-3's `reserved` clause was unimplemented on the IPv4 side. The handover had named it; the review was told not to re-report it for that reason; the remediation pass fixed the IPv6 half and did not return. It fell between the two. |
| 2 | `de086bb` | 3, 6 | Criterion 2 cleared: reserved space now refused by range, 19 new tests including 8 asserting adjacent addresses stay permitted. |
