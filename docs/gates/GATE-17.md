---
issue: 17
risk: risk:high
standards_version: standards@2026.08.1
branch: feat/17-destination-url-policy
commit: 0b69c71
outcome: BLOCK
---

# Gate — #17 Destination URL validation policy

**Three criteria failed.** All seven were evaluated; the gate does not stop at the first failure.

---

## Criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Unaddressed critical finding | **pass** | `docs/reviews/2026-08-17-17.json` records 6 critical findings, all with `"disposition": "addressed"` and `"commit": "63bda98"`. Zero criticals not addressed; zero addressed without a SHA. SEC-001 is addressed by waiver, not code — see Waivers below. |
| 2 | Unproven acceptance criterion | **FAIL** | See F-1. AC-3's `reserved` clause is unproven and unimplemented on the IPv4 side. |
| 3 | Uncovered requirement | **FAIL** | See F-2. REQ-002 has no test asserting it. |
| 4 | Untraceable issue | **pass** | #17 `## Traces to` reads `REQ-001, REQ-002; NFR-006, NFR-009`. Non-empty; every ID resolves to `requirements/baseline.md`. |
| 5 | Stale conformance record | **pass** | `standards/VERSION` contains `standards@2026.08.1`. `validation/url-shortener-conformance.md` declares `**Standards version in force:** standards@2026.08.1`. Identical. Adding WVR-001 changed `standards/waivers.md`, which is the waiver register and not one of the four rule packs, so no version bump was owed. |
| 6 | Missing approvals | **FAIL** | See F-3. No approval of any kind is recorded. |
| 7 | Mode-specific | **pass** | `docs/handovers/2026-08-17-17-destination-url-policy-handover.md` frontmatter reads `mode: TDD`. TDD adds no criterion. |

---

## Failures

### F-1 — Criterion 2. AC-3's "reserved" clause is unproven on the IPv4 side

**The acceptance criterion, quoted from #17:**

> *"Given a destination URL resolving to a loopback, link-local, private or **reserved** address range, when it is validated, then it is rejected with `422`."*

**The code.** `IsPermittedV4` in `src/UrlShortener.Domain/Destinations/DestinationPolicy.cs` enumerates first-octet arms for `0`, `10`, `100`, `127`, `169`, `172`, `192`, `255`. Every other value reaches `_ => true`.

Checked against the switch:

| First octet | Range | In the switch |
|---|---|---|
| `224` | multicast `224.0.0.0/4` | falls to `_ => true` — **permitted** |
| `239` | multicast, incl. `239.255.255.250` SSDP | falls to `_ => true` — **permitted** |
| `240` | reserved `240.0.0.0/4` | falls to `_ => true` — **permitted** |
| `198` | benchmark `198.18.0.0/15` | falls to `_ => true` — **permitted** |
| `203` | TEST-NET-3 | falls to `_ => true` — **permitted** |

**The tests.** No `InlineData` row exists for `224.0`, `239.255`, `240.0`, `198.18`, `192.0.2` or `203.0.113` anywhere in the suite. `169.254` and `255.255` have rows; the reserved space does not.

**Why this survived.** The handover named it as gap 1 and measured it by probe. The review agents were then instructed not to re-report it *because* the handover already had. The remediation pass fixed the IPv6 half — COR-002, which the agents did raise — and never returned for the IPv4 half. **It fell between an artifact that named it and a review told to skip it.**

**This is not a coverage gap alone.** The criterion says `reserved`, the code implements four named examples of `reserved`, and 119 passing tests agree with the code. A test asserting the criterion would fail today.

### F-2 — Criterion 3. REQ-002 has no test coverage at all

**The requirement, quoted from `requirements/baseline.md`:**

> **REQ-002** — *"The service shall resolve a short code to its destination URL and redirect the caller to it."*

No test in the suite resolves a short code, and no test asserts a redirect or a `Location` header. Grep across all four test projects returns no match for a short-code resolution behaviour. The suite tests the destination policy that the redirect path will call — not the requirement.

**REQ-001** — *"The service shall accept a destination URL and return a short code identifying it"* — is partially covered: the acceptance-or-refusal half is tested throughout; the "return a short code" half is not. Criterion 3's bar is "no coverage **at all**", so REQ-001 clears it and REQ-002 does not.

**This failure is arguable and a human should decide it.** #17's own `## Out of Scope` reads *"The create and redirect endpoints themselves — this issue delivers the policy they call."* The issue is a **contributor** to REQ-001 and REQ-002, not a deliverer of them, and #18 and #19 are the issues that deliver them. Read strictly, criterion 3 blocks every component issue whose parent requirement spans several issues — which may be a defect in the criterion rather than in this work.

**The gate does not resolve that.** It reports the failure with the evidence. Overturning it requires either a named human judging the criterion misapplied here, or a waiver.

### F-3 — Criterion 6. No approvals are recorded

**The requirement for this risk class:**

> `risk:high` — engineer, named reviewer, **and explicit written sign-off** — all three recorded with **name and timestamp**.

#17 carries the `risk:high` label. **Zero approvals of any kind exist:** no engineer approval, no named reviewer, no written sign-off. `docs/gates/` contained no prior record before this one.

The only name recorded anywhere in this issue's artifacts is `ReposVijay`, as the approver of WVR-001. That is an approval of a specific waiver, not of this gate, and it is not a substitute for either of the other two.

**This criterion cannot be satisfied by any automated step.** It is the criterion that makes the doctrine real.

---

## Waivers applied

| Waiver | Rule / finding | Approver | Expiry | Status |
|---|---|---|---|---|
| WVR-001 | `STD-SEC-05`, scoped to the redirect path | ReposVijay | 2026-11-17 | **Valid** — unexpired, names a specific rule, names an approver, cites follow-up #19 |

**Checked against the override rules.** WVR-001 has an expiry date and a named approver, and is scoped to a specific rule rather than to a pack. It is a valid override for SEC-001, which is why criterion 1 passes.

**No waiver covers F-1, F-2 or F-3.**

Specifically: WVR-001's `Rule ID` field names `STD-SEC-05` only. Its prose observes that SEC-005 — a `STD-SEC-06` finding — expires with it, but the waiver does not name `STD-SEC-06` in its rule field, so it does not waive that rule. A waiver covers the rule it names.

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

Criteria **2**, **3** and **6** failed.

Reported together rather than one at a time, so all three can be answered in one pass:

1. **F-1** requires code and tests — the IPv4 reserved ranges, mirroring the IPv6 fix already made.
2. **F-2** requires a human judgment on whether criterion 3 applies to a component issue, or a waiver.
3. **F-3** requires two named humans and a timestamp. Nothing automated can supply it.
