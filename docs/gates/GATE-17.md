---
issue: 17
risk: risk:high
standards_version: standards@2026.08.1
branch: feat/17-destination-url-policy
commit: d223fba
run: 3
outcome: PASS
---

# Gate — #17 Destination URL validation policy

**Run 3. PASS.** All seven criteria satisfied — five on evidence, one by waiver, one by recorded human approval.

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
| 6 | Missing approvals | **pass, qualified** | All three roles approved by **ReposVijay** on 2026-08-17 — see Approvals, and the independence note beneath it. |
| 7 | Mode-specific | **pass** | Handover frontmatter reads `mode: TDD`. TDD adds no criterion. |

---

## Note on criterion 6 — the approvals are not independent

**The requirement for this risk class:**

> `risk:high` — engineer, named reviewer, **and explicit written sign-off** — all three recorded with **name and timestamp**.

All three roles are approved by the same person, ReposVijay, who is the sole engineer on this repository. The rows are filled and the criterion is satisfied as written: a named human approved, with a timestamp, which is what the rule requires.

**What is not satisfied is the reason the rule splits the roles.** Three approvals exist so that more than one person has looked. Here one person looked three times. That is a property of a single-engineer project, not a defect in this approval, and it is recorded so a later reader is not misled by three filled rows into believing three people reviewed this.

**Where the independent scrutiny actually came from on this issue:** four review agents deriving findings from `standards/` and `architecture/` rather than from the author, producing six critical findings the author had shipped — including an IPv6 bypass class, a test that pinned a defect rather than catching it, and a telemetry gap invisible to a green suite. That is not equivalent to a human reviewer, and it is not offered as a substitute. It is what there was.

**A candidate for `/workflow-compound`:** either `risk:high` should require named *distinct* approvers, in which case a solo project cannot produce one and the risk taxonomy needs a solo variant — or the rule should say what it means by three roles when only one person exists. As written it is satisfiable by one name three times, which is a gap in the rule rather than in this record.

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
| Engineer | ReposVijay | 2026-08-17 |
| Named reviewer | ReposVijay | 2026-08-17 |
| Written sign-off | ReposVijay | 2026-08-17 |

**Same person in all three rows.** Sole engineer on this repository. See the note on criterion 6 above — the rule is satisfied as written; the independence it exists to create is not, and that is recorded rather than implied.

---

## Outcome — PASS

All seven criteria satisfied: five on evidence, one by valid waiver (criterion 3, WVR-002), one by recorded human approval (criterion 6).

**Two things this pass carries forward rather than settles:**

- **WVR-001 expires 2026-11-17.** The redirect path validates scheme only; on expiry `STD-SEC-05` is in force again and the next gate blocks unless #19 has implemented the re-check.
- **WVR-002 expires 2026-10-17.** Criterion 3 is waived for this issue alone. #43 amends the criterion; until it lands, the next component issue hits the same block and needs its own waiver.

**Ten review findings remain deferred** with written reasons in `docs/reviews/2026-08-17-17.json` — six high, four medium — and have no follow-up issue. The highest is the logging finding at `ValidateDestination.cs:95`: three lenses, one line, and one of the three defects is credentials reaching the log.

---

## Run history

| Run | Commit | Failed | Note |
|---|---|---|---|
| 1 | `0b69c71` | 2, 3, 6 | Criterion 2 failed because AC-3's `reserved` clause was unimplemented on the IPv4 side. The handover had named it; the review was told not to re-report it for that reason; the remediation pass fixed the IPv6 half and did not return. It fell between the two. |
| 2 | `de086bb` | 3, 6 | Criterion 2 cleared: reserved space refused by range, 19 new tests including 8 asserting adjacent addresses stay permitted. |
| 3 | `d223fba` | none | Criterion 3 waived by WVR-002. Criterion 6 satisfied by recorded approval from ReposVijay, sole engineer — three roles, one person, noted as non-independent. **PASS.** |
