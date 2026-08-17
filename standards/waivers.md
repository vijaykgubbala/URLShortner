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
### WVR-001 — <short title>

- **Rule ID:** STD-SEC-05
- **Scope:** src/Redirect/LegacyLinkHandler.cs, endpoint /v1/legacy-links/{code}
- **Rationale:** <why the rule cannot be met here, and the compensating control in place>
- **Approver:** <name>
- **Expiry:** 2026-11-30
- **Follow-up issue:** #42
```

---

## Active waivers

_None._

Every waiver ever granted stays in this file after it expires, moved to the section below. Deleting the record deletes the evidence that the decision was made.

---

## Expired waivers

_None._
