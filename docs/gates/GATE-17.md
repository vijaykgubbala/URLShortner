---
issue: 17
risk: risk:high
standards_version: standards@2026.08.1
branch: feat/17-destination-url-policy
commit: d223fba
run: 3
outcome: BLOCK
---

# Gate — #17 Destination URL validation policy

**Run 3.** Six of seven criteria are satisfied. **Criterion 6 fails.**

All seven were evaluated. The gate does not stop at the first failure.

---

## Criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Unaddressed critical finding | **pass** | `docs/reviews/2026-08-17-17.json`: 6 critical findings, 0 with a disposition other than `addressed`, 0 addressed without a commit SHA. All carry `"commit": "63bda98"`. |
| 2 | Unproven acceptance criterion | **pass** | Each of the six ACs on #17 has a named passing test. AC-3's *"loopback, link-local, private or **reserved** address range"* is asserted on both families: `Ipv4_reserved_ranges_are_refused` (11 rows) and `Ipv6_reserved_ranges_are_refused` (5 rows), with `Ipv4_addresses_adjacent_to_the_reserved_ranges_are_permitted` (8 rows) preventing an over-wide mask from satisfying them. Suite: 138 passing, 0 failing. |
| 3 | Uncovered requirement | **waived** | REQ-002 has no test asserting it — unchanged and still true. Overridden by **WVR-002**, valid: names criterion 3 specifically, scoped to issue #17 alone, approver `ReposVijay`, expiry `2026-10-17` (unexpired), follow-up #43. |
| 4 | Untraceable issue | **pass** | #17 `## Traces to` reads `REQ-001, REQ-002; NFR-006, NFR-009`. Non-empty; every ID resolves in `requirements/baseline.md`. |
| 5 | Stale conformance record | **pass** | `standards/VERSION` = `standards@2026.08.1`. `validation/url-shortener-conformance.md` declares `**Standards version in force:** standards@2026.08.1`. Identical. `standards/waivers.md` gained WVR-001 and WVR-002 since the record was written; the waiver register is not one of the four rule packs, so no version bump was owed. |
| 6 | Missing approvals | **FAIL** | See F-3. |
| 7 | Mode-specific | **pass** | Handover frontmatter reads `mode: TDD`. TDD adds no criterion. |

---

## Failure

### F-3 — Criterion 6. No approvals are recorded

**The requirement for this risk class:**

> `risk:high` — engineer, named reviewer, **and explicit written sign-off** — all three recorded with **name and timestamp**.

#17 carries the `risk:high` label. The Approvals table below has **0 of 3 rows filled**. No engineer approval, no named reviewer, no written sign-off exists in any artifact for this issue.

`ReposVijay` appears twice in this repository — as the approver of WVR-001 and of WVR-002. Both are approvals of specific waivers against specific rules. Neither is an approval of this gate, and one name cannot fill three roles that exist precisely so that more than one person has looked.

**This is the only remaining failure, and nothing automated can close it.** Every other criterion has been satisfied by work or overridden by a written waiver. This one requires people.

> *"A pass requires a named human to have approved it. If no name is available, the gate has not passed — it is waiting."*

---

## Waivers applied

| Waiver | Rule / finding | Approver | Expiry | Status |
|---|---|---|---|---|
| WVR-001 | `STD-SEC-05`, scoped to the redirect path | ReposVijay | 2026-11-17 | **Valid** — unexpired, names a specific rule, names an approver, cites follow-up #19 |
| WVR-002 | `/gate-check` criterion 3, scoped to issue #17 | ReposVijay | 2026-10-17 | **Valid** — unexpired, names a specific criterion, names an approver, cites follow-up #43 |

Both checked against the override rules: each has an expiry date and a named approver, and each is scoped to a single rule and a bounded surface rather than to a pack or a category.

**WVR-002 was deliberately not written as "criterion 3 does not apply to component issues."** Almost every issue in `docs/plans/2026-08-17-solution-plan.md` is a component issue, so that scope would have removed the criterion — which this register's own rules call a repeal, not a waiver.

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

Criterion **6** failed. Criteria 1, 2, 4, 5 and 7 pass on evidence; criterion 3 is overridden by a valid waiver.

**To clear it, three names and three timestamps must be recorded here** — engineer, named reviewer, and written sign-off. That is the whole of what remains.

---

## Run history

| Run | Commit | Failed | Note |
|---|---|---|---|
| 1 | `0b69c71` | 2, 3, 6 | Criterion 2 failed because AC-3's `reserved` clause was unimplemented on the IPv4 side. The handover had named it; the review was told not to re-report it for that reason; the remediation pass fixed the IPv6 half and did not return. It fell between the two. |
| 2 | `de086bb` | 3, 6 | Criterion 2 cleared: reserved space refused by range, 19 new tests including 8 asserting adjacent addresses stay permitted. |
| 3 | `d223fba` | 6 | Criterion 3 waived by WVR-002, scoped to this issue and expiring in two months against follow-up #43. Only the approvals criterion remains. |
