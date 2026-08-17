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
- **Follow-up issue:** #19 — the redirect path re-check, to be implemented with a cached policy verdict and a bounded TTL, or this waiver renewed with a fresh decision.

**Raised by:** `review-security` finding SEC-001 during `/workflow-review 17`. The agent was correct that the accepted-residual-risk note in `docs/plans/2026-08-17-17-feature-destination-url-policy-plan.md` was **not** a waiver and did not satisfy this rule. This record is what makes the deviation reviewable and time-bounded rather than a permanent quiet exception.

**Consequence for SEC-005:** the DNS-rebinding threat named in the brainstorm and plan has no negative test, and cannot have one while this waiver stands — there is no second check to fail closed. That gap expires with this waiver.

Every waiver ever granted stays in this file after it expires, moved to the section below. Deleting the record deletes the evidence that the decision was made.

---

## Expired waivers

_None._
