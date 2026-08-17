# A gap named in a handover is not a work item, and nothing converts it

**From:** #17 · `GATE-17.md` run 1, criterion 2

## Problem

`/workflow-handover`'s "What's Not Tested" section is designed to name gaps plainly. It did. Then the gap shipped anyway.

## What Happened

The reserved-address gap was found and **measured by probe** during the handover, listed as gap 1 of 10, with a table of six addresses proven permitted.

Then:

1. `/workflow-review` dispatched four agents, each told **not to re-report the handover's gap 1** — correctly, since the handover already had it.
2. The review found the **IPv6** instance of the same defect, which the handover had missed.
3. The remediation pass fixed every review finding, including the IPv6 half.
4. The IPv4 half — the one the handover named — was never fixed, because it was in no finding list.
5. `/gate-check` run 1 blocked on it.

The gap was known, written down, and proven for the entire duration. It survived because **being named in a handover creates no obligation on anyone.**

## Root Cause

The handover is read by the reviewer. The remediation pass works from the *review record*. Nothing carries a handover gap into the review record, and the instruction not to re-report it severed the only path it had.

## Prevention

- **Convert each "What's Not Tested" entry into either a test, a deferred review finding, or a follow-up issue** before the handover is considered complete. A gap with no destination is a gap with no owner.
- **Never tell a review agent to skip a known gap.** Duplicate findings cost a dedup line. Skipped findings cost the defect.
- **Read the handover's gap list at remediation time**, not only at review time — the remediation pass works from findings, and findings are not the only record of what is wrong.

## Key Insight

Writing a gap down is not the same as assigning it, and an instruction to skip what is already known removes the second chance to catch it.
