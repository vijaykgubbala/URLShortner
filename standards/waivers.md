# Waivers

A waiver is the **only** override for a blocking rule or a critical finding. There is no other path — not "deferred with a good reason", not "the engineer judged it acceptable", not a review disposition of `ignored`. `/gate-check` reads this file and accepts nothing else.

Waivers are identified `WVR-nnn`. This file owns those IDs.

---

## The rule that makes this work

**A waiver with no expiry date is invalid.** A validator, a review agent, or a gate must treat it as absent — not as a weak waiver, not as a waiver pending clarification. Absent.

A waiver without an expiry is a standard being quietly repealed by whoever was in a hurry. The expiry is what forces the decision to be made again by someone who can see whether it was right.

Two consequences worth stating plainly:

- **An expired waiver is not a warning.** The rule it suspended is in force again from the expiry date, and the gate blocks.
- **A waiver with no named approver is invalid** for the same reason a review disposition with no reason is invalid: there is no one to ask, so there is no record.

---

## Record format

Every waiver carries all six fields. A record missing any one of them is invalid.

| Field | Contents |
|---|---|
| **Rule ID** | The specific `STD-<PACK>-nn` being waived, or the specific review finding ID. Never a pack, never a category — "we have a waiver on security" is not a waiver. |
| **Scope** | The exact files, endpoints, or issue this applies to. A waiver with unbounded scope is a repeal. |
| **Rationale** | Why the rule cannot be met here, and what is done instead. "No time" is a schedule, not a rationale. |
| **Approver** | The name of the human who accepted the risk. A role is not enough — `/gate-check` records a name. |
| **Expiry** | An absolute date, `YYYY-MM-DD`. Not "next quarter", not "when the refactor lands". |
| **Follow-up issue** | The issue number tracking the work that removes the need for this waiver. |

---

## Template

```markdown
### WVR-nnn — <short title>

- **Rule ID:** STD-SEC-05
- **Scope:** src/Redirect/LegacyLinkHandler.cs, endpoint /v1/legacy-links/{code}
- **Rationale:** <why the rule cannot be met here, and the compensating control in place>
- **Approver:** <name>
- **Expiry:** 2026-11-30
- **Follow-up issue:** #42
```

> The heading above is `WVR-nnn` deliberately. An earlier version of this template used `WVR-001`, which collided with the first real waiver the moment one was written — two records under one ID, which is the failure the identifier scheme exists to prevent.

---

## Active waivers

### WVR-001 — Redirect path validates scheme only, not resolved addresses

- **Rule ID:** `STD-SEC-05` — *"A user-supplied or stored destination URL must be validated against an allowlist of permitted schemes and hosts immediately before issuing a redirect or server-side request."*
- **Scope:** The redirect path only — `DestinationPolicy.CheckScheme`, and the resolution endpoint that will call it in #19. The creation path is **not** waived: it performs the full check including resolved addresses.
- **Rationale:** The full check requires a DNS lookup, and #19's acceptance criteria hold the redirect path to 50 ms at p99 under 100 rps. Resolving on every redirect cannot meet that without a cache whose invalidation is itself unbuilt. The service never fetches the destination server-side — it emits a `Location` header and the visitor's own browser makes the request — so the SSRF half of `STD-SEC-05`'s rationale does not apply. **The phishing half does**, and this waiver accepts that residual exposure: a host that resolved publicly at creation and later resolves privately will still be redirected to.
- **Approver:** ReposVijay
- **Expiry:** 2026-11-17
- **Follow-up issue:** #47 — the redirect path re-check, to be implemented with a cached policy verdict and a bounded TTL, or this waiver renewed with a fresh decision.

**Amended 2026-08-17.** This field named **#19** until #19 shipped and closed without implementing the re-check: what merged was the scheme half only, which is inside this waiver's scope and therefore not a violation — but it left the waiver pointing at spent work, so nothing tracked its removal. Raised by `review-security` finding SEC-004 during `/workflow-review 19`. The reference now names #47, which is open. Recorded rather than silently repointed, because the history that matters here is that the remedy was deferred a second time rather than delivered.

**Raised by:** `review-security` finding SEC-001 during `/workflow-review 17`. The agent was correct that the accepted-residual-risk note in `docs/plans/2026-08-17-17-feature-destination-url-policy-plan.md` was **not** a waiver and did not satisfy this rule. This record is what makes the deviation reviewable and time-bounded rather than a permanent quiet exception.

**Consequence for SEC-005:** the DNS-rebinding threat named in the brainstorm and plan has no negative test, and cannot have one while this waiver stands — there is no second check to fail closed. That gap expires with this waiver.

---

### WVR-002 — Criterion 3 waived for #17, which contributes to REQ-002 rather than delivering it

- **Rule ID:** `/gate-check` blocking criterion 3 — *"Any requirement the issue traces to that has no test coverage at all."*
- **Scope:** **Issue #17 only.** Not a class, not a category, not "component issues" generally. The next issue that hits this needs its own waiver, or #43 lands and no waiver is needed.
- **Rationale:** #17 traces to REQ-002 — *"The service shall resolve a short code to its destination URL and redirect the caller to it"* — and no test asserts it, because #17 does not build it. Its Out of Scope reads *"The create and redirect endpoints themselves — this issue delivers the policy they call."* #19 delivers REQ-002 and will carry its tests. The criterion assumes an issue delivers the requirements it traces to; the solution plan decomposes them across issues. Blocking here would mean no component issue can ever pass a gate.
- **Approver:** ReposVijay
- **Expiry:** 2026-10-17
- **Follow-up issue:** #43 — amend criterion 3 so it fits component issues, after which this waiver is unnecessary.

**Why this is scoped to one issue and not to a class.** A waiver reading "criterion 3 does not apply to component issues" would remove the criterion: almost every issue in this plan is a component issue, and a rule that never fires is worse than no rule because the pack still looks complete. The register's own words — *"A waiver with unbounded scope is a repeal"* — apply to a criterion as much as to a standard.

**The expiry is two months, shorter than WVR-001's three.** This waiver exists because a criterion is arguably wrong, and the fix is #43 rather than renewal. A long expiry would remove the pressure to write it.

**What is not waived.** REQ-002 still has no test anywhere in the repository. This waiver says #17 is not the issue that owes one; it does not say the requirement is covered. `traceability/rtm.md` still shows REQ-002 mapped to #17 and #19, and #19 is where the debt sits.

---

## Expired waivers

**Every waiver ever granted stays in this file after it expires**, moved to this section rather than deleted. Deleting the record deletes the evidence that the decision was made.

_None._
